using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Http.Kiota.Generator.Cli;

/// <summary>
/// POCO that captures all CLI arguments for the Kiota code generator.
/// Each property maps to a <c>--flag</c> on the command line and is
/// subsequently projected onto <see cref="global::Kiota.Builder.Configuration.GenerationConfiguration"/>
/// before invoking <see cref="global::Kiota.Builder.KiotaBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// Defaults mirror the <c>GenerationConfiguration</c> constructor defaults so that
/// omitted CLI flags produce the same output as the stock Kiota CLI.
/// </para>
/// <para>
/// Array-typed options (<see cref="IncludePatterns"/>, <see cref="ExcludePatterns"/>,
/// <see cref="Serializers"/>, <see cref="Deserializers"/>, <see cref="StructuredMimeTypes"/>,
/// <see cref="DisableValidationRules"/>) are passed on the command line as
/// semicolon-separated strings and parsed into arrays by the command handler.
/// </para>
/// </remarks>
internal sealed class GeneratorOptions
{
	// ── Required paths ──────────────────────────────────────────────────

	/// <summary>
	/// Path (or URL) to the OpenAPI description file.
	/// Maps to <c>GenerationConfiguration.OpenAPIFilePath</c>.
	/// </summary>
	public required string OpenApiPath { get; init; }

	/// <summary>
	/// Directory where generated C# files are written.
	/// Maps to <c>GenerationConfiguration.OutputPath</c>.
	/// </summary>
	public required string OutputPath { get; init; }

	// ── Naming ──────────────────────────────────────────────────────────

	/// <summary>
	/// Name of the root client class (e.g. <c>PetStoreClient</c>).
	/// Maps to <c>GenerationConfiguration.ClientClassName</c>.
	/// </summary>
	public string ClassName { get; init; } = "ApiClient";

	/// <summary>
	/// Root namespace for generated code.
	/// Maps to <c>GenerationConfiguration.ClientNamespaceName</c>.
	/// </summary>
	public string Namespace { get; init; } = "ApiSdk";

	// ── Feature flags ───────────────────────────────────────────────────

	/// <summary>
	/// Enable the <c>IBackedModel</c> / <c>IBackingStore</c> pattern.
	/// Maps to <c>GenerationConfiguration.UsesBackingStore</c>.
	/// </summary>
	public bool UsesBackingStore { get; init; }

	/// <summary>
	/// Add an <c>AdditionalData</c> dictionary to generated models.
	/// Maps to <c>GenerationConfiguration.IncludeAdditionalData</c>.
	/// </summary>
	public bool IncludeAdditionalData { get; init; } = true;

	/// <summary>
	/// Skip emission of deprecated backward-compatibility code.
	/// Maps to <c>GenerationConfiguration.ExcludeBackwardCompatible</c>.
	/// </summary>
	public bool ExcludeBackwardCompatible { get; init; }

	// ── Type visibility ─────────────────────────────────────────────────

	/// <summary>
	/// Access modifier applied to all generated types (<c>Public</c> or <c>Internal</c>).
	/// Maps to <c>GenerationConfiguration.TypeAccessModifier</c>.
	/// </summary>
	public string TypeAccessModifier { get; init; } = "Public";

	// ── Path filtering ──────────────────────────────────────────────────

	/// <summary>
	/// Glob patterns selecting which API paths to include.
	/// An empty array means "include all".
	/// Maps to <c>GenerationConfiguration.IncludePatterns</c>.
	/// </summary>
	public string[] IncludePatterns { get; init; } = [];

	/// <summary>
	/// Glob patterns selecting which API paths to exclude.
	/// Maps to <c>GenerationConfiguration.ExcludePatterns</c>.
	/// </summary>
	public string[] ExcludePatterns { get; init; } = [];

	// ── Serializer / deserializer registration ──────────────────────────

	/// <summary>
	/// Fully-qualified class names of <c>ISerializationWriterFactory</c>
	/// implementations to register in the generated client constructor.
	/// When empty, Kiota's built-in defaults are used (JSON, Text, Form, Multipart).
	/// Maps to <c>GenerationConfiguration.Serializers</c>.
	/// </summary>
	public string[] Serializers { get; init; } = [];

	/// <summary>
	/// Fully-qualified class names of <c>IParseNodeFactory</c>
	/// implementations to register in the generated client constructor.
	/// When empty, Kiota's built-in defaults are used (JSON, Text, Form).
	/// Maps to <c>GenerationConfiguration.Deserializers</c>.
	/// </summary>
	public string[] Deserializers { get; init; } = [];

	// ── Content negotiation ─────────────────────────────────────────────

	/// <summary>
	/// Structured MIME types the generated client can handle, with optional
	/// quality-weight suffixes (e.g. <c>application/json;q=1</c>).
	/// When empty, Kiota's built-in defaults are used.
	/// Maps to <c>GenerationConfiguration.StructuredMimeTypes</c>.
	/// </summary>
	public string[] StructuredMimeTypes { get; init; } = [];

	// ── Output control ──────────────────────────────────────────────────

	/// <summary>
	/// Delete the output directory before generating new files.
	/// Maps to <c>GenerationConfiguration.CleanOutput</c>.
	/// </summary>
	public bool CleanOutput { get; init; }

	// ── Validation ──────────────────────────────────────────────────────

	/// <summary>
	/// Names of OpenAPI validation rules to suppress.
	/// Maps to <c>GenerationConfiguration.DisabledValidationRules</c>.
	/// </summary>
	public string[] DisableValidationRules { get; init; } = [];

	// ── Diagnostics ─────────────────────────────────────────────────────

	/// <summary>
	/// Minimum log level for generator diagnostics written to stderr.
	/// Maps to the <see cref="ILogger"/> filter level.
	/// </summary>
	public LogLevel LogLevel { get; init; } = LogLevel.Warning;
}
