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
	private static readonly ConditionalWeakTable<AssemblyIdentity, object> _assemblyToNamesMap = new();
	private static readonly ConditionalWeakTable<SyntaxTree, object> _treeToIsCandidate = new();

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

		/*
		    assemblyNameProvider: The output is the assembly name of the current compilation.

			syntaxTreesProvider: The output is all syntax trees that contain invocations like Method(..., x => x..., ...)

			interestingMethodNames: The output is method names from compilation references that have PropertySelector parameter
			filteredSyntaxTreesProvider: The output is all matching InvocationExpressionSyntax nodes given interestingMethodNames.
			finalProvider: The output is `PropertySelectorCandidate`s that are semantics-aware. This only includes calls to methods from metadata.

			interestingMethodNamesFromSource: The output is method names from current compilation sources that have PropertySelector parameter
			filteredSyntaxTreesFromSource: The output is all matching InvocationExpressionSyntax nodes given interestingMethodNamesFromSource.
			finalProviderFromSource: The output is `PropertySelectorCandidate`s that are semantics-aware. This only includes calls to methods from source.


			    ┌──────────────────────┐
   Compilation  |                      |SyntaxTree[]
  ─────────────►│ syntaxTreesProvider  │────────────┐
			    |                      |            │         ┌───────────────────────────┐
			    └──────────────────────┘            │Combine  |                           |InvocationExpressionSyntax[]
		                                            │────────►│filteredSyntaxTreesProvider│──────────────────────────┐ 
			    ┌──────────────────────┐            │		  |                           |	    		 		     │        ┌─────────────┐ 
   Compilation  |                      |  string[]  │		  └───────────────────────────┘							 │Combine |             |Candidate[]
  ─────────────►│interestingMethodNames│────────────┘																 │───────►│finalProvider│──────────► Then combine with assemblyNameProvider & Generate.
			    |                      |                                                                   			 │		  |             |
			    └──────────────────────┘                                                              Compilation    │		  └─────────────┘
																                              ────────────►──────────┘
																                              


			    ┌───────────────────┐
   Compilation  |                   | SyntaxTree[]
  ─────────────►│syntaxTreesProvider│──────────────────────┐
			    |                   |                      │        ┌─────────────────────────────┐
			    └───────────────────┘                      │Combine |                             |InvocationExpressionSyntax[]
		                                                   │───────►│filteredSyntaxTreesFromSource│───────────────────────────┐ 
			    ┌────────────────────────────────┐         │  	    |                             |		   	       		      │       ┌───────────────────────┐
   Compilation  |                                |string[] │		└─────────────────────────────┘							  │Combine|                       |Candidate[]
  ─────────────►│interestingMethodNamesFromSource│─────────┘																  │──────►│finalProviderFromSource│──────────► Then combine with assemblyNameProvider & Generate.
			    |                                |                                                                 		   	  │		  |                       |
			    └────────────────────────────────┘                                                            Compilation     │		  └───────────────────────┘
																                                      ────────────►───────────┘								                              
		 														                              
		*/
		var assemblyNameProvider = context.CompilationProvider.Select((compilation, _) => compilation.AssemblyName);
		var interestingMethodNames = context.CompilationProvider.SelectMany((compilation, ct) =>
		{
			var propertySelectorSymbol = compilation.GetTypeByMetadataName("Uno.Extensions.Edition.PropertySelector`2");
			var callerLineNumberSymbol = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.CallerLineNumberAttribute");
			var callerFilePathSymbol = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.CallerFilePathAttribute");

			var builder = ImmutableArray.CreateBuilder<string>();
			foreach (var reference in compilation.References)
			{
				if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
				{
					if (!_assemblyToNamesMap.TryGetValue(assembly.Identity, out var interestingNames))
					{
						interestingNames = GetFromNamespaceExpensive(assembly.GlobalNamespace, propertySelectorSymbol, callerFilePathSymbol, callerLineNumberSymbol, ct).ToImmutableArray();
						_assemblyToNamesMap.Add(assembly.Identity, interestingNames);
					}

					foreach (var name in (ImmutableArray<string>)interestingNames)
					{
						builder.Add(name);
					}
				}
			}

			return builder.ToImmutableArray();
		});

		var syntaxTreesProvider = context.CompilationProvider
			.SelectMany((compilation, cancellationToken) => GetInterestingTrees(compilation, cancellationToken));

		// This step combines the method names that can have property selector parameters, with the syntax trees from the previous step.
		// The output here is all matching InvocationExpressionSyntax nodes that might be interesting.
		var filteredSyntaxTreesProvider = CreateFilteredSyntaxTreesProvider(syntaxTreesProvider, interestingMethodNames).WithTrackingName("filteredSyntaxTreesProvider_PropertySelectorGenerator");

		// This is NOT GOOD.
		// This step runs both the predicate and transform on every edit.
		// A better implementation would be a bit much more complex.
		// So we try out this simple approach for now.
		var interestingMethodNamesFromSource = context.SyntaxProvider.CreateSyntaxProvider(
			IsCandidateMethodDeclaration,
			TransformMethodDeclarationToName);

		var filteredSyntaxTreesFromSource = CreateFilteredSyntaxTreesProvider(syntaxTreesProvider, interestingMethodNamesFromSource).WithTrackingName("filteredSyntaxTreesFromSource_PropertySelectorGenerator");

		var finalProvider = filteredSyntaxTreesProvider.Combine(context.CompilationProvider).Select((pair, ct) =>
		{
			var (node, compilation) = pair;
			return new PropertySelectorCandidate(node, compilation.GetSemanticModel(node.SyntaxTree), fromSource: false, ct);
		}).Where(candidate => candidate.IsValid);

		var finalProviderFromSource = filteredSyntaxTreesFromSource.Combine(context.CompilationProvider).Select((pair, ct) =>
		{
			var (node, compilation) = pair;
			return new PropertySelectorCandidate(node, compilation.GetSemanticModel(node.SyntaxTree), fromSource: true, ct);
		}).Where(candidate => candidate.IsValid);

		// We use the Implementation as the generated code does not alter the SemanticModel (only generates a registry).
		context.RegisterImplementationSourceOutput(
			finalProvider.Combine(assemblyNameProvider).WithTrackingName("outputFromMetadata_PropertySelectorGenerator"),
			(context, souce) => _tool.Generate(context, souce.Left, souce.Right));

		context.RegisterImplementationSourceOutput(
			finalProviderFromSource.Combine(assemblyNameProvider).WithTrackingName("outputFromSource_PropertySelectorGenerator"),
			(context, souce) => _tool.Generate(context, souce.Left, souce.Right));
	}

	private static IncrementalValuesProvider<InvocationExpressionSyntax> CreateFilteredSyntaxTreesProvider(IncrementalValuesProvider<SyntaxTree> syntaxTreesProvider, IncrementalValuesProvider<string> interestingMethodNames)
	{
		return syntaxTreesProvider.Combine(interestingMethodNames.Collect()).SelectMany((pair, ct) =>
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
				if (GetCalledMethodName(invocationSyntax) is string calledMethodName && interestingNames.Contains(calledMethodName))
				{
					filteredNodes.Add(invocationSyntax);
				}
			}

			return filteredNodes;
		});

		static string? GetCalledMethodName(InvocationExpressionSyntax node)
		{
			if (node.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
			{
				return memberAccessExpressionSyntax.Name.Identifier.ValueText;
			}
			else if (node.Expression is SimpleNameSyntax identifierNameSyntax)
			{
				return identifierNameSyntax.Identifier.ValueText;
			}

			return null;
		}
	}

	private static string TransformMethodDeclarationToName(GeneratorSyntaxContext context, CancellationToken token)
	{
		return ((MethodDeclarationSyntax)context.Node).Identifier.ValueText;
	}

	private bool IsCandidateMethodDeclaration(SyntaxNode node, CancellationToken token)
	{
		if (node is not MethodDeclarationSyntax { ParameterList.Parameters.Count: >= 3 } methodDeclarationSyntax)
		{
			return false;
		}

		var hasPropertySelectorParameter = false;
		var hasCallerLineNumberParameter = false;
		var hasCallerFilePathParameter = false;
		foreach (var parameter in methodDeclarationSyntax.ParameterList.Parameters)
		{
			var type = GetRightmostName(parameter.Type);

			if (type is GenericNameSyntax genericName && genericName.Identifier.ValueText == "PropertySelector" && genericName.Arity == 2)
			{
				hasPropertySelectorParameter = true;
				continue;
			}

			foreach (var attributeList in parameter.AttributeLists)
			{
				foreach (var attribute in attributeList.Attributes)
				{
					var attributeName = GetRightmostName(attribute.Name);
					if (attributeName is IdentifierNameSyntax identifier)
					{
						if (identifier.Identifier.ValueText is "CallerFilePath" or "CallerFilePathAttribute")
						{
							hasCallerFilePathParameter = true;
						}
						else if (identifier.Identifier.ValueText is "CallerLineNumber" or "CallerLineNumberAttribute")
						{
							hasCallerLineNumberParameter = true;
						}
						
					}
				}
			}
		}

		return hasPropertySelectorParameter && hasCallerLineNumberParameter && hasCallerFilePathParameter;
	}

	private static SimpleNameSyntax? GetRightmostName(TypeSyntax? type)
	{
		if (type is NullableTypeSyntax nullableTypeSyntax)
		{
			type = nullableTypeSyntax.ElementType;
		}

		if (type is AliasQualifiedNameSyntax alias)
		{
			return alias.Name;
		}
		else if (type is QualifiedNameSyntax qualified)
		{
			return qualified.Right;
		}

		return type as SimpleNameSyntax;
	}

	private static ImmutableArray<SyntaxTree> GetInterestingTrees(Compilation compilation, CancellationToken cancellationToken)
	{
		// Mostly a copy from:
		// https://github.com/dotnet/roslyn/blob/2bd3c9891e0d52661f9a7bfba8a8caf2b6430070/src/Compilers/Core/Portable/SourceGeneration/Nodes/SyntaxValueProvider_ForAttributeWithSimpleName.cs#L126-L150

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
			cancellationToken.ThrowIfCancellationRequested();
			if (IsCandidateTree(tree, cancellationToken))
				builder.Add(tree);
		}

		return builder.MoveToImmutable();
	}

	private static bool IsCandidateTree(SyntaxTree tree, CancellationToken ct)
	{
		if (_treeToIsCandidate.TryGetValue(tree, out var candidate))
		{
			return (bool)candidate;
		}

		var root =  (CompilationUnitSyntax)tree.GetRoot(ct);
		foreach (var member in root.Members)
		{
			foreach (var node in member.DescendantNodesAndSelf())
			{
				ct.ThrowIfCancellationRequested();
				if (IsCandidate(node))
				{
					_treeToIsCandidate.Add(tree, true);
					return true;
				}
			}
		}

		_treeToIsCandidate.Add(tree, false);
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

	private static IEnumerable<string> GetFromNamespaceExpensive(INamespaceSymbol @namespace, INamedTypeSymbol? propertySelectorSymbol, INamedTypeSymbol? callerFilePathSymbol, INamedTypeSymbol? callerLineNumberSymbol, CancellationToken ct)
	{
		foreach (var member in @namespace.GetMembers())
		{
			ct.ThrowIfCancellationRequested();
			if (member is INamespaceSymbol innerNamespace)
			{
				foreach (var inner in GetFromNamespaceExpensive(innerNamespace, propertySelectorSymbol, callerFilePathSymbol, callerLineNumberSymbol, ct))
				{
					yield return inner;
				}
			}
			else if (member is INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public })
			{
				foreach (var method in member.GetMembers())
				{
					ct.ThrowIfCancellationRequested();
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
