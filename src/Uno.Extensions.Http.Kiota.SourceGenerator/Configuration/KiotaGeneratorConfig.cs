using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;

/// <summary>
/// Immutable configuration for a single Kiota client generation sourced from
/// MSBuild properties and per-file <c>AdditionalFiles</c> metadata.
/// <para>
/// Implements value equality so the Roslyn incremental pipeline can skip
/// re-generation when configuration has not changed between compilations.
/// </para>
/// </summary>
internal readonly struct KiotaGeneratorConfig : IEquatable<KiotaGeneratorConfig>
{
	// ------------------------------------------------------------------
	// Default values (match the buildTransitive .props defaults)
	// ------------------------------------------------------------------

	/// <summary>Default client class name when <c>KiotaClientName</c> metadata is not specified.</summary>
	public const string DefaultClientClassName = "ApiClient";

	/// <summary>Default namespace when <c>KiotaNamespace</c> metadata is not specified.</summary>
	public const string DefaultClientNamespaceName = "ApiSdk";

	/// <summary>Default for <see cref="UsesBackingStore"/>.</summary>
	public const bool DefaultUsesBackingStore = false;

	/// <summary>Default for <see cref="IncludeAdditionalData"/>.</summary>
	public const bool DefaultIncludeAdditionalData = true;

	/// <summary>Default for <see cref="ExcludeBackwardCompatible"/>.</summary>
	public const bool DefaultExcludeBackwardCompatible = false;

	/// <summary>Default for <see cref="TypeAccessModifier"/>.</summary>
	public const string DefaultTypeAccessModifier = "Public";

	// ------------------------------------------------------------------
	// Constructor
	// ------------------------------------------------------------------

	/// <summary>
	/// Initializes a new <see cref="KiotaGeneratorConfig"/> with the given values.
	/// </summary>
	public KiotaGeneratorConfig(
		string clientClassName,
		string clientNamespaceName,
		bool usesBackingStore,
		bool includeAdditionalData,
		bool excludeBackwardCompatible,
		string typeAccessModifier,
		ImmutableArray<string> includePatterns,
		ImmutableArray<string> excludePatterns)
	{
		ClientClassName = clientClassName ?? DefaultClientClassName;
		ClientNamespaceName = clientNamespaceName ?? DefaultClientNamespaceName;
		UsesBackingStore = usesBackingStore;
		IncludeAdditionalData = includeAdditionalData;
		ExcludeBackwardCompatible = excludeBackwardCompatible;
		TypeAccessModifier = typeAccessModifier ?? DefaultTypeAccessModifier;
		IncludePatterns = includePatterns.IsDefault ? ImmutableArray<string>.Empty : includePatterns;
		ExcludePatterns = excludePatterns.IsDefault ? ImmutableArray<string>.Empty : excludePatterns;
	}

	// ------------------------------------------------------------------
	// Properties
	// ------------------------------------------------------------------

	/// <summary>
	/// The name of the generated root client class (e.g. <c>PetStoreClient</c>).
	/// Sourced from <c>build_metadata.AdditionalFiles.KiotaClientName</c>.
	/// </summary>
	public string ClientClassName { get; }

	/// <summary>
	/// The C# namespace for all generated types (e.g. <c>MyApp.PetStore</c>).
	/// Sourced from <c>build_metadata.AdditionalFiles.KiotaNamespace</c>.
	/// </summary>
	public string ClientNamespaceName { get; }

	/// <summary>
	/// When <see langword="true"/>, generated model classes implement
	/// <c>IBackedModel</c> and use a backing store for property storage.
	/// </summary>
	public bool UsesBackingStore { get; }

	/// <summary>
	/// When <see langword="true"/>, generated model classes implement
	/// <c>IAdditionalDataHolder</c> and expose an <c>AdditionalData</c> dictionary.
	/// </summary>
	public bool IncludeAdditionalData { get; }

	/// <summary>
	/// When <see langword="true"/>, backward-compatible overloads are not generated.
	/// </summary>
	public bool ExcludeBackwardCompatible { get; }

	/// <summary>
	/// The access modifier for generated types — <c>"Public"</c> or <c>"Internal"</c>.
	/// </summary>
	public string TypeAccessModifier { get; }

	/// <summary>
	/// Semicolon-separated glob patterns restricting which API paths to include.
	/// An empty array means include all paths.
	/// </summary>
	public ImmutableArray<string> IncludePatterns { get; }

	/// <summary>
	/// Semicolon-separated glob patterns restricting which API paths to exclude.
	/// Applied after <see cref="IncludePatterns"/>.
	/// </summary>
	public ImmutableArray<string> ExcludePatterns { get; }

	// ------------------------------------------------------------------
	// Equality (critical for Roslyn incremental caching correctness)
	// ------------------------------------------------------------------

	/// <inheritdoc />
	public bool Equals(KiotaGeneratorConfig other)
	{
		return string.Equals(ClientClassName, other.ClientClassName, StringComparison.Ordinal)
			&& string.Equals(ClientNamespaceName, other.ClientNamespaceName, StringComparison.Ordinal)
			&& UsesBackingStore == other.UsesBackingStore
			&& IncludeAdditionalData == other.IncludeAdditionalData
			&& ExcludeBackwardCompatible == other.ExcludeBackwardCompatible
			&& string.Equals(TypeAccessModifier, other.TypeAccessModifier, StringComparison.Ordinal)
			&& SequenceEquals(IncludePatterns, other.IncludePatterns)
			&& SequenceEquals(ExcludePatterns, other.ExcludePatterns);
	}

	/// <inheritdoc />
	public override bool Equals(object obj) => obj is KiotaGeneratorConfig other && Equals(other);

	/// <inheritdoc />
	public override int GetHashCode()
	{
		unchecked
		{
			var hash = 17;
			hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ClientClassName ?? string.Empty);
			hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ClientNamespaceName ?? string.Empty);
			hash = (hash * 31) + UsesBackingStore.GetHashCode();
			hash = (hash * 31) + IncludeAdditionalData.GetHashCode();
			hash = (hash * 31) + ExcludeBackwardCompatible.GetHashCode();
			hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(TypeAccessModifier ?? string.Empty);
			hash = (hash * 31) + GetSequenceHashCode(IncludePatterns);
			hash = (hash * 31) + GetSequenceHashCode(ExcludePatterns);
			return hash;
		}
	}

	/// <summary>Equality operator.</summary>
	public static bool operator ==(KiotaGeneratorConfig left, KiotaGeneratorConfig right) => left.Equals(right);

	/// <summary>Inequality operator.</summary>
	public static bool operator !=(KiotaGeneratorConfig left, KiotaGeneratorConfig right) => !left.Equals(right);

	// ------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------

	private static bool SequenceEquals(ImmutableArray<string> left, ImmutableArray<string> right)
	{
		if (left.Length != right.Length)
		{
			return false;
		}

		for (var i = 0; i < left.Length; i++)
		{
			if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
			{
				return false;
			}
		}

		return true;
	}

	private static int GetSequenceHashCode(ImmutableArray<string> array)
	{
		unchecked
		{
			var hash = 17;
			for (var i = 0; i < array.Length; i++)
			{
				hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(array[i] ?? string.Empty);
			}

			return hash;
		}
	}
}
