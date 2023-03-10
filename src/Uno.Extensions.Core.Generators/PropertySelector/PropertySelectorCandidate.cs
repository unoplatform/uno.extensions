using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.Extensions.Core.Generators.Helpers;
using Uno.Extensions.Edition;

namespace Uno.Extensions.Generators.PropertySelector;

internal readonly record struct PropertySelectorCandidate
{
	public PropertySelectorCandidate(GeneratorSyntaxContext context, CancellationToken ct)
	{
		var syntax = (InvocationExpressionSyntax)context.Node;
		Location = syntax.SyntaxTree.GetLineSpan(syntax.Span);
		
		var method = context.SemanticModel.GetSymbolInfo(syntax, ct).Symbol as IMethodSymbol;
		
		if (method is null ||
			!method.Parameters.Any(p => p.Type is INamedTypeSymbol
			{
				IsGenericType: true,
				MetadataName: "PropertySelector`2",
				ContainingNamespace: { Name: "Edition", ContainingNamespace: { Name: "Extensions", ContainingNamespace.Name: "Uno" } }
			}) ||
			method.Parameters.FirstOrDefault(p => p.HasAttribute<CallerFilePathAttribute>()) is not { } callerPathParameter ||
			method.Parameters.FirstOrDefault(p => p.HasAttribute<CallerLineNumberAttribute>()) is not { } callerLineParameter
			)
		{
			Accessors = null;
			MethodGlobalNamespace = null;
			MethodName = null;
			return;
		}

		var arguments = syntax.ArgumentList.Arguments;

		var callerPathArg = arguments.ElementAtOrDefault(callerPathParameter.Ordinal)?.Expression;
		var callerPath = callerPathArg switch
		{
			LiteralExpressionSyntax pathArg => pathArg.Token.ValueText,
			// It might be something else, but in this case the analyzer will complain.
			_ => Location.Path,
		};

		var callerLineArg = arguments.ElementAtOrDefault(callerLineParameter.Ordinal)?.Expression;
		var callerLine = callerLineArg switch
		{
			LiteralExpressionSyntax lineArg => lineArg.Token.ValueText,
			// It might be something else, but in this case the analyzer will complain.
			_ => (Location.StartLinePosition.Line + 1).ToString(CultureInfo.InvariantCulture),
		};

		MethodName = method.Name;
		MethodGlobalNamespace = method.ContainingModule?.GlobalNamespace?.ToString() ?? "";

		Accessors = method.Parameters
			.Where(param => param.Type is INamedTypeSymbol
			{
				IsGenericType: true,
				MetadataName: "PropertySelector`2",
				ContainingNamespace: { Name: "Edition", ContainingNamespace: { Name: "Extensions", ContainingNamespace.Name: "Uno" } }
			})
			.Select(param =>
				(key: $"{param.Name}{callerPath}{callerLine}", accessor: PropertySelectorsGenerationTool.GenerateAccessor((INamedTypeSymbol)param.Type, arguments.ElementAtOrDefault(param.Ordinal))))
			.Where(a => a.accessor is not null)
			.ToImmutableArray()
			.AsEquatableArray()!;
	}

	[MemberNotNullWhen(true, nameof(Accessors), nameof(MethodGlobalNamespace), nameof(MethodName))]
	public bool IsValid => Accessors is not null;
	public FileLinePositionSpan Location { get; }
	public string? MethodGlobalNamespace { get; }
	public string? MethodName { get; }
	public EquatableArray<(string key, string accessor)>? Accessors { get; }
}
