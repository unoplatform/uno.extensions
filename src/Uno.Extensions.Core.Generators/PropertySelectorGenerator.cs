using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.Extensions.Generators.PropertySelector;

namespace Uno.Extensions.Core.Generators;

/// <summary>
/// A generator that generates IKeyEquatable implementation.
/// </summary>
[Generator]
public partial class PropertySelectorGenerator : IIncrementalGenerator
{
	private readonly PropertySelectorsGenerationTool _tool;
	private static readonly ConditionalWeakTable<AssemblyIdentity, object> s_assemblyToNamesMap = new();
	private static readonly ConditionalWeakTable<SyntaxTree, object> s_TreeToIsCandidate = new();

	/// <summary>
	/// Creates a new instance of the PropertySelectorGenerator
	/// </summary>
	public PropertySelectorGenerator()
	{
		_tool = new PropertySelectorsGenerationTool();
	}

	/// <inheritdoc />
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
#if DEBUGGING_GENERATOR
		var process = Process.GetCurrentProcess().ProcessName;
		if (process.IndexOf("VBCSCompiler", StringComparison.OrdinalIgnoreCase) is not -1
			|| process.IndexOf("csc", StringComparison.OrdinalIgnoreCase) is not -1)
		{
			Debugger.Launch();
		}
#endif
		var assemblyNameProvider = context.CompilationProvider.Select((compilation, _) => compilation.AssemblyName);
		var interestingMethodNames = context.CompilationProvider.SelectMany((compilation, _) =>
		{
			var propertySelectorSymbol = compilation.GetTypeByMetadataName("Uno.Extensions.Edition.PropertySelector`2");
			var callerLineNumberSymbol = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.CallerLineNumberAttribute");
			var callerFilePathSymbol = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.CallerFilePathAttribute");

			var builder = ImmutableArray.CreateBuilder<string>();
			foreach (var reference in compilation.References)
			{
				if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
				{
					if (!s_assemblyToNamesMap.TryGetValue(assembly.Identity, out var interestingNames))
					{
						interestingNames = GetFromNamespaceExpensive(assembly.GlobalNamespace, propertySelectorSymbol, callerFilePathSymbol, callerLineNumberSymbol).ToImmutableArray();
						s_assemblyToNamesMap.Add(assembly.Identity, interestingNames);
					}

					builder.AddRange((ImmutableArray<string>)interestingNames);
				}
			}

			return builder.ToImmutableArray();
		});

		// Create a provider over all the syntax trees in the compilation.  This is better than CreateSyntaxProvider as
		// using SyntaxTrees is purely syntax and will not update the incremental node for a tree when another tree is
		// changed. CreateSyntaxProvider will have to rerun all incremental nodes since it passes along the
		// SemanticModel, and that model is updated whenever any tree changes (since it is tied to the compilation).
		var syntaxTreesProvider = context.CompilationProvider
			.SelectMany((compilation, cancellationToken) => GetInterestingTrees(compilation, cancellationToken));

		var filteredSyntaxTreesProvider = syntaxTreesProvider.Combine(interestingMethodNames.Collect()).SelectMany((pair, ct) =>
		{
			var (tree, interestingNames) = pair;
			var filteredNodes = ImmutableArray.CreateBuilder<InvocationExpressionSyntax>();
			foreach (var node in tree.GetRoot().DescendantNodes())
			{
				if (!IsCandidate(node))
				{
					continue;
				}

				var invocationSyntax = (InvocationExpressionSyntax)node;
				if (invocationSyntax.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: string calledMethodName } &&
					interestingNames.Contains(calledMethodName))
				{
					filteredNodes.Add(invocationSyntax);
				}
			}

			return filteredNodes;
		});

		var finalProvider = filteredSyntaxTreesProvider.Combine(context.CompilationProvider).Select((pair, ct) =>
		{
			var (node, compilation) = pair;
			return new PropertySelectorCandidate(node, compilation.GetSemanticModel(node.SyntaxTree), ct);
		}).WithTrackingName("syntaxProvider_PropertySelectorGenerator").Where(candidate => candidate.IsValid);

		// We use the Implementation as the generated code does not alter the SemanticModel (only generates a registry).
		context.RegisterImplementationSourceOutput(
			finalProvider.Combine(assemblyNameProvider).WithTrackingName("combinedProvider_PropertySelectorGenerator"),
			(context, souce) => _tool.Generate(context, souce.Left, souce.Right));
	}

	private static ImmutableArray<SyntaxTree> GetInterestingTrees(Compilation compilation, CancellationToken cancellationToken)
	{
		// Get the count up front so we can allocate without waste.
		var count = 0;
		foreach (var tree in compilation.SyntaxTrees)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (IsCandidateTree(tree, cancellationToken))
				count++;
		}

		var builder = ImmutableArray.CreateBuilder<SyntaxTree>(count);

		// Iterate again.  This will be free as the values from before will already be cached on the syntax tree.
		foreach (var tree in compilation.SyntaxTrees)
		{
			if (IsCandidateTree(tree, cancellationToken))
				builder.Add(tree);
		}

		return builder.MoveToImmutable();
	}

	private static bool IsCandidateTree(SyntaxTree tree, CancellationToken ct)
	{
		if (s_TreeToIsCandidate.TryGetValue(tree, out var candidate))
		{
			return (bool)candidate;
		}

		var root =  (CompilationUnitSyntax)tree.GetRoot(ct);
		foreach (var member in root.Members)
		{
			foreach (var node in member.DescendantNodesAndSelf())
			{
				if (IsCandidate(node))
				{
					s_TreeToIsCandidate.Add(tree, true);
					return true;
				}
			}
		}

		s_TreeToIsCandidate.Add(tree, false);
		return false;
	}

	internal static bool IsCandidate(IMethodSymbol method, INamedTypeSymbol? propertySelectorSymbol, INamedTypeSymbol? callerFilePathSymbol, INamedTypeSymbol? callerLineNumberSymbol)
	{
		bool foundPropertySelector = false;
		bool foundCallerFilePath = false;
		bool foundCallerLineNumber = false;
		foreach (var parameter in method.Parameters)
		{
			if (parameter.Type.OriginalDefinition.Equals(propertySelectorSymbol, SymbolEqualityComparer.Default))
			{
				foundPropertySelector = true;
			}
			else if (parameter.FindAttribute(callerFilePathSymbol) is not null)
			{
				foundCallerFilePath = true;
			}
			else if (parameter.FindAttribute(callerLineNumberSymbol) is not null)
			{
				foundCallerLineNumber = true;
			}
		}

		return foundPropertySelector && foundCallerFilePath && foundCallerLineNumber;
	}

	private static IEnumerable<string> GetFromNamespaceExpensive(INamespaceSymbol @namespace, INamedTypeSymbol? propertySelectorSymbol, INamedTypeSymbol? callerFilePathSymbol, INamedTypeSymbol? callerLineNumberSymbol)
	{
		foreach (var member in @namespace.GetMembers())
		{
			if (member is INamespaceSymbol innerNamespace)
			{
				foreach (var inner in GetFromNamespaceExpensive(innerNamespace, propertySelectorSymbol, callerFilePathSymbol, callerLineNumberSymbol))
				{
					yield return inner;
				}
			}
			else if (member is INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public })
			{
				foreach (var method in member.GetMembers())
				{
					if (method is IMethodSymbol methodSymbol)
					{
						if (IsCandidate(methodSymbol, propertySelectorSymbol, callerFilePathSymbol, callerLineNumberSymbol))
						{
							yield return methodSymbol.Name;
						}
					}
				}
			}
		}
	}

	private static bool IsCandidate(SyntaxNode node)
	{
		if (node is InvocationExpressionSyntax invocationExpression)
		{
			foreach (var arg in invocationExpression.ArgumentList.Arguments)
			{
				if (arg.Expression is SimpleLambdaExpressionSyntax simpleLambda && IsValidLambda(simpleLambda))
				{
					return true;
				}
			}
		}

		return false;

		static bool IsValidLambda(SimpleLambdaExpressionSyntax simpleLambda)
		{
			return simpleLambda is { Parameter: { IsMissing: false, Identifier.ValueText.Length: > 0 }, ExpressionBody.IsMissing: false };
		}
	}
}
