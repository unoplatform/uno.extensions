#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;

/// <summary>
/// Reads Kiota source generator configuration from Roslyn's
/// <see cref="AnalyzerConfigOptionsProvider"/>.
/// <para>
/// Global properties are exposed via <c>build_property.KiotaGenerator_*</c>
/// keys and per-file metadata via <c>build_metadata.AdditionalFiles.Kiota*</c>
/// keys, both declared in the companion <c>buildTransitive/.props</c> file.
/// </para>
/// </summary>
internal static class ConfigurationReader
{
	// ------------------------------------------------------------------
	// Global property keys (build_property.*)
	// ------------------------------------------------------------------

	private const string GlobalEnabled = "build_property.KiotaGenerator_Enabled";
	private const string GlobalDefaultUsesBackingStore = "build_property.KiotaGenerator_DefaultUsesBackingStore";
	private const string GlobalDefaultIncludeAdditionalData = "build_property.KiotaGenerator_DefaultIncludeAdditionalData";
	private const string GlobalDefaultExcludeBackwardCompatible = "build_property.KiotaGenerator_DefaultExcludeBackwardCompatible";
	private const string GlobalDefaultTypeAccessModifier = "build_property.KiotaGenerator_DefaultTypeAccessModifier";

	// ------------------------------------------------------------------
	// Per-file metadata keys (build_metadata.AdditionalFiles.*)
	// ------------------------------------------------------------------

	private const string MetaClientName = "build_metadata.AdditionalFiles.KiotaClientName";
	private const string MetaNamespace = "build_metadata.AdditionalFiles.KiotaNamespace";
	private const string MetaUsesBackingStore = "build_metadata.AdditionalFiles.KiotaUsesBackingStore";
	private const string MetaIncludeAdditionalData = "build_metadata.AdditionalFiles.KiotaIncludeAdditionalData";
	private const string MetaExcludeBackwardCompatible = "build_metadata.AdditionalFiles.KiotaExcludeBackwardCompatible";
	private const string MetaTypeAccessModifier = "build_metadata.AdditionalFiles.KiotaTypeAccessModifier";
	private const string MetaIncludePatterns = "build_metadata.AdditionalFiles.KiotaIncludePatterns";
	private const string MetaExcludePatterns = "build_metadata.AdditionalFiles.KiotaExcludePatterns";

	/// <summary>
	/// Separator used to split semicolon-delimited pattern lists
	/// (e.g. <c>"**/pets/**;**/stores/**"</c>).
	/// </summary>
	private static readonly char[] PatternSeparators = { ';' };

	// ------------------------------------------------------------------
	// Public API
	// ------------------------------------------------------------------

	/// <summary>
	/// Returns <see langword="true"/> when code generation is enabled for
	/// the current compilation (global <c>KiotaGenerator_Enabled</c> property).
	/// Defaults to <see langword="true"/> when the property is absent.
	/// </summary>
	public static bool IsEnabled(AnalyzerConfigOptionsProvider optionsProvider)
	{
		if (optionsProvider.GlobalOptions.TryGetValue(GlobalEnabled, out var value)
			&& bool.TryParse(value, out var enabled))
		{
			return enabled;
		}

		// Default to enabled when the property is not set.
		return true;
	}

	/// <summary>
	/// Returns <see langword="true"/> when <paramref name="file"/> has
	/// <c>KiotaClientName</c> metadata, indicating it is an OpenAPI spec
	/// that the generator should process.
	/// </summary>
	public static bool IsKiotaAdditionalFile(
		AdditionalText file,
		AnalyzerConfigOptionsProvider optionsProvider)
	{
		var fileOptions = optionsProvider.GetOptions(file);
		return fileOptions.TryGetValue(MetaClientName, out var clientName)
			&& !string.IsNullOrEmpty(clientName);
	}

	/// <summary>
	/// Reads all per-file and global configuration for the given
	/// <paramref name="file"/> and returns an immutable
	/// <see cref="KiotaGeneratorConfig"/>.
	/// <para>
	/// Per-file metadata takes precedence; absent values fall back to
	/// global defaults and then to compile-time constants in
	/// <see cref="KiotaGeneratorConfig"/>.
	/// </para>
	/// </summary>
	public static KiotaGeneratorConfig Read(
		AdditionalText file,
		AnalyzerConfigOptionsProvider optionsProvider)
	{
		var fileOptions = optionsProvider.GetOptions(file);
		var globalOptions = optionsProvider.GlobalOptions;

		// --- String properties (per-file only, constant defaults) ---

		fileOptions.TryGetValue(MetaClientName, out var clientName);
		fileOptions.TryGetValue(MetaNamespace, out var ns);

		// --- Boolean properties (per-file → global default → constant) ---

		var usesBackingStore = ReadBool(
			fileOptions, MetaUsesBackingStore,
			globalOptions, GlobalDefaultUsesBackingStore,
			KiotaGeneratorConfig.DefaultUsesBackingStore);

		var includeAdditionalData = ReadBool(
			fileOptions, MetaIncludeAdditionalData,
			globalOptions, GlobalDefaultIncludeAdditionalData,
			KiotaGeneratorConfig.DefaultIncludeAdditionalData);

		var excludeBackwardCompatible = ReadBool(
			fileOptions, MetaExcludeBackwardCompatible,
			globalOptions, GlobalDefaultExcludeBackwardCompatible,
			KiotaGeneratorConfig.DefaultExcludeBackwardCompatible);

		// --- String with global fallback ---

		var typeAccessModifier = ReadStringWithGlobalFallback(
			fileOptions, MetaTypeAccessModifier,
			globalOptions, GlobalDefaultTypeAccessModifier,
			KiotaGeneratorConfig.DefaultTypeAccessModifier);

		// --- Pattern arrays (per-file only, semicolon-separated) ---

		var includePatterns = ReadPatterns(fileOptions, MetaIncludePatterns);
		var excludePatterns = ReadPatterns(fileOptions, MetaExcludePatterns);

		return new KiotaGeneratorConfig(
			clientClassName: clientName,
			clientNamespaceName: ns,
			usesBackingStore: usesBackingStore,
			includeAdditionalData: includeAdditionalData,
			excludeBackwardCompatible: excludeBackwardCompatible,
			typeAccessModifier: typeAccessModifier,
			includePatterns: includePatterns,
			excludePatterns: excludePatterns);
	}

	// ------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------

	/// <summary>
	/// Reads a boolean value with a three-level fallback chain:
	/// per-file metadata → global property → compile-time constant.
	/// </summary>
	private static bool ReadBool(
		AnalyzerConfigOptions fileOptions,
		string fileKey,
		AnalyzerConfigOptions globalOptions,
		string globalKey,
		bool defaultValue)
	{
		// 1. Per-file metadata
		if (fileOptions.TryGetValue(fileKey, out var fileValue)
			&& !string.IsNullOrEmpty(fileValue)
			&& bool.TryParse(fileValue, out var fileBool))
		{
			return fileBool;
		}

		// 2. Global default property
		if (globalOptions.TryGetValue(globalKey, out var globalValue)
			&& !string.IsNullOrEmpty(globalValue)
			&& bool.TryParse(globalValue, out var globalBool))
		{
			return globalBool;
		}

		// 3. Compile-time constant
		return defaultValue;
	}

	/// <summary>
	/// Reads a string value with a three-level fallback chain:
	/// per-file metadata → global property → compile-time constant.
	/// </summary>
	private static string ReadStringWithGlobalFallback(
		AnalyzerConfigOptions fileOptions,
		string fileKey,
		AnalyzerConfigOptions globalOptions,
		string globalKey,
		string defaultValue)
	{
		if (fileOptions.TryGetValue(fileKey, out var fileValue)
			&& !string.IsNullOrEmpty(fileValue))
		{
			return fileValue;
		}

		if (globalOptions.TryGetValue(globalKey, out var globalValue)
			&& !string.IsNullOrEmpty(globalValue))
		{
			return globalValue;
		}

		return defaultValue;
	}

	/// <summary>
	/// Splits a semicolon-delimited metadata value into an
	/// <see cref="ImmutableArray{T}"/> of non-empty trimmed strings.
	/// Returns <see cref="ImmutableArray{T}.Empty"/> when the metadata
	/// is absent or blank.
	/// </summary>
	private static ImmutableArray<string> ReadPatterns(
		AnalyzerConfigOptions fileOptions,
		string key)
	{
		if (!fileOptions.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
		{
			return ImmutableArray<string>.Empty;
		}

		var parts = raw.Split(PatternSeparators, StringSplitOptions.RemoveEmptyEntries);
		var builder = ImmutableArray.CreateBuilder<string>(parts.Length);

		for (var i = 0; i < parts.Length; i++)
		{
			var trimmed = parts[i].Trim();
			if (trimmed.Length > 0)
			{
				builder.Add(trimmed);
			}
		}

		return builder.ToImmutable();
	}
}
