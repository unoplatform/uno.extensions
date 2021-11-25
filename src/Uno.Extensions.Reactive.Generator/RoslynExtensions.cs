using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

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

	///// <summary>
	///// Determines if the symbol inherits from the specified type.
	///// </summary>
	///// <param name="symbol">The current symbol</param>
	///// <param name="other">A potential base class.</param>
	//public static bool Is(this INamedTypeSymbol symbol, INamedTypeSymbol other)
	//{
	//	do
	//	{
	//		//if (SymbolEqualityComparer.Default.Equals(symbol, other))
	//		//{
	//		//	return true;
	//		//}

	//		symbol = symbol.BaseType;

	//		if (symbol == null)
	//		{
	//			break;
	//		}

	//	} while (symbol.Name != "Object");

	//	return false;
	//}

	//public static AttributeData FindAttribute(this ISymbol property, INamedTypeSymbol attributeClassSymbol)
	//{
	//	return default!;
	//	//return property.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeClassSymbol));
	//}
}
