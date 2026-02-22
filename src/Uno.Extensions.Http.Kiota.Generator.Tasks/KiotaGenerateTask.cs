using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.Extensions.Http.Kiota.Generator.Tasks;

/// <summary>
/// MSBuild task that invokes the <c>kiota-gen</c> CLI tool to generate C#
/// client code from an OpenAPI specification file.
/// </summary>
/// <remarks>
/// <para>
/// This task wraps the CLI tool with structured MSBuild diagnostics,
/// providing richer error reporting than a plain <c>&lt;Exec&gt;</c> call.
/// Each CLI output line formatted as <c>error KIOTAXXX: message</c> or
/// <c>warning KIOTAXXX: message</c> is parsed into a proper MSBuild
/// error or warning with subcategory and code.
/// </para>
/// <para>
/// The task supports two tool resolution modes:
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Framework-dependent</b>: set <see cref="KiotaToolDll"/> to the
///       path of <c>kiota-gen.dll</c>. The task invokes <c>dotnet exec</c>.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Self-contained</b>: set <see cref="KiotaToolExe"/> to the path
///       of the platform-specific <c>kiota-gen</c> executable.
///     </description>
///   </item>
/// </list>
/// At least one of these properties must be set.
/// </para>
/// </remarks>
public class KiotaGenerateTask : ToolTask
{
	// ── Regex for structured CLI output parsing ─────────────────────────

	/// <summary>
	/// Matches <c>error KIOTA001: some message</c> or
	/// <c>warning KIOTA002: some message</c> from the CLI stderr output.
	/// </summary>
	private static readonly Regex s_diagnosticPattern = new Regex(
		@"^(error|warning)\s+(KIOTA\d+):\s+(.+)$",
		RegexOptions.Compiled | RegexOptions.IgnoreCase);

	// ── Tool resolution ─────────────────────────────────────────────────

	/// <summary>
	/// Path to the framework-dependent <c>kiota-gen.dll</c>.
	/// When set, the task invokes <c>dotnet exec &lt;dll&gt;</c>.
	/// </summary>
	public string? KiotaToolDll { get; set; }

	/// <summary>
	/// Path to the self-contained <c>kiota-gen</c> executable.
	/// Used when <see cref="KiotaToolDll"/> is not set.
	/// </summary>
	public string? KiotaToolExe { get; set; }

	// ── Required inputs ─────────────────────────────────────────────────

	/// <summary>
	/// Path (or URL) to the OpenAPI description file.
	/// Maps to <c>--openapi</c>.
	/// </summary>
	[Required]
	public string OpenApi { get; set; } = "";

	/// <summary>
	/// Directory where generated C# files are written.
	/// Maps to <c>--output</c>.
	/// </summary>
	[Required]
	public string OutputDirectory { get; set; } = "";

	// ── Optional inputs with defaults ───────────────────────────────────

	/// <summary>
	/// Name of the root client class. Maps to <c>--class-name</c>.
	/// </summary>
	public string ClientClassName { get; set; } = "ApiClient";

	/// <summary>
	/// Root namespace for generated code. Maps to <c>--namespace</c>.
	/// </summary>
	public string Namespace { get; set; } = "ApiSdk";

	/// <summary>
	/// Enable the IBackedModel / IBackingStore pattern.
	/// Maps to <c>--uses-backing-store</c>.
	/// </summary>
	public bool UsesBackingStore { get; set; }

	/// <summary>
	/// Add an AdditionalData dictionary to generated models.
	/// Maps to <c>--include-additional-data</c>.
	/// </summary>
	public bool IncludeAdditionalData { get; set; } = true;

	/// <summary>
	/// Skip emission of deprecated backward-compatibility code.
	/// Maps to <c>--exclude-backward-compatible</c>.
	/// </summary>
	public bool ExcludeBackwardCompatible { get; set; }

	/// <summary>
	/// Access modifier for generated types (<c>Public</c> or <c>Internal</c>).
	/// Maps to <c>--type-access-modifier</c>.
	/// </summary>
	public string TypeAccessModifier { get; set; } = "Public";

	/// <summary>
	/// Semicolon-separated glob patterns for API paths to include.
	/// Maps to <c>--include-patterns</c>.
	/// </summary>
	public string IncludePatterns { get; set; } = "";

	/// <summary>
	/// Semicolon-separated glob patterns for API paths to exclude.
	/// Maps to <c>--exclude-patterns</c>.
	/// </summary>
	public string ExcludePatterns { get; set; } = "";

	/// <summary>
	/// Semicolon-separated fully-qualified <c>ISerializationWriterFactory</c> class names.
	/// Maps to <c>--serializers</c>.
	/// </summary>
	public string Serializers { get; set; } = "";

	/// <summary>
	/// Semicolon-separated fully-qualified <c>IParseNodeFactory</c> class names.
	/// Maps to <c>--deserializers</c>.
	/// </summary>
	public string Deserializers { get; set; } = "";

	/// <summary>
	/// Semicolon-separated structured MIME types.
	/// Maps to <c>--structured-mime-types</c>.
	/// </summary>
	public string StructuredMimeTypes { get; set; } = "";

	/// <summary>
	/// Delete output directory before generating. Maps to <c>--clean-output</c>.
	/// </summary>
	public bool CleanOutput { get; set; }

	/// <summary>
	/// Semicolon-separated OpenAPI validation rule names to suppress.
	/// Maps to <c>--disable-validation-rules</c>.
	/// </summary>
	public string DisableValidationRules { get; set; } = "";

	/// <summary>
	/// Minimum log level (<c>Warning</c>, <c>Information</c>, <c>Error</c>, etc.).
	/// Maps to <c>--log-level</c>.
	/// </summary>
	public string KiotaLogLevel { get; set; } = "Warning";

	// ── Outputs ─────────────────────────────────────────────────────────

	/// <summary>
	/// The list of <c>.cs</c> files generated by the tool.
	/// Populated after successful execution by scanning <see cref="OutputDirectory"/>.
	/// </summary>
	[Output]
	public ITaskItem[] GeneratedFiles { get; set; } = Array.Empty<ITaskItem>();

	// ── ToolTask overrides ──────────────────────────────────────────────

	/// <inheritdoc />
	protected override string ToolName
	{
		get
		{
			if (!string.IsNullOrEmpty(KiotaToolDll))
			{
				return "dotnet";
			}

			if (!string.IsNullOrEmpty(KiotaToolExe))
			{
				return Path.GetFileName(KiotaToolExe);
			}

			return "kiota-gen";
		}
	}

	/// <inheritdoc />
	protected override string GenerateFullPathToTool()
	{
		if (!string.IsNullOrEmpty(KiotaToolDll))
		{
			// Framework-dependent: invoke via `dotnet exec`.
			return "dotnet";
		}

		if (!string.IsNullOrEmpty(KiotaToolExe))
		{
			return KiotaToolExe!;
		}

		Log.LogError(
			subcategory: "Kiota",
			errorCode: "KIOTA003",
			helpKeyword: null,
			file: null,
			lineNumber: 0,
			columnNumber: 0,
			endLineNumber: 0,
			endColumnNumber: 0,
			message: "Neither KiotaToolDll nor KiotaToolExe is set. "
				+ "Cannot resolve the kiota-gen CLI tool. "
				+ "Ensure the Uno.Extensions.Http.Kiota.Generator NuGet package is installed correctly.");

		return string.Empty;
	}

	/// <inheritdoc />
	protected override string GenerateCommandLineCommands()
	{
		var builder = new CommandLineBuilder();

		// Framework-dependent mode: prepend `exec <dll>`.
		if (!string.IsNullOrEmpty(KiotaToolDll))
		{
			builder.AppendSwitch("exec");
			builder.AppendFileNameIfNotNull(KiotaToolDll);
		}

		// Required arguments.
		builder.AppendSwitchIfNotNull("--openapi ", OpenApi);
		builder.AppendSwitchIfNotNull("--output ", OutputDirectory);

		// Naming.
		builder.AppendSwitchIfNotNull("--class-name ", ClientClassName);
		builder.AppendSwitchIfNotNull("--namespace ", Namespace);

		// Feature flags.
		builder.AppendSwitch("--uses-backing-store");
		builder.AppendSwitch(UsesBackingStore ? "true" : "false");

		builder.AppendSwitch("--include-additional-data");
		builder.AppendSwitch(IncludeAdditionalData ? "true" : "false");

		builder.AppendSwitch("--exclude-backward-compatible");
		builder.AppendSwitch(ExcludeBackwardCompatible ? "true" : "false");

		// Type visibility.
		builder.AppendSwitchIfNotNull("--type-access-modifier ", TypeAccessModifier);

		// Optional semicolon-separated array arguments.
		// The CLI's ExpandSemicolonSeparated handles empty strings gracefully.
		AppendIfNotEmpty(builder, "--include-patterns", IncludePatterns);
		AppendIfNotEmpty(builder, "--exclude-patterns", ExcludePatterns);
		AppendIfNotEmpty(builder, "--serializers", Serializers);
		AppendIfNotEmpty(builder, "--deserializers", Deserializers);
		AppendIfNotEmpty(builder, "--structured-mime-types", StructuredMimeTypes);

		// Output control.
		builder.AppendSwitch("--clean-output");
		builder.AppendSwitch(CleanOutput ? "true" : "false");

		// Validation rules.
		AppendIfNotEmpty(builder, "--disable-validation-rules", DisableValidationRules);

		// Log level.
		builder.AppendSwitchIfNotNull("--log-level ", KiotaLogLevel);

		return builder.ToString();
	}

	/// <inheritdoc />
	protected override MessageImportance StandardOutputLoggingImportance => MessageImportance.Low;

	/// <inheritdoc />
	protected override MessageImportance StandardErrorLoggingImportance => MessageImportance.High;

	/// <summary>
	/// Parses a single line from the tool's standard error stream.
	/// Recognizes the <c>error KIOTAXXX: message</c> and
	/// <c>warning KIOTAXXX: message</c> patterns emitted by the CLI
	/// and converts them into structured MSBuild diagnostics.
	/// </summary>
	protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
	{
		if (string.IsNullOrWhiteSpace(singleLine))
		{
			return;
		}

		var match = s_diagnosticPattern.Match(singleLine);
		if (match.Success)
		{
			var severity = match.Groups[1].Value;
			var code = match.Groups[2].Value;
			var message = match.Groups[3].Value;

			if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
			{
				Log.LogError(
					subcategory: "Kiota",
					errorCode: code,
					helpKeyword: null,
					file: OpenApi,
					lineNumber: 0,
					columnNumber: 0,
					endLineNumber: 0,
					endColumnNumber: 0,
					message: message);
			}
			else
			{
				Log.LogWarning(
					subcategory: "Kiota",
					warningCode: code,
					helpKeyword: null,
					file: OpenApi,
					lineNumber: 0,
					columnNumber: 0,
					endLineNumber: 0,
					endColumnNumber: 0,
					message: message);
			}

			return;
		}

		// Fall back to the base implementation for unrecognized output.
		base.LogEventsFromTextOutput(singleLine, messageImportance);
	}

	/// <inheritdoc />
	protected override bool ValidateParameters()
	{
		if (string.IsNullOrEmpty(OpenApi))
		{
			Log.LogError(
				subcategory: "Kiota",
				errorCode: "KIOTA004",
				helpKeyword: null,
				file: null,
				lineNumber: 0,
				columnNumber: 0,
				endLineNumber: 0,
				endColumnNumber: 0,
				message: "The OpenApi property is required. "
					+ "Specify the path to an OpenAPI description file.");
			return false;
		}

		if (string.IsNullOrEmpty(OutputDirectory))
		{
			Log.LogError(
				subcategory: "Kiota",
				errorCode: "KIOTA005",
				helpKeyword: null,
				file: null,
				lineNumber: 0,
				columnNumber: 0,
				endLineNumber: 0,
				endColumnNumber: 0,
				message: "The OutputDirectory property is required. "
					+ "Specify the directory for generated C# files.");
			return false;
		}

		if (string.IsNullOrEmpty(KiotaToolDll) && string.IsNullOrEmpty(KiotaToolExe))
		{
			Log.LogError(
				subcategory: "Kiota",
				errorCode: "KIOTA003",
				helpKeyword: null,
				file: null,
				lineNumber: 0,
				columnNumber: 0,
				endLineNumber: 0,
				endColumnNumber: 0,
				message: "Neither KiotaToolDll nor KiotaToolExe is set. "
					+ "Cannot resolve the kiota-gen CLI tool. "
					+ "Ensure the Uno.Extensions.Http.Kiota.Generator NuGet package is installed correctly.");
			return false;
		}

		return base.ValidateParameters();
	}

	/// <summary>
	/// Called after successful execution. Scans the output directory for
	/// generated <c>.cs</c> files and populates <see cref="GeneratedFiles"/>.
	/// </summary>
	protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
	{
		// Ensure the output directory exists before invoking the tool.
		if (!string.IsNullOrEmpty(OutputDirectory))
		{
			Directory.CreateDirectory(OutputDirectory);
		}

		var exitCode = base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

		if (exitCode == 0)
		{
			CollectGeneratedFiles();
		}

		return exitCode;
	}

	// ── Private helpers ─────────────────────────────────────────────────

	/// <summary>
	/// Appends a <c>--flag "value"</c> switch only when <paramref name="value"/>
	/// is not null or empty. This avoids passing empty strings for
	/// optional semicolon-separated array arguments.
	/// </summary>
	private static void AppendIfNotEmpty(CommandLineBuilder builder, string switchName, string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			builder.AppendSwitchIfNotNull(switchName + " ", value);
		}
	}

	/// <summary>
	/// Scans <see cref="OutputDirectory"/> for <c>*.cs</c> files and
	/// populates the <see cref="GeneratedFiles"/> output property.
	/// </summary>
	private void CollectGeneratedFiles()
	{
		if (string.IsNullOrEmpty(OutputDirectory) || !Directory.Exists(OutputDirectory))
		{
			GeneratedFiles = Array.Empty<ITaskItem>();
			return;
		}

		var files = Directory.GetFiles(OutputDirectory, "*.cs", SearchOption.AllDirectories);
		var items = new List<ITaskItem>(files.Length);

		foreach (var file in files)
		{
			items.Add(new TaskItem(file));
		}

		GeneratedFiles = items.ToArray();

		Log.LogMessage(
			MessageImportance.Low,
			"Kiota generated {0} file(s) in '{1}'.",
			items.Count,
			OutputDirectory);
	}
}
