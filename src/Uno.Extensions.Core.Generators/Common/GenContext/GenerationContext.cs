using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Generators;

internal static class GenerationContext
{
	public static bool IsDisabled(this GeneratorExecutionContext context, string disableGeneratorPropertyName, bool defaultValue = false)
		=> bool.TryParse(context.GetMSBuildPropertyValue(disableGeneratorPropertyName, defaultValue.ToString()), out var isDisabled) && isDisabled;

	public static TContext? TryGet<TContext>(GeneratorExecutionContext context, [NotNullWhen(true)] out string? error)
		where TContext : notnull
	{
		try
		{
			var compilation = context.Compilation;
			var ctor = typeof(TContext)
				.GetConstructors()
				.Single(ctor => (ctor.GetParameters() is { Length: > 1 } parameters && parameters.Skip(1).All(p => p.ParameterType == typeof(INamedTypeSymbol))));

			var arguments = ctor
				.GetParameters()
				.Skip(1) // GeneratorExecutionContext
				.Select(parameter => (parameter, attribute: parameter.GetCustomAttribute<ContextTypeAttribute>()))
				.Where(x => x.attribute is not null)
				.Select(x =>
				(
					x.parameter,
					type: x.attribute.Type,
					isOptional: x.attribute.IsOptional || x.parameter.GetCustomAttributesData().Any(attr => attr.AttributeType.FullName.Equals("System.Runtime.CompilerServices.NullableAttribute")),
					symbol: compilation!.GetTypeByMetadataName(x.attribute.Type)
				))
				.ToList();

			if (arguments
					.Where(arg => arg is {symbol: null, isOptional: false})
					.ToList() is { Count: > 0 } missingArgs)
			{
				error = $"Failed to resolve types {missingArgs.Select(arg => arg.type).JoinBy(", ")}";
				return default;
			}

			error = null;
			return (TContext)ctor.Invoke(new object[] { context }.Concat(arguments.Select(arg => arg.symbol)).ToArray());
		}
		catch (Exception ex)
		{
			error = "Failed to initialize key equality generation context.\r\n" + ex;
			return default;
		}
	}
}
