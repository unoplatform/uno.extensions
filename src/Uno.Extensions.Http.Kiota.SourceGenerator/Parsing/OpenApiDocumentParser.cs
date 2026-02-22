using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Parsing;

/// <summary>
/// Parses OpenAPI documents (JSON and YAML) synchronously for use inside a
/// Roslyn <see cref="Microsoft.CodeAnalysis.IIncrementalGenerator"/> pipeline.
/// <para>
/// Uses <see cref="OpenApiStreamReader"/> from <c>Microsoft.OpenApi.Readers</c>
/// v1.6.28 which targets <c>netstandard2.0</c> and provides a synchronous API.
/// </para>
/// </summary>
internal static class OpenApiDocumentParser
{
	// ------------------------------------------------------------------
	// Supported file extensions
	// ------------------------------------------------------------------

	private static readonly string[] SupportedExtensions = { ".json", ".yaml", ".yml" };

	// ------------------------------------------------------------------
	// Diagnostic descriptors
	// ------------------------------------------------------------------

	/// <summary>KIOTA001: Failed to parse OpenAPI document.</summary>
	internal static readonly DiagnosticDescriptor ParseFailure = new(
		id: "KIOTA001",
		title: "Failed to parse OpenAPI document",
		messageFormat: "Failed to parse OpenAPI document '{0}': {1}",
		category: "Kiota.SourceGenerator",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>KIOTA002: Invalid or unsupported OpenAPI version.</summary>
	internal static readonly DiagnosticDescriptor UnsupportedVersion = new(
		id: "KIOTA002",
		title: "Unsupported OpenAPI version",
		messageFormat: "OpenAPI document '{0}' uses unsupported version '{1}'. Only OpenAPI 2.0, 3.0, and 3.1 are supported.",
		category: "Kiota.SourceGenerator",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>KIOTA010: Non-fatal warning encountered during parsing.</summary>
	internal static readonly DiagnosticDescriptor ParseWarning = new(
		id: "KIOTA010",
		title: "OpenAPI parsing warning",
		messageFormat: "OpenAPI document '{0}': {1}",
		category: "Kiota.SourceGenerator",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	/// <summary>KIOTA020: OpenAPI spec exceeds recommended size for source generation.</summary>
	internal static readonly DiagnosticDescriptor LargeSpec = new(
		id: "KIOTA020",
		title: "Large OpenAPI specification",
		messageFormat: "OpenAPI document '{0}' is {1:N0} characters. Specs over {2:N0} characters may cause slow IDE performance; consider the MSBuild task instead.",
		category: "Kiota.SourceGenerator",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	// ------------------------------------------------------------------
	// Configuration constants
	// ------------------------------------------------------------------

	/// <summary>
	/// Recommended maximum source text length (in characters). Specs above
	/// this threshold emit <see cref="LargeSpec"/> (KIOTA020).
	/// Approximately 100K lines ≈ 5 million characters.
	/// </summary>
	internal const int LargeSpecThreshold = 5_000_000;

	// ------------------------------------------------------------------
	// Public API
	// ------------------------------------------------------------------

	/// <summary>
	/// Returns <see langword="true"/> when <paramref name="filePath"/> has a
	/// supported OpenAPI file extension (<c>.json</c>, <c>.yaml</c>, or <c>.yml</c>).
	/// </summary>
	public static bool IsSupportedFileExtension(string filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			return false;
		}

		var extension = Path.GetExtension(filePath);
		for (var i = 0; i < SupportedExtensions.Length; i++)
		{
			if (string.Equals(extension, SupportedExtensions[i], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Parses an OpenAPI document from the provided <see cref="SourceText"/>.
	/// <para>
	/// The parser auto-detects JSON vs. YAML based on the file extension.
	/// Both OpenAPI 2.0 (Swagger), 3.0, and 3.1 documents are supported by
	/// the underlying <see cref="OpenApiStreamReader"/>.
	/// </para>
	/// </summary>
	/// <param name="sourceText">
	/// The <see cref="SourceText"/> of the OpenAPI document, obtained from
	/// <see cref="AdditionalText.GetText"/>.
	/// </param>
	/// <param name="filePath">
	/// The file path of the document, used for diagnostics and format detection.
	/// </param>
	/// <returns>
	/// An <see cref="OpenApiParseResult"/> containing either the parsed document
	/// or diagnostics describing any errors.
	/// </returns>
	public static OpenApiParseResult Parse(SourceText sourceText, string filePath)
	{
		if (sourceText is null)
		{
			return OpenApiParseResult.Failure(
				Diagnostic.Create(
					ParseFailure,
					Location.None,
					filePath ?? "(unknown)",
					"Source text is null."));
		}

		var text = sourceText.ToString();

		if (string.IsNullOrWhiteSpace(text))
		{
			return OpenApiParseResult.Failure(
				Diagnostic.Create(
					ParseFailure,
					Location.None,
					filePath ?? "(unknown)",
					"Document is empty or contains only whitespace."));
		}

		var diagnosticsBuilder = ImmutableArray.CreateBuilder<Diagnostic>();

		// Warn for very large specs that may degrade IDE responsiveness.
		if (text.Length > LargeSpecThreshold)
		{
			diagnosticsBuilder.Add(
				Diagnostic.Create(
					LargeSpec,
					Location.None,
					filePath ?? "(unknown)",
					text.Length,
					LargeSpecThreshold));
		}

		OpenApiDocument document;
		OpenApiDiagnostic openApiDiagnostic;

		try
		{
			var bytes = Encoding.UTF8.GetBytes(text);
			using var stream = new MemoryStream(bytes);
			var reader = new OpenApiStreamReader();
			document = reader.Read(stream, out openApiDiagnostic);
		}
		catch (Exception ex)
		{
			return OpenApiParseResult.Failure(
				Diagnostic.Create(
					ParseFailure,
					Location.None,
					filePath ?? "(unknown)",
					ex.Message));
		}

		if (document is null)
		{
			// The reader returned null without throwing — fall back to generic error.
			var errorMessage = openApiDiagnostic?.Errors?.Count > 0
				? string.Join("; ", System.Linq.Enumerable.Select(openApiDiagnostic.Errors, e => e.Message))
				: "Unknown parsing error.";

			return OpenApiParseResult.Failure(
				Diagnostic.Create(
					ParseFailure,
					Location.None,
					filePath ?? "(unknown)",
					errorMessage));
		}

		// Propagate OpenAPI diagnostic errors as Roslyn diagnostics.
		if (openApiDiagnostic?.Errors?.Count > 0)
		{
			foreach (var error in openApiDiagnostic.Errors)
			{
				diagnosticsBuilder.Add(
					Diagnostic.Create(
						ParseFailure,
						Location.None,
						filePath ?? "(unknown)",
						error.Message));
			}
		}

		// Propagate OpenAPI diagnostic warnings as Roslyn diagnostics.
		if (openApiDiagnostic?.Warnings?.Count > 0)
		{
			foreach (var warning in openApiDiagnostic.Warnings)
			{
				diagnosticsBuilder.Add(
					Diagnostic.Create(
						ParseWarning,
						Location.None,
						filePath ?? "(unknown)",
						warning.Message));
			}
		}

		// Validate that the spec version is one we can handle.
		if (openApiDiagnostic?.SpecificationVersion != null)
		{
			var version = openApiDiagnostic.SpecificationVersion;
			if (version != OpenApiSpecVersion.OpenApi2_0
				&& version != OpenApiSpecVersion.OpenApi3_0)
			{
				// OpenApiStreamReader v1.6.28 maps OpenAPI 3.1 to OpenApi3_0
				// enum value. If somehow an unrecognised version appears, warn.
				diagnosticsBuilder.Add(
					Diagnostic.Create(
						UnsupportedVersion,
						Location.None,
						filePath ?? "(unknown)",
						version.ToString()));
			}
		}

		return new OpenApiParseResult(document, diagnosticsBuilder.ToImmutable());
	}
}