using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Reactive.Bindings;
using Uno.SourceGeneration;

namespace Uno.Extensions.Reactive.Generator;

internal record BindableGenerationContext(
	INamedTypeSymbol Feed,
	INamedTypeSymbol Input,
	INamedTypeSymbol CommandBuilder,
	INamedTypeSymbol CommandBuilderOfT,
	INamedTypeSymbol BindableAttribute,
	INamedTypeSymbol InputAttribute,
	INamedTypeSymbol ValueAttribute,
	INamedTypeSymbol DefaultRecordCtor)
{
	public static BindableGenerationContext? TryGet(Uno.SourceGeneration.GeneratorExecutionContext context, out string? error)
	{
		var compilation = context.Compilation;

		var feed = compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.IFeed`1");
		var input = compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.IInput`1");
		var commandBuilder = compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.ICommandBuilder");
		var commandBuilderOfT = compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.ICommandBuilder`1");
		var bindable = compilation.GetTypeByMetadataName(typeof(ReactiveBindableAttribute).FullName);
		var inputAttribute = compilation.GetTypeByMetadataName(typeof(InputAttribute).FullName);
		var valueAttribute = compilation.GetTypeByMetadataName(typeof(ValueAttribute).FullName);
		var defaultRecordCtor = compilation.GetTypeByMetadataName(typeof(BindableDefaultConstructorAttribute).FullName);

		IEnumerable<string> Missing()
		{
			if (feed is null)
			{
				yield return "IFeed";
			}

			if (input is null)
			{
				yield return "IInput";
			}

			if (commandBuilder is null)
			{
				yield return "ICommandBuilder";
			}

			if (commandBuilderOfT is null)
			{
				yield return "ICommandBuilder<T>";
			}

			if (bindable is null)
			{
				yield return typeof(ReactiveBindableAttribute).FullName;
			}

			if (inputAttribute is null)
			{
				yield return typeof(InputAttribute).FullName;
			}

			if (valueAttribute is null)
			{
				yield return typeof(ValueAttribute).FullName;
			}

			if (defaultRecordCtor is null)
			{
				yield return typeof(BindableDefaultConstructorAttribute).FullName;
			}
		}

		if (Missing().ToList() is { Count: > 0 } missings)
		{
			error = $"Failed to resolve {missings.JoinBy(", ")}";
			return null;
		}

		error = null;
		return new BindableGenerationContext
		(
			feed!,
			input!,
			commandBuilder!,
			commandBuilderOfT!,
			bindable!,
			inputAttribute!,
			valueAttribute!,
			defaultRecordCtor!
		);
	}

	public bool IsGenerationNotDisable(ISymbol symbol)
		=> IsGenerationEnabled(symbol) ?? true;

	public bool? IsGenerationEnabled(ISymbol symbol)
	{
		var generatorAttribute = symbol.FindAttribute(BindableAttribute);
		if (generatorAttribute is null)
		{
			return null;
		}
		else if (generatorAttribute
			.NamedArguments
			.FirstOrDefault(kvp => kvp.Key.Equals(nameof(ReactiveBindableAttribute.IsEnabled), StringComparison.OrdinalIgnoreCase))
			.Value is { IsNull: false } namedArg)
		{
			return (bool)namedArg.Value!;
		}
		else if (generatorAttribute
			.ConstructorArguments
			.ElementAtOrDefault(0) is { IsNull: false } ctorArg)
		{
			return (bool)ctorArg.Value!;
		}
		else
		{
			return true;
		}
	}

	public bool IsFeed(ITypeSymbol type)
		=> type.GetAllInterfaces().Select(intf => intf.OriginalDefinition).Contains(Feed, SymbolEqualityComparer.Default);

	public bool IsFeed(ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? valueType)
	{
		if (type.GetGenericParametersOfInterface(Feed).FirstOrDefault() is { IsDefaultOrEmpty: false, Length: 1 } feedTypeParam)
		{
			valueType = feedTypeParam.Single();
			return true;
		}
		else
		{
			valueType = null;
			return false;
		}
	}

	public bool IsFeed(IParameterSymbol parameter, [NotNullWhen(true)] out ITypeSymbol? valueType, [NotNullWhen(true)] out InputKind? kind)
	{
		var definedKind = default(InputKind?);
		if (parameter.FindAttribute(Input)?.ConstructorArguments[0].Value is InputKind inputKind)
		{
			definedKind = inputKind;
		}
		else if (parameter.FindAttribute(ValueAttribute) is not null)
		{
			definedKind = InputKind.Value;
		}

		if (parameter.Type.GetGenericParametersOfInterface(Input).FirstOrDefault() is { IsDefaultOrEmpty: false, Length: 1 } inputTypeParam)
		{
			valueType = inputTypeParam.Single();
			kind = definedKind ?? InputKind.Edit;
			return true;
		}
		else if (parameter.Type.GetGenericParametersOfInterface(Feed).FirstOrDefault() is { IsDefaultOrEmpty: false, Length: 1 } feedTypeParam)
		{
			valueType = feedTypeParam.Single();
			kind = definedKind ?? InputKind.External; // If not defined, Feed<T> as considered as external, unlike inputs which are by default Edit
			return true;
		}
		else
		{
			valueType = null;
			kind = default;
			return false;
		}
	}

	public bool IsInputOrCommand(ITypeSymbol type)
		=> type.GetAllInterfaces().Any(intf =>
			SymbolEqualityComparer.Default.Equals(intf.OriginalDefinition, Input)
			|| SymbolEqualityComparer.Default.Equals(intf.OriginalDefinition, CommandBuilder)
			|| SymbolEqualityComparer.Default.Equals(intf.OriginalDefinition, CommandBuilderOfT));

	public bool IsCommand(ITypeSymbol type, out ITypeSymbol? parameterType)
	{
		if (SymbolEqualityComparer.Default.Equals(type, CommandBuilder))
		{
			parameterType = null;
			return true;
		}
		else if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, CommandBuilderOfT))
		{
			parameterType = ((INamedTypeSymbol)type).TypeArguments.Single();
			return true;
		}
		else
		{
			parameterType = null;
			return false;
		}
	}
}
