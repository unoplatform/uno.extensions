using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.Extensions.Generators.PropertySelector;

internal readonly record struct PropertySelectorCandidate
{
	public PropertySelectorCandidate(GeneratorSyntaxContext context, CancellationToken ct)
	{
		var syntax = context.Node as InvocationExpressionSyntax;
		var position = syntax is null ? default : syntax.SyntaxTree.GetLineSpan(syntax.Span);
		var method = syntax is null
			? null
			: context.SemanticModel.GetSymbolInfo(syntax, ct).Symbol as IMethodSymbol;
		var hasPropertySelector = method?.Parameters.Any(p => p.Type is INamedTypeSymbol
		{
			IsGenericType: true,
			MetadataName: "PropertySelector`2",
			ContainingNamespace: { Name: "Edition", ContainingNamespace: { Name: "Extensions", ContainingNamespace.Name: "Uno" } }
		});

		Context = context;
		Syntax = syntax;
		Location = position;
		Method = method;
		IsValid = hasPropertySelector ?? false;
	}

	[MemberNotNullWhen(true, nameof(Syntax), nameof(Method))]
	public bool IsValid { get; }

	public GeneratorSyntaxContext Context { get; }

	public InvocationExpressionSyntax? Syntax { get; }
	public FileLinePositionSpan Location { get; }

	public IMethodSymbol? Method { get; }
}
