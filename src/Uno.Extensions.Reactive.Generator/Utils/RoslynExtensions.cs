using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.Extensions.Reactive.Generator;

internal static class RoslynExtensions
{
	public static IEnumerable<INamedTypeSymbol> GetNamespaceTypes(this INamespaceSymbol sym)
	{
		foreach (var child in sym.GetTypeMembers())
		{
			yield return child;
		}

		foreach (var ns in sym.GetNamespaceMembers())
		{
			foreach (var child2 in GetNamespaceTypes(ns))
			{
				yield return child2;
			}
		}
	}

	public static IEnumerable<INamedTypeSymbol> GetNamespaceTypes(this IModuleSymbol module)
	{
		foreach (var type in module.GlobalNamespace.GetNamespaceTypes())
		{
			yield return type;

			foreach (var inner in type.GetTypeMembers())
			{
				yield return inner;
			}
		}
	}

	public static IEnumerable<ImmutableArray<ITypeSymbol>> GetGenericParametersOfInterface(this ITypeSymbol type, INamedTypeSymbol @interface)
	{
		if (@interface is null)
		{
			throw new ArgumentNullException(nameof(@interface));
		}
			
		if (!@interface.IsGenericType)
		{
			throw new InvalidOperationException($"Interface {@interface} is not generic.");
		}

		if (!@interface.IsInterface())
		{
			throw new InvalidOperationException($"Type {@interface} is not an interface.");
		}

		return type
			.GetAllInterfaces()
			.Where(intf => intf.OriginalDefinition.Is(@interface) && intf.IsGenericType && !intf.IsUnboundGenericType)
			.Select(intf => intf.TypeArguments);
	}

	/// <summary>
	/// Determines if the symbol inherits from the specified type.
	/// </summary>
	public static bool IsOrImplements(this ITypeSymbol symbol, INamedTypeSymbol typeOrInterface, [NotNullWhen(true)] out INamedTypeSymbol? boundedType)
		=> IsOrImplements(symbol, typeOrInterface, true, out boundedType);

	/// <summary>
	/// Determines if the symbol inherits from the specified type.
	/// </summary>
	public static bool IsOrImplements(this ITypeSymbol symbol, INamedTypeSymbol typeOrInterface, bool allowBaseTypes, [NotNullWhen(true)] out INamedTypeSymbol? boundedType)
	{
		do
		{
			if (symbol is INamedTypeSymbol named)
			{
				if (IsExpectedType(named))
				{
					boundedType = named;
					return true;
				}
				else if (named.Interfaces.FirstOrDefault(IsExpectedType) is { } concreteType)
				{
					boundedType = concreteType;
					return true;
				}
			}

			if (!allowBaseTypes)
			{
				break;
			}

			symbol = symbol.BaseType!;
			if (symbol is null)
			{
				break;
			}

		} while (symbol.Name != "Object");

		boundedType = null;
		return false;

		bool IsExpectedType(INamedTypeSymbol named)
			=> SymbolEqualityComparer.Default.Equals(named, typeOrInterface)
			|| SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, typeOrInterface);
	}

	/// <summary>
	/// Determines if the symbol inherits from the specified type.
	/// </summary>
	public static bool Is(this ITypeSymbol symbol, INamedTypeSymbol typeOrInterface, bool allowBaseTypes, [NotNullWhen(true)] out INamedTypeSymbol? boundedType)
	{
		do
		{
			if (symbol is INamedTypeSymbol named)
			{
				if (IsExpectedType(named))
				{
					boundedType = named;
					return true;
				}
			}

			if (!allowBaseTypes)
			{
				break;
			}

			symbol = symbol.BaseType!;
			if (symbol is null)
			{
				break;
			}

		} while (symbol.Name != "Object");

		boundedType = null;
		return false;

		bool IsExpectedType(INamedTypeSymbol named)
			=> SymbolEqualityComparer.Default.Equals(named, typeOrInterface)
			|| SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, typeOrInterface);
	}

	public static IMethodSymbol? FindLocalImplementationOf(this INamedTypeSymbol type, IMethodSymbol boundedInterfaceMethod, SymbolEqualityComparer? comparer = null)
		=> type.GetMethods().FirstOrDefault(m => m.IsImplementationOf(boundedInterfaceMethod, comparer));

	public static bool IsImplementationOf(this IMethodSymbol method, IMethodSymbol boundedInterfaceMethod, SymbolEqualityComparer? comparer = null)
		=> method is { MethodKind: MethodKind.ExplicitInterfaceImplementation }
			? method.ExplicitInterfaceImplementations.Contains(boundedInterfaceMethod)
			: method.Name.Equals(boundedInterfaceMethod.Name, StringComparison.Ordinal)
				&& method.Parameters.Length == boundedInterfaceMethod.Parameters.Length
				&& method.Parameters
					.Select((param, i) => (expected: boundedInterfaceMethod.Parameters[i], actual: param))
					.All(parameters => (comparer ?? SymbolEqualityComparer.IncludeNullability).Equals(parameters.expected.Type, parameters.actual.Type))
				&& (comparer ?? SymbolEqualityComparer.IncludeNullability).Equals(method.ReturnType, boundedInterfaceMethod.ReturnType);

	public static string GetDeclaringLocationsDisplayString(this ISymbol symbol)
		=> symbol
			.DeclaringSyntaxReferences
			.Select(@ref => $"{@ref.SyntaxTree.FilePath}@{@ref.SyntaxTree.GetLineSpan(@ref.Span).StartLinePosition}")
			.JoinBy(", ");

	public static string GetPascalCaseName(this ISymbol symbol)
	{
		var name = symbol.Name.ToArray();
		name[0] = char.ToUpperInvariant(name[0]);
		return new string(name);
	}

	public static string GetCamelCaseName(this ISymbol symbol)
	{
		var name = symbol.Name.ToArray();
		name[0] = char.ToLowerInvariant(name[0]);
		return new string(name);
	}


	public static bool IsAccessible(this ISymbol symbol)
		=> symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;


	/// <summary>
	/// Converts declared accessibility on a symbol to a string usable in generated code.
	/// </summary>
	/// <param name="accessibility">The accessibility to get string for.</param>
	/// <returns>Accessibility in format "public", "protected internal", etc.</returns>
	public static string ToCSharpCodeString(this Accessibility accessibility)
	{
		switch (accessibility)
		{
			case Accessibility.Private:
				return "private";
			case Accessibility.ProtectedOrInternal:
				return "protected internal";
			case Accessibility.Protected:
				return "protected";
			case Accessibility.Internal:
				return "internal";
			case Accessibility.Public:
				return "public";
		}

		throw new ArgumentOutOfRangeException($"{accessibility} is not supported.");
	}

	public static bool IsPartial(this INamedTypeSymbol type)
	{
		var syntaxRefs = type.DeclaringSyntaxReferences;
		var isPartial = syntaxRefs.Length switch
		{
			0 => true,
			1 => IsPartialSyntax(syntaxRefs[0]),
			_ => true, // If we have multiple declaration syntax, well the class is partial ^^
		};

		return isPartial && (type.ContainingType?.IsPartial() ?? true);

		bool IsPartialSyntax(SyntaxReference syntaxRef)
		{
			var syntax = syntaxRef.GetSyntax(CancellationToken.None);
			return syntax switch
			{
				ClassDeclarationSyntax @class => @class.Modifiers.Any(SyntaxKind.PartialKeyword),
				RecordDeclarationSyntax @record => @record.Modifiers.Any(SyntaxKind.PartialKeyword),
				_ => false
			};
		}
	}

	public static bool IsCloneCtor(this IMethodSymbol ctor, INamedTypeSymbol type)
		=> ctor.MethodKind is MethodKind.Constructor
			&& ctor.Parameters is { Length: 1 } parameters
			&& SymbolEqualityComparer.Default.Equals(parameters[0].Type, type);

	public static (bool isDefined, string? value) FindAttributeValue(this ISymbol symbol, INamedTypeSymbol attributeSymbol, string? propertyName = default, uint? ctorPosition = default)
		=> FindAttributeValueCore<string>(symbol, attributeSymbol, propertyName, ctorPosition);

	public static (bool isDefined, T? value) FindAttributeValue<T>(this ISymbol symbol, INamedTypeSymbol attributeSymbol, string? propertyName = default, uint? ctorPosition = default)
		where T : struct
		=> FindAttributeValueCore<T>(symbol, attributeSymbol, propertyName, ctorPosition) is { isDefined: true } result ? result : (false, default(T?));

	private static (bool isDefined, T? value) FindAttributeValueCore<T>(this ISymbol symbol, INamedTypeSymbol attributeSymbol, string? propertyName = default, uint? ctorPosition = default)
	{
		if (propertyName is null && ctorPosition is null)
		{
			throw new InvalidOperationException($"You must define at least one of the {nameof(propertyName)} and the {nameof(ctorPosition)}");
		}

		var attribute = symbol.FindAttribute(attributeSymbol);
		if (attribute is null)
		{
			return (false, default);
		}
		else if (propertyName is not null
			&& attribute
				.NamedArguments
				.FirstOrDefault(kvp => kvp.Key.Equals(propertyName, StringComparison.Ordinal))
				.Value is { IsNull: false } namedArg)
		{
			return (true, (T)namedArg.Value!);
		}
		else if (ctorPosition is not null
			&& attribute
				.ConstructorArguments
				.ElementAt((int)ctorPosition.Value) is { IsNull: false } ctorArg)
		{
			return (true, (T)ctorArg.Value!);
		}
		else
		{
			// This case should happen only if the attribute has been set and we asked for a property that has not been set by the user.
			// If the ctorPosition has been provided, even if the parameter has a default value, it will be resolved
			return (true, default);
		}
	}

	public static TAttribute? FindAttribute<TAttribute>(this ISymbol symbol)
		where TAttribute : Attribute
	{
		var type = typeof(TAttribute);
		var data = symbol
			.GetAttributes()
			.FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString().Equals(type.FullName, StringComparison.Ordinal) ?? false);

		if (data is null)
		{
			return default;
		}

		var ctor = type.GetConstructors().Single(defCtor => Matches(defCtor, data.AttributeConstructor));
		var ctorArgs = data.ConstructorArguments.Select(GetValue).ToArray();

		var attribute = ctor.Invoke(ctorArgs);

		foreach (var dataProperty in data.NamedArguments)
		{
			var prop = type.GetProperty(dataProperty.Key, BindingFlags.Public | BindingFlags.Instance);
			if (prop is null)
			{
				throw new InvalidOperationException($"Failed to get the property named '{dataProperty.Key}' on '{type.FullName}'.");
			}
			prop.SetValue(attribute, GetValue(dataProperty.Value));
		}

		return (TAttribute)attribute;
	}

	private static SymbolDisplayFormat _reflectionFullNameFormat = SymbolDisplayFormat
		.FullyQualifiedFormat
		.RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes) // Force usage of `System.String` instead of `string`
		.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted); // Remove teh `global::` prefix
	private static bool Matches(MethodBase def, IMethodSymbol? actual)
	{
		var actualParameters = actual?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
		var defParameters = def.GetParameters();

		if (actualParameters.Length != defParameters.Length)
		{
			return false;
		}

		for (var i = 0; i < defParameters.Length; i++)
		{
			var actualParameter = actualParameters[i];
			var defParameter = defParameters[i];

			if (!actualParameter.Name.Equals(defParameter.Name, StringComparison.Ordinal)
				|| !actualParameter.Type.ToDisplayString(_reflectionFullNameFormat).Equals(defParameter.ParameterType.FullName, StringComparison.Ordinal))
			{
				return false;
			}
		}

		return true;
	}

	private static object? GetValue(TypedConstant arg)
		=> arg switch
		{
			{ IsNull: true } => null,
			{ Kind: TypedConstantKind.Error } => throw new InvalidOperationException("Attribute argument not resolved properly."),
			{ Kind: TypedConstantKind.Array } => ToTypedArray(Type.GetType(arg.Type!.ToDisplayString(_reflectionFullNameFormat), throwOnError: true, ignoreCase: true)!, arg.Values.Select(GetValue)),
			{ Kind: TypedConstantKind.Type } => throw new InvalidOperationException("Type arguments are not supported for Attribute re-construction."),
			{ Kind: TypedConstantKind.Enum or TypedConstantKind.Primitive } => arg.Value!,
			_ => throw new InvalidOperationException("Attribute argument is unknown."),
		};

	private static object ToTypedArray(Type type, IEnumerable<object?> values)
	{
		var items = values.ToArray();
		var typedItems = Array.CreateInstance(type.IsArray ? type.GetElementType()! : type, items.Length);

		Array.Copy(items, typedItems, items.Length);

		return typedItems;
	}

	public static IPropertySymbol? FindProperty(this INamedTypeSymbol type, string propertyName, bool allowBaseTypes = true, StringComparison comparison = StringComparison.Ordinal)
		=> type.GetProperties().FirstOrDefault(property => property.Name.Equals(propertyName, comparison))
			?? (allowBaseTypes && type.BaseType is { } @base ? @base.FindProperty(propertyName, allowBaseTypes, comparison) : null);

	public static IMethodSymbol? FindMethod(this INamedTypeSymbol type, string methodName, bool allowBaseTypes = true, StringComparison comparison = StringComparison.Ordinal)
		=> type.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(method => method.Name.Equals(methodName, comparison))
			?? (allowBaseTypes && type.BaseType is { } @base? @base.FindMethod(methodName, allowBaseTypes, comparison) : null);

	public static IMethodSymbol GetMethod(this INamedTypeSymbol type, string methodName, bool allowBaseTypes = true, StringComparison comparison = StringComparison.Ordinal)
		=> type.FindMethod(methodName, allowBaseTypes, comparison) ?? throw new InvalidOperationException($"Method {methodName} not found on {type.Name}.");
}
