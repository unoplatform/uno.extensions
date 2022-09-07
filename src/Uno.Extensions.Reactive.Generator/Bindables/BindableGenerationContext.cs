using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Tags;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Config;
using Uno.Extensions.Reactive.Commands;

namespace Uno.Extensions.Reactive.Generator;

internal record BindableGenerationContext(
	GeneratorExecutionContext Context,

	// Core types
	INamedTypeSymbol Feed,
	INamedTypeSymbol Input,
	INamedTypeSymbol ListFeed,
	INamedTypeSymbol CommandBuilder,
	INamedTypeSymbol CommandBuilderOfT,

	// Generation config attributes
	INamedTypeSymbol ImplicitCommandsAttribute,
	INamedTypeSymbol ImplicitCommandParametersAttribute,

	// Bindable attributes
	INamedTypeSymbol BindableAttribute,
	INamedTypeSymbol InputAttribute,
	INamedTypeSymbol ValueAttribute,
	INamedTypeSymbol DefaultCtorAttribute,

	// Commands attributes
	INamedTypeSymbol CommandAttribute,
	INamedTypeSymbol CommandParameterAttribute,

	// General stuff types
	INamedTypeSymbol CancellationToken,
	INamedTypeSymbol ImmutableArray,
	INamedTypeSymbol ImmutableList,
	INamedTypeSymbol ImmutableQueue,
	INamedTypeSymbol ImmutableSet,
	INamedTypeSymbol ImmutableStack)
{
	public static BindableGenerationContext? TryGet(GeneratorExecutionContext context, out string? error)
	{
		var compilation = context.Compilation;

		INamedTypeSymbol? feed = default, input = default, listFeed = default, commandBuilder = default, commandBuilderOfT = default;
		INamedTypeSymbol? implicitCommandsAttribute = default, implicitCommandParametersAttribute = default;
		INamedTypeSymbol? bindableAttribute = default, inputAttribute = default, valueAttribute = default, defaultCtorAttribute = default;
		INamedTypeSymbol? commandAttribute = default, commandParameterAttribute = default;
		INamedTypeSymbol? cancellationToken = default, immutableArray = default, immutableList = default, immutableQueue = default, immutableSet = default, immutableStack = default;

		IEnumerable<string?> Resolve()
		{
			yield return ByName("Uno.Extensions.Reactive.IFeed`1", out feed);
			yield return ByName("Uno.Extensions.Reactive.IInput`1", out input);
			yield return ByName("Uno.Extensions.Reactive.IListFeed`1", out listFeed);
			yield return ByName("Uno.Extensions.Reactive.ICommandBuilder", out commandBuilder);
			yield return ByName("Uno.Extensions.Reactive.ICommandBuilder`1", out commandBuilderOfT);

			yield return ByType(typeof(ImplicitCommandsAttribute), out implicitCommandsAttribute);
			yield return ByType(typeof(ImplicitFeedCommandParametersAttribute), out implicitCommandParametersAttribute);

			yield return ByType(typeof(ReactiveBindableAttribute), out bindableAttribute);
			yield return ByType(typeof(InputAttribute), out inputAttribute);
			yield return ByType(typeof(ValueAttribute), out valueAttribute);
			yield return ByType(typeof(BindableDefaultConstructorAttribute), out defaultCtorAttribute);

			yield return ByType(typeof(CommandAttribute), out commandAttribute);
			yield return ByType(typeof(FeedParameterAttribute), out commandParameterAttribute);

			yield return ByType(typeof(CancellationToken), out cancellationToken);
			yield return ByType(typeof(ImmutableArray<>), out immutableArray);
			yield return ByType(typeof(IImmutableList<>), out immutableList);
			yield return ByType(typeof(IImmutableQueue<>), out immutableQueue);
			yield return ByType(typeof(IImmutableSet<>), out immutableSet);
			yield return ByType(typeof(IImmutableStack<>), out immutableStack);
		}

		string? ByType(Type type, out INamedTypeSymbol symbol)
			=> ByName(type.FullName, out symbol);

		string? ByName(string type, out INamedTypeSymbol symbol)
		{
			symbol = compilation!.GetTypeByMetadataName(type)!;
			return symbol is null ? type : null;
		}

		if (Resolve().Where(missing => missing is not null).ToList() is { Count: > 0 } missings)
		{
			error = $"Failed to resolve {missings.JoinBy(", ")}";
			return null;
		}

		error = null;
		return new BindableGenerationContext
		(
			context,
			feed!, input!, listFeed!, commandBuilder!, commandBuilderOfT!,
			implicitCommandsAttribute!, implicitCommandParametersAttribute!,
			bindableAttribute!, inputAttribute!, valueAttribute!, defaultCtorAttribute!,
			commandAttribute!, commandParameterAttribute!,
			cancellationToken!, immutableArray!, immutableList!, immutableQueue!, immutableSet!, immutableStack!
		);
	}

	public bool IsGenerationNotDisable(ISymbol symbol)
		=> IsGenerationEnabled(symbol) ?? true;

	public bool? IsGenerationEnabled(ISymbol symbol)
		=> symbol.FindAttributeValue<bool>(BindableAttribute, nameof(ReactiveBindableAttribute.IsEnabled), 0) is { isDefined: true } attribute
			? attribute.value ?? true
			: null;

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

	public bool IsFeedOfList(ITypeSymbol type)
		=> IsFeed(type, out var listType) && IsKindOfImmutableList(listType, out _);

	public bool IsFeedOfList(ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? collectionType, [NotNullWhen(true)] out ITypeSymbol? itemType)
	{
		itemType = null;
		return IsFeed(type, out collectionType) && IsKindOfImmutableList(collectionType, out itemType);
	}

	public bool IsFeedOfList(IParameterSymbol parameter, [NotNullWhen(true)] out ITypeSymbol? collectionType, [NotNullWhen(true)] out ITypeSymbol? itemType, [NotNullWhen(true)] out InputKind? kind)
	{
		itemType = null;
		return IsFeed(parameter, out collectionType, out kind) && IsKindOfImmutableList(collectionType, out itemType);
	}

	private bool IsKindOfImmutableList(ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? itemType)
	{
		if (type is IArrayTypeSymbol array)
		{
			itemType = array.ElementType;
			return true;
		}
		if (type.IsOrImplements(ImmutableArray, out var immutableArray))
		{
			itemType = immutableArray.TypeArguments.Single();
			return true;
		}
		if (type.IsOrImplements(ImmutableList, out var immutableList))
		{
			itemType = immutableList.TypeArguments.Single();
			return true;
		}
		if (type.IsOrImplements(ImmutableQueue, out var immutableQueue))
		{
			itemType = immutableQueue.TypeArguments.Single();
			return true;
		}
		if (type.IsOrImplements(ImmutableSet, out var immutableSet))
		{
			itemType = immutableSet.TypeArguments.Single();
			return true;
		}
		if (type.IsOrImplements(ImmutableStack, out var immutableStack))
		{
			itemType = immutableStack.TypeArguments.Single();
			return true;
		}

		itemType = null;
		return false;
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

	public bool IsAwaitable(IMethodSymbol method)
		=> method
			.DeclaringSyntaxReferences
			.Select(syntaxRef => syntaxRef.GetSyntax(Context.CancellationToken))
			.OfType<MethodDeclarationSyntax>()
			.Any(syntax => syntax.Modifiers.Any(SyntaxKind.AsyncKeyword))
			|| IsAwaitable(method.ReturnType);

	public bool IsAwaitable(ITypeSymbol type)
	{
		// We usually use this IsAwaitable for return type of method.
		if (type.SpecialType == SpecialType.System_Void)
		{
			return false;
		}

		// Fast path to avoid lookup into all types
		var typeStr = type.ToString();
		if (typeStr.StartsWith(_task, StringComparison.Ordinal)
			|| typeStr.StartsWith(_valueTask, StringComparison.Ordinal))
		{
			return true;
		}

		lock (_isAwaitableCache)
		{
			if (_isAwaitableCache.TryGetValue(type, out var isAwaitable))
			{
				return isAwaitable;
			}

			isAwaitable = type.GetMembers(WellKnownMemberNames.GetAwaiter).Any(IsInstanceGetAwaiter)
				|| Context.Compilation.GetSymbolsWithName(WellKnownMemberNames.GetAwaiter, SymbolFilter.Member, Context.CancellationToken).Any(IsExtensionGetAwaiter);

			_isAwaitableCache[type] = isAwaitable;

			return isAwaitable;
		}

		static bool IsInstanceGetAwaiter(ISymbol symbol)
			=> symbol is IMethodSymbol { IsStatic: false, Parameters.Length: 0 } method
				&& IsAwaiter(method.ReturnType);

		bool IsExtensionGetAwaiter(ISymbol symbol)
			=> symbol is IMethodSymbol { IsStatic: true, Parameters.Length: 1 } method
				&& SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, type)
				&& IsAwaiter(method.ReturnType);

		static bool IsAwaiter(ITypeSymbol returnType)
			=> returnType.AllInterfaces.Any(intf => intf.ToString().Equals(_notifyCompletion));
	}

	public IMethodSymbol? GetDefaultCtor(INamedTypeSymbol type)
	{
		return type
			.Constructors
			.Where(ctor => ctor.IsAccessible() && !ctor.IsCloneCtor(type))
			.OrderBy(ctor => ctor.HasAttributes(DefaultCtorAttribute) ? 0 : 1)
			.ThenBy(ctor => ctor.Parameters.Length)
			.FirstOrDefault();
	}

#pragma warning disable RS1024 // Compare symbols correctly => FALSE POSITIVE
	private static readonly Dictionary<ITypeSymbol, bool> _isAwaitableCache = new(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly
	private static readonly string _notifyCompletion = typeof(INotifyCompletion).FullName;
	private static readonly string _task = typeof(Task).FullName;
	private static readonly string _valueTask = typeof(ValueTask).FullName;
}
