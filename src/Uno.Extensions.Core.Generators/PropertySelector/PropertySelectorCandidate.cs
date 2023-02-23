using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.Extensions.Generators.PropertySelector;

internal readonly record struct PropertySelectorCandidate
{
	public PropertySelectorCandidate(GeneratorSyntaxContext context, CancellationToken ct)
	{
		IsValid = true;
		Syntax = (InvocationExpressionSyntax)context.Node;
		Location = Syntax.SyntaxTree.GetLineSpan(Syntax.Span);
		
		var method = context.SemanticModel.GetSymbolInfo(Syntax, ct).Symbol as IMethodSymbol;
		
		if (method is null ||
			!method.Parameters.Any(p => p.Type is INamedTypeSymbol
			{
				IsGenericType: true,
				MetadataName: "PropertySelector`2",
				ContainingNamespace: { Name: "Edition", ContainingNamespace: { Name: "Extensions", ContainingNamespace.Name: "Uno" } }
			}))
		{
			IsValid = false;
			Selectors = null;
			MethodGlobalNamespace = null;
			MethodName = null;
			return;
		}

		MethodName = method.Name;
		MethodGlobalNamespace = method.ContainingModule?.GlobalNamespace?.ToString() ?? "";

		var callerPathParameter = method.Parameters.FirstOrDefault(p => p.HasAttribute<CallerFilePathAttribute>());
		var callerLineParameter = method.Parameters.FirstOrDefault(p => p.HasAttribute<CallerLineNumberAttribute>());

		if (callerPathParameter is null || callerLineParameter is null)
		{
			IsValid = false;
			Selectors = null;
			return;
		}

		var arguments = Syntax.ArgumentList.Arguments;

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

		Selectors = method.Parameters
			.Where(param => param.Type is INamedTypeSymbol
			{
				IsGenericType: true,
				MetadataName: "PropertySelector`2",
				ContainingNamespace: { Name: "Edition", ContainingNamespace: { Name: "Extensions", ContainingNamespace.Name: "Uno" } }
			})
			.Select(param => new PropertySelectorUsageItem(
				(INamedTypeSymbol)param.Type,
				arguments.ElementAtOrDefault(param.Ordinal),
				$"{param.Name}{callerPath}{callerLine}"))
			.ToImmutableList();
	}

	[MemberNotNullWhen(true, nameof(Syntax), nameof(Selectors), nameof(MethodGlobalNamespace), nameof(MethodName))]
	public bool IsValid { get; }

	public InvocationExpressionSyntax? Syntax { get; }
	public FileLinePositionSpan Location { get; }
	public string? MethodGlobalNamespace { get; }
	public string? MethodName { get; }
	public ImmutableList<PropertySelectorUsageItem>? Selectors { get; }
}
