using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.Extensions.Generators;

/// <summary>How a feed member of a model relates to the model's inputs.</summary>
internal enum FeedKind
{
	/// <summary>Fed by a constructor parameter (a service) — the mockable input set.</summary>
	ServiceDependent,

	/// <summary>Computed over another feed member — an optional override, real logic by default.</summary>
	Derived,

	/// <summary>Neither — not part of the mock.</summary>
	Independent,
}

/// <summary>
/// The model-side dependency analysis behind <c>[FeedDependency]</c> and <c>[CtorDependency]</c>: which
/// feed members are fed by a constructor parameter, which are derived from another feed, and which
/// constructor parameters are dereferenced eagerly. It describes an MVUX model whether or not anything
/// ever mocks it.
///
/// It lives in the shared folder because two generators must answer these questions identically: the
/// MVUX generator emits the answers as metadata for consumers that read the app as a compiled
/// reference, and the mocking generator runs the analysis directly when the models sit in the
/// compilation being generated — there the attributes come from a sibling generator, and a generator
/// cannot observe another generator's output.
/// </summary>
internal sealed class FeedDependencyAnalysis
{
	private readonly Compilation _compilation;
	private readonly Func<ISymbol, bool> _isFeedMember;

	// Binding a tree is expensive and every model of an assembly tends to sit in a handful of trees, so
	// the models share one cache for the lifetime of the analysis rather than re-binding per member.
	private readonly Dictionary<SyntaxTree, SemanticModel> _semanticModels = new Dictionary<SyntaxTree, SemanticModel>();

	/// <param name="compilation">The compilation the models are read from.</param>
	/// <param name="isFeedMember">
	/// Whether a field/property is a feed. Injected because the two callers resolve the feed
	/// interfaces through different plumbing.
	/// </param>
	public FeedDependencyAnalysis(Compilation compilation, Func<ISymbol, bool> isFeedMember)
	{
		_compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
		_isFeedMember = isFeedMember ?? throw new ArgumentNullException(nameof(isFeedMember));
	}

	/// <summary>The model's own accessible, non-static feed members, in declaration order.</summary>
	public List<ISymbol> GetFeedMembers(INamedTypeSymbol model)
		=> model
			.GetMembers()
			.Where(m => m is IPropertySymbol or IFieldSymbol && !m.IsStatic && m.IsAccessible())
			.Where(m => _isFeedMember(m))
			.ToList();

	/// <summary>The constructors a consumer could call — the clone constructor is not one of them.</summary>
	public IEnumerable<IMethodSymbol> AccessibleInstanceCtors(INamedTypeSymbol model)
		=> model.Constructors.Where(c => !c.IsStatic && !c.IsCloneCtor(model) && c.DeclaredAccessibility is not Accessibility.Private);

	/// <summary>Every constructor parameter name of the model.</summary>
	public HashSet<string> GetCtorParameterNames(INamedTypeSymbol model)
	{
		var names = new HashSet<string>(StringComparer.Ordinal);
		foreach (var ctor in AccessibleInstanceCtors(model))
		{
			foreach (var p in ctor.Parameters)
			{
				names.Add(p.Name);
			}
		}

		return names;
	}

	/// <summary>
	/// Maps a field/property name to the constructor parameter it is fed by, so a feed body referencing
	/// that member is recognized as service-dependent — either through an explicit assignment
	/// (<c>_svc = svc;</c>) or through a positional record's synthesized property.
	/// </summary>
	public Dictionary<string, string> BuildFieldToParamMap(INamedTypeSymbol model, HashSet<string> ctorParamNames)
	{
		var map = new Dictionary<string, string>(StringComparer.Ordinal);

		// A primary constructor has no body to scan. On a positional record the parameter also surfaces
		// as a same-named property, and a feed body referencing it binds to that property rather than to
		// the parameter, so map it here. Seeded first: an explicit assignment below wins.
		foreach (var ctor in AccessibleInstanceCtors(model).Where(IsPrimaryConstructor))
		{
			foreach (var p in ctor.Parameters)
			{
				if (model.GetMembers(p.Name).Any(m => m is IPropertySymbol or IFieldSymbol))
				{
					map[p.Name] = p.Name;
				}
			}
		}

		foreach (var ctor in AccessibleInstanceCtors(model))
		{
			foreach (var node in ctor.DeclaringSyntaxReferences.Select(syntaxRef => syntaxRef.GetSyntax()))
			{
				var body = (SyntaxNode?)(node as ConstructorDeclarationSyntax)?.Body
					?? (node as ConstructorDeclarationSyntax)?.ExpressionBody?.Expression;
				if (body is null)
				{
					continue;
				}

				var semanticModel = GetSemanticModel(node.SyntaxTree);
				foreach (var assignment in body.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>())
				{
					if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
					{
						continue;
					}

					if (assignment.Right is not IdentifierNameSyntax rhs)
					{
						continue;
					}

					if (semanticModel.GetSymbolInfo(rhs).Symbol is not IParameterSymbol param || !ctorParamNames.Contains(param.Name))
					{
						continue;
					}

					var lhsSymbol = semanticModel.GetSymbolInfo(assignment.Left).Symbol;
					var targetName = lhsSymbol switch
					{
						IFieldSymbol field => field.Name,
						IPropertySymbol prop => prop.Name,
						_ => null,
					};
					if (targetName is not null)
					{
						map[targetName] = param.Name;
					}
				}
			}
		}

		return map;
	}

	/// <summary>
	/// A primary constructor is declared by the type declaration itself rather than by a constructor
	/// declaration — that is what distinguishes it from a hand-written constructor.
	/// </summary>
	private static bool IsPrimaryConstructor(IMethodSymbol ctor)
		=> ctor.DeclaringSyntaxReferences.Any(syntaxRef => syntaxRef.GetSyntax() is TypeDeclarationSyntax);

	/// <summary>Classifies one feed member of <paramref name="model"/>.</summary>
	public (FeedKind kind, List<string> derivedFrom, List<string> services) ClassifyFeedMember(
		ISymbol member,
		HashSet<string> feedMemberNames,
		HashSet<string> ctorParamNames,
		Dictionary<string, string> fieldToParam,
		INamedTypeSymbol model)
	{
		var derivedFrom = new List<string>();
		var services = new List<string>();
		var seenDerived = new HashSet<string>(StringComparer.Ordinal);
		var seenServices = new HashSet<string>(StringComparer.Ordinal);

		foreach (var body in GetMemberBodies(member))
		{
			var semanticModel = GetSemanticModel(body.SyntaxTree);
			foreach (var symbol in body
				.DescendantNodesAndSelf()
				.OfType<SimpleNameSyntax>()
				.Select(id => semanticModel.GetSymbolInfo(id).Symbol))
			{
				if (symbol is null)
				{
					continue;
				}

				// Another feed member of THIS model → derived.
				if ((symbol is IPropertySymbol or IFieldSymbol)
					&& SymbolEqualityComparer.Default.Equals(symbol.ContainingType, model)
					&& !string.Equals(symbol.Name, member.Name, StringComparison.Ordinal)
					&& feedMemberNames.Contains(symbol.Name))
				{
					if (seenDerived.Add(symbol.Name))
					{
						derivedFrom.Add(symbol.Name);
					}
					continue;
				}

				// A ctor parameter (primary-ctor capture), directly referenced → service.
				if (symbol is IParameterSymbol p && ctorParamNames.Contains(p.Name))
				{
					if (seenServices.Add(p.Name))
					{
						services.Add(p.Name);
					}
					continue;
				}

				// A field/property fed by a ctor parameter → service.
				var backingName = symbol switch
				{
					IFieldSymbol f => f.Name,
					IPropertySymbol pr => pr.Name,
					_ => null,
				};
				if (backingName is not null
					&& SymbolEqualityComparer.Default.Equals(symbol.ContainingType, model)
					&& fieldToParam.TryGetValue(backingName, out var paramName)
					&& seenServices.Add(paramName))
				{
					services.Add(paramName);
				}
			}
		}

		if (derivedFrom.Count > 0)
		{
			return (FeedKind.Derived, derivedFrom, services);
		}
		if (services.Count > 0)
		{
			return (FeedKind.ServiceDependent, derivedFrom, services);
		}
		return (FeedKind.Independent, derivedFrom, services);
	}

	private SemanticModel GetSemanticModel(SyntaxTree tree)
	{
		if (!_semanticModels.TryGetValue(tree, out var semanticModel))
		{
			_semanticModels[tree] = semanticModel = _compilation.GetSemanticModel(tree);
		}

		return semanticModel;
	}

	/// <summary>
	/// Returns the getter/initializer body syntax nodes of a feed member (property expression body,
	/// getter body, or property/field initializer).
	/// </summary>
	private IEnumerable<SyntaxNode> GetMemberBodies(ISymbol member)
	{
		var bodies = new List<SyntaxNode>();
		foreach (var syntaxRef in member.DeclaringSyntaxReferences)
		{
			switch (syntaxRef.GetSyntax())
			{
				case PropertyDeclarationSyntax pds:
					if (pds.Initializer?.Value is { } propertyInit)
					{
						bodies.Add(propertyInit);
					}
					else if (pds.ExpressionBody?.Expression is { } exprBody)
					{
						bodies.Add(exprBody);
					}
					else if (pds.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) is { } getter)
					{
						var gb = (SyntaxNode?)getter.ExpressionBody?.Expression ?? getter.Body;
						if (gb is not null)
						{
							bodies.Add(gb);
						}
					}
					break;

				case VariableDeclaratorSyntax vds when vds.Initializer?.Value is { } fieldInit:
					bodies.Add(fieldInit);
					break;
			}
		}

		return bodies;
	}

	/// <summary>
	/// Constructor instrumentation (R1): finds constructor parameters that are dereferenced eagerly
	/// (member access / invocation receiver), directly or through the member they feed, in a constructor body
	/// or in a feed member's body, excluding references nested in a lambda / anonymous method / local function,
	/// which are deferred rather than eager. Feed bodies count because the generated view-model's constructor
	/// builds every feed: a method group such as <c>ListFeed.Async(Service.GetItems)</c> reads its service then.
	/// </summary>
	public HashSet<string> FindEagerCtorParameters(
		INamedTypeSymbol model,
		IEnumerable<ISymbol> feedMembers,
		HashSet<string> ctorParamNames,
		Dictionary<string, string> fieldToParam)
	{
		var eager = new HashSet<string>(StringComparer.Ordinal);

		foreach (var ctor in AccessibleInstanceCtors(model))
		{
			foreach (var node in ctor.DeclaringSyntaxReferences.Select(syntaxRef => syntaxRef.GetSyntax()))
			{
				var body = (SyntaxNode?)(node as ConstructorDeclarationSyntax)?.Body
					?? (node as ConstructorDeclarationSyntax)?.ExpressionBody?.Expression;
				if (body is null)
				{
					continue;
				}

				InspectEager(body, model, ctorParamNames, fieldToParam, eager);
			}
		}

		foreach (var body in feedMembers.SelectMany(GetMemberBodies))
		{
			InspectEager(body, model, ctorParamNames, fieldToParam, eager);
		}

		return eager;
	}

	private void InspectEager(
		SyntaxNode body,
		INamedTypeSymbol model,
		HashSet<string> ctorParamNames,
		Dictionary<string, string> fieldToParam,
		HashSet<string> eager)
	{
		var semanticModel = GetSemanticModel(body.SyntaxTree);

		// The receiver of a member-access / element-access is an eager dereference.
		var receivers = body
			.DescendantNodesAndSelf()
			.Select(access => access switch
			{
				MemberAccessExpressionSyntax mae => mae.Expression,
				ElementAccessExpressionSyntax eae => eae.Expression,
				_ => (ExpressionSyntax?)null,
			});

		foreach (var receiver in receivers)
		{
			if (receiver is null || DereferencedName(receiver) is not { } id)
			{
				continue;
			}

			if (IsInsideDeferredBoundary(receiver, body))
			{
				continue; // lambda/anonymous/local-function body → not eager at construction
			}

			if (CtorParameterBehind(semanticModel.GetSymbolInfo(id).Symbol, model, ctorParamNames, fieldToParam) is { } parameter)
			{
				eager.Add(parameter);
			}
		}
	}

	/// <summary>The name a receiver dereferences, seen through <c>this.</c>, parentheses, <c>!</c> and casts.</summary>
	private static IdentifierNameSyntax? DereferencedName(ExpressionSyntax receiver)
		=> receiver switch
		{
			IdentifierNameSyntax name => name,
			MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax name } => name,
			ParenthesizedExpressionSyntax parenthesized => DereferencedName(parenthesized.Expression),
			PostfixUnaryExpressionSyntax suppressed when suppressed.IsKind(SyntaxKind.SuppressNullableWarningExpression) => DereferencedName(suppressed.Operand),
			CastExpressionSyntax cast => DereferencedName(cast.Expression),
			_ => null,
		};

	/// <summary>The constructor parameter a symbol is, or the one the model member it names is fed by.</summary>
	private static string? CtorParameterBehind(
		ISymbol? symbol,
		INamedTypeSymbol model,
		HashSet<string> ctorParamNames,
		Dictionary<string, string> fieldToParam)
	{
		if (symbol is IParameterSymbol parameter)
		{
			return ctorParamNames.Contains(parameter.Name) ? parameter.Name : null;
		}

		return symbol is IFieldSymbol or IPropertySymbol
			&& SymbolEqualityComparer.Default.Equals(symbol.ContainingType, model)
			&& fieldToParam.TryGetValue(symbol.Name, out var parameterName)
				? parameterName
				: null;
	}

	private static bool IsInsideDeferredBoundary(SyntaxNode node, SyntaxNode stopAt)
	{
		for (var current = node.Parent; current is not null && current != stopAt; current = current.Parent)
		{
			if (current is SimpleLambdaExpressionSyntax
				or ParenthesizedLambdaExpressionSyntax
				or AnonymousMethodExpressionSyntax
				or LocalFunctionStatementSyntax)
			{
				return true;
			}
		}
		return false;
	}
}
