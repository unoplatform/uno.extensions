using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.Extensions.Generators.PropertySelector;

internal static class PropertySelectorUsageResolver
{
	public static PropertySelectorUsage? FindUsage(PropertySelectorCandidate candidate)
	{
		if (!candidate.IsValid)
		{
			return null;
		}

		var callerPathArgIndex = candidate
			.Method
			.Parameters
			.Select((param, index) => (isCallerFile: param.HasAttribute<CallerFilePathAttribute>(), index: (int?)index))
			.FirstOrDefault(p => p.isCallerFile)
			.index;
		var callerLineArgIndex = candidate
			.Method
			.Parameters
			.Select((param, index) => (isCallerLine: param.HasAttribute<CallerLineNumberAttribute>(), index: (int?)index))
			.FirstOrDefault(p => p.isCallerLine)
			.index;

		if (callerPathArgIndex is null || callerLineArgIndex is null)
		{
			// Breaks PS0101, but we don't throw as the issue is on the method declaration, not its usage!
			return default;
		}

		var arguments = candidate.Syntax.ArgumentList.Arguments;

		var callerPathArg = arguments.ElementAtOrDefault(callerPathArgIndex.Value)?.Expression;
		var callerPath = callerPathArg switch
		{
			null => candidate.Location.Path,
			LiteralExpressionSyntax pathArg => pathArg.Token.ValueText,
			_ => throw Rules.PS0102.FailFileArg(candidate.Method, candidate.Method.Parameters.ElementAtOrDefault(callerPathArgIndex.Value), callerPathArg),
		};
		var callerLineArg = arguments.ElementAtOrDefault(callerLineArgIndex.Value)?.Expression;
		var callerLine = callerLineArg switch
		{
			null => (candidate.Location.StartLinePosition.Line + 1).ToString(CultureInfo.InvariantCulture),
			LiteralExpressionSyntax lineArg => lineArg.Token.ValueText,
			_ => throw Rules.PS0102.FailLineArg(candidate.Method, candidate.Method.Parameters.ElementAtOrDefault(callerLineArgIndex.Value), callerLineArg),
		};

		var selectors = candidate
			.Method
			.Parameters
			.Select((param, index) =>
			(
				param,
				index,
				isPropertySelector: param.Type is INamedTypeSymbol
				{
					IsGenericType: true,
					MetadataName: "PropertySelector`2",
					ContainingNamespace: { Name: "Edition", ContainingNamespace: { Name: "Extensions", ContainingNamespace.Name: "Uno" } }
				}
			))
			.Where(param => param.isPropertySelector)
			.Select(param => new PropertySelectorUsageItem(
				param.param,
				arguments.ElementAtOrDefault(param.index),
				$"{param.param.Name}{callerPath}{callerLine}"))
			.ToImmutableList();

		return new(candidate.Method, candidate.Location, selectors);
	}
}
