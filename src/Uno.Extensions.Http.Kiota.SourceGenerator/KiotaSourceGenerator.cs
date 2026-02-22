#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;
using Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;
using Uno.Extensions.Http.Kiota.SourceGenerator.Emitter;
using Uno.Extensions.Http.Kiota.SourceGenerator.Parsing;
using Uno.Extensions.Http.Kiota.SourceGenerator.Refinement;

namespace Uno.Extensions.Http.Kiota.SourceGenerator;

/// <summary>
/// Roslyn incremental source generator that produces Kiota-compatible C# client
/// code from OpenAPI specification files included as <c>AdditionalFiles</c>.
/// <para>
/// The generator identifies relevant files by the presence of
/// <c>KiotaClientName</c> metadata on the <c>AdditionalFiles</c> item, reads
/// per-file and global configuration through <see cref="ConfigurationReader"/>,
/// and parses the OpenAPI document via <see cref="OpenApiDocumentParser"/>.
/// </para>
/// <para>
/// The full pipeline is:
/// <list type="number">
///   <item>Filter <c>AdditionalFiles</c> to those with <c>KiotaClientName</c> metadata.</item>
///   <item>Read configuration and parse the OpenAPI document.</item>
///   <item>Build a <see cref="CodeNamespace"/> tree via <see cref="KiotaCodeDomBuilder"/>.</item>
///   <item>Apply C#-specific refinements via <see cref="CSharpRefiner"/>.</item>
///   <item>Emit C# source files via <see cref="CSharpEmitter"/> and register
///         them with the <c>SourceProductionContext</c>.</item>
/// </list>
/// </para>
/// <para>
/// Error handling strategy (T052):
/// <list type="bullet">
///   <item>All pipeline stages are wrapped in <c>try/catch</c> to prevent
///         unhandled exceptions from crashing the compiler or IDE.</item>
///   <item>Failures are reported as Roslyn diagnostics (KIOTA001–KIOTA051).</item>
///   <item>Individual type emission failures are caught and skipped, allowing
///         partial generation of remaining types.</item>
///   <item>On complete pipeline failure, a fallback source comment is emitted
///         explaining the error.</item>
/// </list>
/// </para>
/// </summary>
[Generator(LanguageNames.CSharp)]
internal sealed class KiotaSourceGenerator : IIncrementalGenerator
{
	// ------------------------------------------------------------------
	// Diagnostic descriptors
	// ------------------------------------------------------------------

	/// <summary>KIOTA003: Missing required configuration.</summary>
	internal static readonly DiagnosticDescriptor MissingConfiguration = new(
		id: "KIOTA003",
		title: "Missing required Kiota configuration",
		messageFormat: "OpenAPI file '{0}' is missing required metadata: {1}. Ensure the AdditionalFiles item has KiotaClientName metadata.",
		category: "Kiota.SourceGenerator",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>KIOTA030: Generation completed successfully.</summary>
	internal static readonly DiagnosticDescriptor GenerationCompleted = new(
		id: "KIOTA030",
		title: "Kiota code generation completed",
		messageFormat: "Kiota source generator processed '{0}' — generated {1} source file(s).",
		category: "Kiota.SourceGenerator",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true);

	/// <summary>KIOTA031: Generation completed with partial output (some types were skipped).</summary>
	internal static readonly DiagnosticDescriptor PartialGeneration = new(
		id: "KIOTA031",
		title: "Kiota code generation completed with warnings",
		messageFormat: "Kiota source generator processed '{0}' — generated {1} source file(s), skipped {2} type(s) due to errors.",
		category: "Kiota.SourceGenerator",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	/// <summary>KIOTA040: Unhandled exception in the generator pipeline.</summary>
	internal static readonly DiagnosticDescriptor UnhandledException = new(
		id: "KIOTA040",
		title: "Kiota source generator failed",
		messageFormat: "Kiota source generator encountered an unexpected error processing '{0}': {1}",
		category: "Kiota.SourceGenerator",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>KIOTA050: Individual type emission failed (partial generation).</summary>
	internal static readonly DiagnosticDescriptor TypeEmissionFailed = new(
		id: "KIOTA050",
		title: "Failed to generate source for type",
		messageFormat: "Kiota source generator skipped type '{0}' in '{1}': {2}",
		category: "Kiota.SourceGenerator",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	/// <summary>KIOTA051: CodeDOM build failed for the spec.</summary>
	internal static readonly DiagnosticDescriptor CodeDomBuildFailed = new(
		id: "KIOTA051",
		title: "Failed to build code model from OpenAPI document",
		messageFormat: "Kiota source generator failed to build the code model for '{0}': {1}",
		category: "Kiota.SourceGenerator",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <inheritdoc />
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Step 1: Combine each AdditionalText with the AnalyzerConfigOptionsProvider
		//         so we can read per-file metadata in predicates and transforms.
		var filesWithOptions = context.AdditionalTextsProvider
			.Combine(context.AnalyzerConfigOptionsProvider);

		// Step 2: Filter to files that have KiotaClientName metadata AND a
		//         supported OpenAPI file extension (.json, .yaml, .yml).
		var kiotaFiles = filesWithOptions
			.Where(static pair => IsKiotaOpenApiFile(pair.Left, pair.Right));

		// Step 3: For each matched file, read configuration and parse the
		//         OpenAPI document. This transform is the most expensive step
		//         and is cached by Roslyn's incremental pipeline when neither
		//         the file content nor the analyzer options change.
		var parsedSpecs = kiotaFiles
			.Select(static (pair, cancellationToken) =>
				ParseSpec(pair.Left, pair.Right, cancellationToken));

		// Step 4: Register the source output. For each parsed spec, build
		//         the CodeDOM, refine for C#, emit source files, and add
		//         them to the compilation.
		context.RegisterSourceOutput(parsedSpecs, EmitOutput);
	}

	// ------------------------------------------------------------------
	// Pipeline predicates and transforms
	// ------------------------------------------------------------------

	/// <summary>
	/// Returns <see langword="true"/> when the file has <c>KiotaClientName</c>
	/// metadata and a supported OpenAPI file extension.
	/// <para>
	/// Wrapped in <c>try/catch</c> to ensure the predicate never throws —
	/// an exception in a <c>Where</c> predicate would crash the entire
	/// incremental pipeline.
	/// </para>
	/// </summary>
	private static bool IsKiotaOpenApiFile(
		AdditionalText file,
		AnalyzerConfigOptionsProvider optionsProvider)
	{
		try
		{
			// Quick extension check first (cheap) before reading metadata.
			if (!OpenApiDocumentParser.IsSupportedFileExtension(file.Path))
			{
				return false;
			}

			return ConfigurationReader.IsKiotaAdditionalFile(file, optionsProvider);
		}
		catch (Exception)
		{
			// Swallow — the file will not be processed. A diagnostic cannot
			// be emitted from a Where predicate, but the user will notice
			// that no code was generated and can investigate.
			return false;
		}
	}

	/// <summary>
	/// Reads configuration and parses the OpenAPI document for a single
	/// <c>AdditionalFiles</c> entry. Returns an intermediate result that
	/// carries the parsed document, configuration, diagnostics, and file
	/// path for downstream emission.
	/// <para>
	/// The entire method is wrapped in <c>try/catch</c> to prevent any
	/// unhandled exception from crashing the incremental pipeline. Config
	/// read failures and parse failures are reported as diagnostics.
	/// </para>
	/// </summary>
	private static KiotaSpecParseResult ParseSpec(
		AdditionalText file,
		AnalyzerConfigOptionsProvider optionsProvider,
		CancellationToken cancellationToken)
	{
		var filePath = file.Path ?? "(unknown)";

		try
		{
			// Check global enabled flag.
			if (!ConfigurationReader.IsEnabled(optionsProvider))
			{
				return KiotaSpecParseResult.Skipped(filePath);
			}

			// Read per-file + global configuration.
			var config = ConfigurationReader.Read(file, optionsProvider);

			// Validate required configuration — KiotaClientName must be non-empty.
			if (string.IsNullOrEmpty(config.ClientClassName))
			{
				var diagnostic = Diagnostic.Create(
					MissingConfiguration,
					Location.None,
					filePath,
					"KiotaClientName");
				return KiotaSpecParseResult.WithDiagnostics(
					filePath, config, ImmutableArray.Create(diagnostic));
			}

			// Read the source text (may be null if the file was deleted between
			// filtering and transform execution).
			var sourceText = file.GetText(cancellationToken);
			if (sourceText is null)
			{
				var diagnostic = Diagnostic.Create(
					OpenApiDocumentParser.ParseFailure,
					Location.None,
					filePath,
					"Unable to read file content.");
				return KiotaSpecParseResult.WithDiagnostics(
					filePath, config, ImmutableArray.Create(diagnostic));
			}

			// Parse the OpenAPI document.
			var parseResult = OpenApiDocumentParser.Parse(sourceText, filePath);

			return new KiotaSpecParseResult(
				filePath,
				config,
				parseResult,
				isSkipped: false);
		}
		catch (OperationCanceledException)
		{
			// Cancellation is expected (e.g. user editing in IDE). Re-throw to
			// let Roslyn handle it properly.
			throw;
		}
		catch (Exception ex)
		{
			// Catch-all for unexpected failures in config reading or parsing.
			// Report as a diagnostic and return a failed result so the pipeline
			// does not crash.
			var diagnostic = Diagnostic.Create(
				UnhandledException,
				Location.None,
				filePath,
				ex.Message);
			return KiotaSpecParseResult.WithDiagnostics(
				filePath,
				default,
				ImmutableArray.Create(diagnostic));
		}
	}

	// ------------------------------------------------------------------
	// Source output registration
	// ------------------------------------------------------------------

	/// <summary>
	/// Emits source output (or diagnostics) for a single parsed OpenAPI spec.
	/// <para>
	/// Executes the full pipeline: CodeDOM build → C# refine → emit. Each
	/// emitted source file is registered via
	/// <see cref="SourceProductionContext.AddSource(string, SourceText)"/>.
	/// </para>
	/// <para>
	/// Error handling strategy:
	/// <list type="bullet">
	///   <item>Parse diagnostics are forwarded to the compilation.</item>
	///   <item>CodeDOM build failures report <c>KIOTA051</c> and emit a
	///         fallback comment source.</item>
	///   <item>Individual type emission failures report <c>KIOTA050</c> per
	///         skipped type; remaining types are still emitted (partial generation).</item>
	///   <item>Complete pipeline failures report <c>KIOTA040</c> and emit a
	///         fallback comment source.</item>
	/// </list>
	/// </para>
	/// </summary>
	private static void EmitOutput(
		SourceProductionContext context,
		KiotaSpecParseResult result)
	{
		if (result.IsSkipped)
		{
			return;
		}

		// Forward any diagnostics from parsing to the compilation.
		if (!result.ParseResult.Diagnostics.IsDefaultOrEmpty)
		{
			foreach (var diagnostic in result.ParseResult.Diagnostics)
			{
				context.ReportDiagnostic(diagnostic);
			}
		}

		// If parsing failed, stop — errors have already been reported.
		if (!result.ParseResult.IsSuccess)
		{
			return;
		}

		try
		{
			// ── Phase 1: Build the CodeDOM tree ──
			CodeNamespace codeModel;
			try
			{
				codeModel = new KiotaCodeDomBuilder(result.Config)
					.Build(result.ParseResult.Document);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						CodeDomBuildFailed,
						Location.None,
						result.FilePath,
						ex.Message));

				EmitFallbackComment(context, result.FilePath, result.Config,
					"CodeDOM build failed: " + ex.Message);
				return;
			}

			// ── Phase 2: Apply C#-specific refinements ──
			try
			{
				new CSharpRefiner(result.Config).Refine(codeModel);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						CodeDomBuildFailed,
						Location.None,
						result.FilePath,
						"C# refinement failed: " + ex.Message));

				EmitFallbackComment(context, result.FilePath, result.Config,
					"C# refinement failed: " + ex.Message);
				return;
			}

			// ── Phase 3: Emit C# source files with per-type error handling ──
			var emitter = new CSharpEmitter(result.Config);
			var fileCount = 0;
			var skippedCount = 0;

			foreach (var (hintName, source) in emitter.Emit(codeModel))
			{
				try
				{
					context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
					fileCount++;
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					// Individual type emission failed. Report the error and
					// continue with remaining types (partial generation).
					skippedCount++;
					context.ReportDiagnostic(
						Diagnostic.Create(
							TypeEmissionFailed,
							Location.None,
							hintName,
							result.FilePath,
							ex.Message));
				}
			}

			// Report summary diagnostic.
			if (skippedCount > 0)
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						PartialGeneration,
						Location.None,
						result.FilePath,
						fileCount,
						skippedCount));
			}
			else
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						GenerationCompleted,
						Location.None,
						result.FilePath,
						fileCount));
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			// Catch-all to prevent the generator from crashing the compiler.
			context.ReportDiagnostic(
				Diagnostic.Create(
					UnhandledException,
					Location.None,
					result.FilePath,
					ex.Message));

			EmitFallbackComment(context, result.FilePath, result.Config,
				ex.Message);
		}
	}

	// ------------------------------------------------------------------
	// Fallback source emission
	// ------------------------------------------------------------------

	/// <summary>
	/// Emits a fallback C# source file containing a comment that explains
	/// why code generation failed. This ensures the user sees a clear
	/// message rather than silently missing types.
	/// </summary>
	/// <param name="context">The source production context.</param>
	/// <param name="filePath">The path of the OpenAPI spec that failed.</param>
	/// <param name="config">
	/// The generator configuration (may be <see langword="default"/>).
	/// </param>
	/// <param name="errorMessage">A description of the failure.</param>
	private static void EmitFallbackComment(
		SourceProductionContext context,
		string filePath,
		KiotaGeneratorConfig config,
		string errorMessage)
	{
		var clientName = !string.IsNullOrEmpty(config.ClientClassName)
			? config.ClientClassName
			: "KiotaClient";

		var sb = new StringBuilder();
		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("// Kiota source generator encountered an error and could not generate code.");
		sb.AppendLine("//");
		sb.Append("// Source file: ").AppendLine(filePath ?? "(unknown)");
		sb.Append("// Error: ").AppendLine(SanitizeForComment(errorMessage));
		sb.AppendLine("//");
		sb.AppendLine("// Please check the Error List for diagnostic details (KIOTA* codes).");
		sb.AppendLine("// If this persists, verify your OpenAPI specification is valid and");
		sb.AppendLine("// that AdditionalFiles KiotaClientName metadata is correctly configured.");

		var hintName = clientName + ".KiotaGenerationError.g.cs";

		try
		{
			context.AddSource(hintName, SourceText.From(sb.ToString(), Encoding.UTF8));
		}
		catch (Exception)
		{
			// If even the fallback fails (e.g. duplicate hint name), silently
			// give up — the diagnostic has already been reported.
		}
	}

	/// <summary>
	/// Sanitizes a message for safe inclusion in a C# <c>//</c> comment by
	/// replacing line breaks with spaces and stripping control characters.
	/// </summary>
	private static string SanitizeForComment(string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return "(no details available)";
		}

		// Replace CR/LF with spaces so the entire message stays on one line.
		var sanitized = message
			.Replace("\r\n", " ")
			.Replace('\r', ' ')
			.Replace('\n', ' ');

		// Truncate extremely long messages to avoid bloated source files.
		const int maxLength = 500;
		if (sanitized.Length > maxLength)
		{
			sanitized = sanitized.Substring(0, maxLength) + "…";
		}

		return sanitized;
	}

	// ------------------------------------------------------------------
	// Internal intermediate result type
	// ------------------------------------------------------------------

	/// <summary>
	/// Immutable intermediate result carrying the parsed spec, configuration,
	/// and diagnostics through the incremental pipeline.
	/// <para>
	/// Implements <see cref="IEquatable{T}"/> so the Roslyn pipeline can
	/// detect unchanged results and skip downstream work.
	/// </para>
	/// </summary>
	internal readonly struct KiotaSpecParseResult : IEquatable<KiotaSpecParseResult>
	{
		public KiotaSpecParseResult(
			string filePath,
			KiotaGeneratorConfig config,
			OpenApiParseResult parseResult,
			bool isSkipped)
		{
			FilePath = filePath ?? string.Empty;
			Config = config;
			ParseResult = parseResult;
			IsSkipped = isSkipped;
		}

		/// <summary>The path of the OpenAPI spec file.</summary>
		public string FilePath { get; }

		/// <summary>The resolved configuration for this spec.</summary>
		public KiotaGeneratorConfig Config { get; }

		/// <summary>The result of parsing the OpenAPI document.</summary>
		public OpenApiParseResult ParseResult { get; }

		/// <summary>
		/// When <see langword="true"/>, generation was skipped (e.g. generator disabled).
		/// </summary>
		public bool IsSkipped { get; }

		/// <summary>Creates a skipped result.</summary>
		public static KiotaSpecParseResult Skipped(string filePath)
		{
			return new KiotaSpecParseResult(
				filePath,
				default,
				default,
				isSkipped: true);
		}

		/// <summary>Creates a result with diagnostics but no parsed document.</summary>
		public static KiotaSpecParseResult WithDiagnostics(
			string filePath,
			KiotaGeneratorConfig config,
			ImmutableArray<Diagnostic> diagnostics)
		{
			return new KiotaSpecParseResult(
				filePath,
				config,
				new OpenApiParseResult(null, diagnostics),
				isSkipped: false);
		}

		// ----------------------------------------------------------
		// IEquatable<T> for incremental pipeline caching
		// ----------------------------------------------------------

		/// <inheritdoc />
		public bool Equals(KiotaSpecParseResult other)
		{
			return IsSkipped == other.IsSkipped
				&& string.Equals(FilePath, other.FilePath, StringComparison.Ordinal)
				&& Config.Equals(other.Config)
				&& ParseResult.Equals(other.ParseResult);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
			=> obj is KiotaSpecParseResult other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				hash = (hash * 31) + IsSkipped.GetHashCode();
				hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(FilePath ?? string.Empty);
				hash = (hash * 31) + Config.GetHashCode();
				hash = (hash * 31) + ParseResult.GetHashCode();
				return hash;
			}
		}

		/// <summary>Equality operator.</summary>
		public static bool operator ==(KiotaSpecParseResult left, KiotaSpecParseResult right)
			=> left.Equals(right);

		/// <summary>Inequality operator.</summary>
		public static bool operator !=(KiotaSpecParseResult left, KiotaSpecParseResult right)
			=> !left.Equals(right);
	}
}
