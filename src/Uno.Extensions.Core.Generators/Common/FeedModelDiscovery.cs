using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Generators;

/// <summary>
/// How a type is recognized as an MVUX model, and how the view-model generated from it is named.
///
/// Both generators compile this. The MVUX generator decides what to generate from it; the mocking
/// generator has to name and construct that same view-model — by name, in code it emits — when the
/// model sits in the compilation being generated and the view-model is therefore not yet a symbol.
/// A divergence here does not degrade gracefully: the emitted mock references a type that does not
/// exist, and the error lands in generated code the consumer cannot edit.
/// </summary>
internal static class FeedModelDiscovery
{
	/// <summary>The metadata name of the attribute enabling or disabling generation on one type.</summary>
	public const string ReactiveBindableAttributeName = "Uno.Extensions.Reactive.ReactiveBindableAttribute";

	/// <summary>The metadata name of the assembly-level attribute carrying the implicit model patterns.</summary>
	public const string ImplicitBindablesAttributeName = "Uno.Extensions.Reactive.Config.ImplicitBindablesAttribute";

	private const string ViewModelSuffix = "ViewModel";

	/// <summary>The pattern applied when the assembly declares none.</summary>
	public const string DefaultModelPattern = "Model$";

	/// <summary>
	/// Bounds a single match. The patterns come from user code, so a pathological one must not hang the
	/// compiler — which, in an IDE, means hanging on every keystroke.
	/// </summary>
	private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

	public static string GetModelName(INamedTypeSymbol type)
		=> type.Name.TrimEnd("Model", StringComparison.Ordinal);

	public static string GetViewModelName(INamedTypeSymbol model)
		=> $"{GetModelName(model)}{ViewModelSuffix}";

	/// <summary>
	/// The fully qualified name of the generated view-model. Derived from the model's own full name, so a
	/// nested model resolves inside its containing type rather than at namespace scope.
	/// </summary>
	public static string GetViewModelFullName(INamedTypeSymbol model)
		=> $"{model.ToFullString().TrimEnd(model.Name, StringComparison.Ordinal)}{GetModelName(model)}{ViewModelSuffix}";

	/// <summary>
	/// Whether <paramref name="type"/> is a model the MVUX generator generates a view-model for: an
	/// explicit <c>[ReactiveBindable]</c> decides on its own, otherwise a partial type whose full name
	/// matches one of its assembly's implicit patterns.
	/// </summary>
	/// <param name="type">The candidate.</param>
	/// <param name="reactiveBindable">The resolved <c>ReactiveBindableAttribute</c>, or null when unavailable.</param>
	/// <param name="implicitBindables">The resolved <c>ImplicitBindablesAttribute</c>, or null when unavailable.</param>
	public static bool IsModel(INamedTypeSymbol? type, INamedTypeSymbol? reactiveBindable, INamedTypeSymbol? implicitBindables)
	{
		if (type is null)
		{
			return false;
		}

		if (reactiveBindable is not null
			&& type.FindAttributeValue<bool>(reactiveBindable, "IsEnabled", 0) is { isDefined: true } attribute)
		{
			// When the attribute is set the `partial` is not checked: the build has to fail if it is missing.
			return attribute.value ?? true;
		}

		if (!type.IsPartial())
		{
			return false;
		}

		// Read from the type's own assembly: a model of a referenced assembly is governed by that
		// assembly's patterns, not by the ones of the compilation doing the reading.
		var (isEnabled, patterns) = ReadImplicitBindables(type.ContainingAssembly, implicitBindables);
		if (!isEnabled)
		{
			return false;
		}

		var name = type.ToString();
		return patterns.Any(pattern => IsMatch(pattern, name));
	}

	/// <summary>The implicit-model configuration of one assembly, with the defaults applied.</summary>
	public static (bool isEnabled, string[] patterns) ReadImplicitBindables(IAssemblySymbol? assembly, INamedTypeSymbol? implicitBindables)
	{
		var attribute = assembly?
			.GetAttributes()
			.FirstOrDefault(a => implicitBindables is not null && SymbolEqualityComparer.Default.Equals(a.AttributeClass, implicitBindables));
		if (attribute is null)
		{
			return (true, new[] { DefaultModelPattern });
		}

		var isEnabled = attribute.NamedArguments.FirstOrDefault(na => na.Key == "IsEnabled").Value.Value as bool? ?? true;

		var patterns = attribute.ConstructorArguments
			.SelectMany(arg => arg.Kind == TypedConstantKind.Array ? (IEnumerable<TypedConstant>)arg.Values : new[] { arg })
			.Select(value => value.Value as string)
			.Where(value => !string.IsNullOrEmpty(value))
			.Select(value => value!)
			.ToArray();

		return (isEnabled, patterns.Length > 0 ? patterns : new[] { DefaultModelPattern });
	}

	/// <summary>Whether <paramref name="pattern"/> is a usable expression; false for one that cannot compile.</summary>
	public static bool IsValidPattern(string pattern)
	{
		try
		{
			_ = Regex.Match(string.Empty, pattern, RegexOptions.CultureInvariant, MatchTimeout);
			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	private static bool IsMatch(string pattern, string typeName)
	{
		try
		{
			return Regex.IsMatch(typeName, pattern, RegexOptions.CultureInvariant, MatchTimeout);
		}
		catch (ArgumentException)
		{
			return false; // Not a valid expression — reported once per compilation by the caller.
		}
		catch (RegexMatchTimeoutException)
		{
			return false;
		}
	}
}
