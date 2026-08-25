using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

/// <summary>
/// Spec 013 — MVUX mocking metadata emission (D11/D12).
///
/// Emits, on the model partial and only under <c>[assembly: EnableFeedMocking]</c>:
///   - <c>[FeedDependency(member, OnParameter/OnFeed)]</c> classifying every feed member as
///     service-dependent input / derived / independent (read by the external mocking generator);
///   - <c>[CtorDependency(param, Eager=true)]</c> for constructor parameters dereferenced eagerly,
///     so the generated <c>Create(...)</c> can require them (R1 — would NRE under null-inject).
///
/// There are deliberately NO per-feed swap hooks (D11): the runtime swap is reflection over the
/// model's <c>IHotSwapState&lt;T&gt;</c> members, reusing the hot-reload driver, fail-hard.
///
/// When the opt-in is absent, <see cref="GenerateMockingMetadata"/> returns an empty string, so the
/// generated output is byte-identical (G5).
/// </summary>
internal partial class ViewModelGenTool_3
{
	private enum FeedKind { ServiceDependent, Derived, Independent }

	private string GenerateMockingMetadata(INamedTypeSymbol model)
	{
		if (!_ctx.IsMockingEnabled())
		{
			return string.Empty; // opt-out → byte-identical output (G5)
		}

		var compilation = _ctx.Context.Compilation;

		// Feed members of the model (name set for derived-detection + iteration).
		var feedMembers = model
			.GetMembers()
			.Where(m => m is IPropertySymbol or IFieldSymbol && !m.IsStatic && m.IsAccessible())
			.Where(m => IsFeedMember(m))
			.ToList();
		var feedMemberNames = new HashSet<string>(feedMembers.Select(m => m.Name), StringComparer.Ordinal);

		// Constructor parameters (services) + a field/property -> parameter map (assignments in ctor bodies).
		var ctorParamNames = new HashSet<string>(StringComparer.Ordinal);
		foreach (var ctor in AccessibleInstanceCtors(model))
		{
			foreach (var p in ctor.Parameters)
			{
				ctorParamNames.Add(p.Name);
			}
		}

		var fieldToParam = BuildFieldToParamMap(model, ctorParamNames, compilation);

		var sb = new StringBuilder();

		// 1) Feed classification.
		foreach (var member in feedMembers)
		{
			var (kind, derivedFrom, services) = ClassifyFeedMember(member, feedMemberNames, ctorParamNames, fieldToParam, compilation, model);

			switch (kind)
			{
				case FeedKind.Derived:
					foreach (var feed in derivedFrom)
					{
						sb.Append($"\r\n[{NS.Config}.FeedDependency(\"{member.Name}\", OnFeed = \"{feed}\")]");
					}
					break;

				case FeedKind.ServiceDependent:
					foreach (var svc in services)
					{
						sb.Append($"\r\n[{NS.Config}.FeedDependency(\"{member.Name}\", OnParameter = \"{svc}\")]");
					}
					break;

				default:
					sb.Append($"\r\n[{NS.Config}.FeedDependency(\"{member.Name}\")]");
					break;
			}
		}

		// 2) Ctor instrumentation — eager parameter dereference (R1).
		var eager = FindEagerCtorParameters(model, ctorParamNames, compilation);
		foreach (var kvp in eager.OrderBy(k => k.Key, StringComparer.Ordinal))
		{
			var members = kvp.Value.Count > 0
				? $", Members = new[] {{ {string.Join(", ", kvp.Value.OrderBy(m => m, StringComparer.Ordinal).Select(m => $"\"{m}\""))} }}"
				: string.Empty;
			sb.Append($"\r\n[{NS.Config}.CtorDependency(\"{kvp.Key}\", Eager = true{members})]");
		}

		return sb.ToString();
	}

	/// <summary>
	/// Emits the view-model mocking seam (spec 013, gated by opt-in). Commands have no
	/// <c>IHotSwapState&lt;T&gt;</c> backing, so the reflection swap (D11) cannot reach them: a dedicated
	/// public <c>__Mock_SetCommand</c> hook lets the external mocking generator override a command
	/// after construction. Fail-hard: an unknown command name throws (strict mocking, like D11).
	///
	/// Construction itself needs NO seam: the generated public constructors + the ambient
	/// <c>MockingService.Enable()</c> scope (D12) already produce a mockable <c>SourceContext</c>
	/// (the bit is captured on the context instance at creation, so a lazy first subscription after
	/// the scope is disposed still wraps).
	/// </summary>
	private string GenerateVmMockingSeam(IEnumerable<IMappedMember> members)
	{
		if (!_ctx.IsMockingEnabled())
		{
			return string.Empty; // opt-out → byte-identical output (G5)
		}

		var commands = members.OfType<CommandFromMethod>().ToList();
		if (commands.Count == 0)
		{
			return string.Empty;
		}

		var cases = commands
			.Select(c => $"case \"{c.Name}\": {c.Name} = command; break;")
			.JoinBy("\r\n");

		return $@"
			[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
			public void __Mock_SetCommand(string name, {NS.Reactive}.IAsyncCommand command)
			{{
				switch (name)
				{{
					{cases}
					default: throw new global::System.ArgumentException($""No mockable command '{{name}}' on this view model."", nameof(name));
				}}
			}}";
	}

	private bool IsFeedMember(ISymbol member)
	{
		var type = member switch
		{
			IPropertySymbol p => p.Type,
			IFieldSymbol f => f.Type,
			_ => null,
		};
		return type is not null && (_ctx.IsFeed(type) || _ctx.IsListFeed(type) || _ctx.IsFeedOfList(type));
	}

	private IEnumerable<IMethodSymbol> AccessibleInstanceCtors(INamedTypeSymbol model)
		=> model.Constructors.Where(c => !c.IsStatic && !c.IsCloneCtor(model) && c.DeclaredAccessibility is not Accessibility.Private);

	/// <summary>
	/// Maps a field/property name to the constructor parameter it is assigned from (e.g. <c>_svc = svc;</c>
	/// or a primary-constructor capture), so a feed body referencing that field is recognized as service-dependent.
	/// </summary>
	private Dictionary<string, string> BuildFieldToParamMap(INamedTypeSymbol model, HashSet<string> ctorParamNames, Compilation compilation)
	{
		var map = new Dictionary<string, string>(StringComparer.Ordinal);

		foreach (var ctor in AccessibleInstanceCtors(model))
		{
			foreach (var syntaxRef in ctor.DeclaringSyntaxReferences)
			{
				var node = syntaxRef.GetSyntax();
				var body = (SyntaxNode?)(node as ConstructorDeclarationSyntax)?.Body
					?? (node as ConstructorDeclarationSyntax)?.ExpressionBody?.Expression;
				if (body is null)
				{
					continue;
				}

				var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
				foreach (var assignment in body.DescendantNodes().OfType<AssignmentExpressionSyntax>())
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

	private (FeedKind kind, List<string> derivedFrom, List<string> services) ClassifyFeedMember(
		ISymbol member,
		HashSet<string> feedMemberNames,
		HashSet<string> ctorParamNames,
		Dictionary<string, string> fieldToParam,
		Compilation compilation,
		INamedTypeSymbol model)
	{
		var derivedFrom = new List<string>();
		var services = new List<string>();
		var seenDerived = new HashSet<string>(StringComparer.Ordinal);
		var seenServices = new HashSet<string>(StringComparer.Ordinal);

		foreach (var body in GetMemberBodies(member, compilation, out var semanticModelByTree))
		{
			var semanticModel = semanticModelByTree(body.SyntaxTree);
			foreach (var id in body.DescendantNodesAndSelf().OfType<SimpleNameSyntax>())
			{
				var symbol = semanticModel.GetSymbolInfo(id).Symbol;
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

				// A field/property assigned from a ctor parameter → service.
				var backingName = symbol switch
				{
					IFieldSymbol f => f.Name,
					IPropertySymbol pr => pr.Name,
					_ => null,
				};
				if (backingName is not null
					&& SymbolEqualityComparer.Default.Equals(symbol.ContainingType, model)
					&& fieldToParam.TryGetValue(backingName, out var paramName))
				{
					if (seenServices.Add(paramName))
					{
						services.Add(paramName);
					}
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

	/// <summary>
	/// Returns the getter/initializer body syntax nodes of a feed member (property expression body,
	/// getter body, or field initializer).
	/// </summary>
	private IEnumerable<SyntaxNode> GetMemberBodies(ISymbol member, Compilation compilation, out Func<SyntaxTree, SemanticModel> semanticModelByTree)
	{
		var cache = new Dictionary<SyntaxTree, SemanticModel>();
		semanticModelByTree = tree =>
		{
			if (!cache.TryGetValue(tree, out var sm))
			{
				cache[tree] = sm = compilation.GetSemanticModel(tree);
			}
			return sm;
		};

		var bodies = new List<SyntaxNode>();
		foreach (var syntaxRef in member.DeclaringSyntaxReferences)
		{
			switch (syntaxRef.GetSyntax())
			{
				case PropertyDeclarationSyntax pds:
					if (pds.ExpressionBody?.Expression is { } exprBody)
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
	/// (member access / invocation receiver) in a ctor body or an instance field/property initializer,
	/// excluding references nested in a lambda / anonymous method / local function (deferred boundary).
	/// </summary>
	private Dictionary<string, HashSet<string>> FindEagerCtorParameters(INamedTypeSymbol model, HashSet<string> ctorParamNames, Compilation compilation)
	{
		var eager = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

		void Mark(string param, string? member)
		{
			if (!eager.TryGetValue(param, out var set))
			{
				eager[param] = set = new HashSet<string>(StringComparer.Ordinal);
			}
			if (member is not null)
			{
				set.Add(member);
			}
		}

		foreach (var ctor in AccessibleInstanceCtors(model))
		{
			foreach (var syntaxRef in ctor.DeclaringSyntaxReferences)
			{
				var node = syntaxRef.GetSyntax();
				var body = (SyntaxNode?)(node as ConstructorDeclarationSyntax)?.Body
					?? (node as ConstructorDeclarationSyntax)?.ExpressionBody?.Expression;
				if (body is null)
				{
					continue;
				}

				var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
				InspectEager(body, semanticModel, ctorParamNames, Mark, enclosingMember: null);
			}
		}

		return eager;
	}

	private void InspectEager(SyntaxNode body, SemanticModel semanticModel, HashSet<string> ctorParamNames, Action<string, string?> mark, string? enclosingMember)
	{
		foreach (var access in body.DescendantNodesAndSelf())
		{
			// The receiver of a member-access / element-access is an eager dereference.
			ExpressionSyntax? receiver = access switch
			{
				MemberAccessExpressionSyntax mae => mae.Expression,
				ElementAccessExpressionSyntax eae => eae.Expression,
				_ => null,
			};
			if (receiver is not IdentifierNameSyntax id)
			{
				continue;
			}

			if (IsInsideDeferredBoundary(receiver, body))
			{
				continue; // lambda/anonymous/local-function body → not eager at construction
			}

			if (semanticModel.GetSymbolInfo(id).Symbol is IParameterSymbol param && ctorParamNames.Contains(param.Name))
			{
				mark(param.Name, enclosingMember);
			}
		}
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
