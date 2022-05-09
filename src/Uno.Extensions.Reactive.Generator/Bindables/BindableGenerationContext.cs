using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Reactive.Bindings;

namespace Uno.Extensions.Reactive.Generator;

internal record BindableGenerationContext(
	GeneratorExecutionContext Context,
	INamedTypeSymbol Feed,
	INamedTypeSymbol Input,
	INamedTypeSymbol ListFeed,
	INamedTypeSymbol CommandBuilder,
	INamedTypeSymbol CommandBuilderOfT,
	INamedTypeSymbol BindableAttribute,
	INamedTypeSymbol InputAttribute,
	INamedTypeSymbol ValueAttribute,
	INamedTypeSymbol DefaultRecordCtor,
	INamedTypeSymbol CancellationToken)
{
	public static BindableGenerationContext? TryGet(GeneratorExecutionContext context, out string? error)
	{
		var compilation = context.Compilation;

		var feed = compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.IFeed`1");
		var input = compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.IInput`1");
		var listFeed = compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.IListFeed`1");
		var commandBuilder = compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.ICommandBuilder");
		var commandBuilderOfT = compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.ICommandBuilder`1");
		var bindable = compilation.GetTypeByMetadataName(typeof(ReactiveBindableAttribute).FullName);
		var inputAttribute = compilation.GetTypeByMetadataName(typeof(InputAttribute).FullName);
		var valueAttribute = compilation.GetTypeByMetadataName(typeof(ValueAttribute).FullName);
		var defaultRecordCtor = compilation.GetTypeByMetadataName(typeof(BindableDefaultConstructorAttribute).FullName);
		var cancellationToken = compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName);

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

			if (listFeed is null)
			{
				yield return "IListFeed";
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

			if (cancellationToken is null)
			{
				yield return typeof(CancellationToken).FullName;
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
			context,
			feed!,
			input!,
			listFeed!,
			commandBuilder!,
			commandBuilderOfT!,
			bindable!,
			inputAttribute!,
			valueAttribute!,
			defaultRecordCtor!,
			cancellationToken!
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
		if (parameter.FindAttribute(InputAttribute)?.ConstructorArguments[0].Value is InputKind inputKind)
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

	public bool IsListFeed(ITypeSymbol type)
		=> type.GetAllInterfaces().Select(intf => intf.OriginalDefinition).Contains(ListFeed, SymbolEqualityComparer.Default);

	public bool IsListFeed(ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? valueType)
	{
		if (type.GetGenericParametersOfInterface(ListFeed).FirstOrDefault() is { IsDefaultOrEmpty: false, Length: 1 } feedTypeParam)
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

	public bool IsListFeed(IParameterSymbol parameter, [NotNullWhen(true)] out ITypeSymbol? valueType, [NotNullWhen(true)] out InputKind? kind)
	{
		var definedKind = default(InputKind?);
		if (parameter.FindAttribute(InputAttribute)?.ConstructorArguments[0].Value is InputKind inputKind)
		{
			definedKind = inputKind;
		}
		else if (parameter.FindAttribute(ValueAttribute) is not null)
		{
			definedKind = InputKind.Value;
		}

		if (parameter.Type.GetGenericParametersOfInterface(Feed).FirstOrDefault() is { IsDefaultOrEmpty: false, Length: 1 } feedTypeParam)
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

	public bool IsAwaitable(ITypeSymbol type)
	{
		// We usually use this IsAwaitable for return type of method.
		if (type.SpecialType == SpecialType.System_Void)
		{
			return false;
		}

		return type.GetMembers("GetAwaiter").Any(IsInstanceGetAwaiter)
			|| Context.Compilation.GetSymbolsWithName("GetAwaiter", SymbolFilter.Member, Context.CancellationToken).Any(IsExtensionGetAwaiter);

		static bool IsInstanceGetAwaiter(ISymbol symbol)
			=> symbol is IMethodSymbol { IsStatic: false, Parameters.Length: 0 } method
				&& IsAwaiter(method.ReturnType);

		bool IsExtensionGetAwaiter(ISymbol symbol)
			=> symbol is IMethodSymbol { IsStatic: true, Parameters.Length: 1 } method
				&& SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, type)
				&& IsAwaiter(method.ReturnType);

		static bool IsAwaiter(ITypeSymbol returnType)
			=> returnType.AllInterfaces.Any(intf => intf.ToString().Equals(typeof(INotifyCompletion).FullName));
	}
}
