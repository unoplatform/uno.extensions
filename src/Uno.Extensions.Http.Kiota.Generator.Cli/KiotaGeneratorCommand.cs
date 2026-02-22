using System.CommandLine;
using System.CommandLine.Invocation;
using Kiota.Builder;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Settings;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Http.Kiota.Generator.Cli;

/// <summary>
/// <see cref="System.CommandLine.RootCommand"/> definition for the <c>kiota-gen</c> CLI.
/// Maps every <c>--flag</c> to <see cref="GeneratorOptions"/>, projects the options onto
/// <see cref="GenerationConfiguration"/>, then invokes
/// <see cref="KiotaBuilder.GenerateClientAsync(CancellationToken)"/>.
/// </summary>
internal sealed class KiotaGeneratorCommand : RootCommand
{
	// ── Options ──────────────────────────────────────────────────────────

	private readonly Option<string> _openApiOption = new("--openapi", "Path (or URL) to the OpenAPI description file.")
	{
		IsRequired = true,
	};

	private readonly Option<string> _outputOption = new("--output", "Directory where generated C# files are written.")
	{
		IsRequired = true,
	};

	private readonly Option<string> _classNameOption = new(
		"--class-name",
		getDefaultValue: () => "ApiClient",
		description: "Name of the root client class.");

	private readonly Option<string> _namespaceOption = new(
		"--namespace",
		getDefaultValue: () => "ApiSdk",
		description: "Root namespace for generated code.");

	private readonly Option<bool> _usesBackingStoreOption = new(
		"--uses-backing-store",
		getDefaultValue: () => false,
		description: "Enable the IBackedModel / IBackingStore pattern.");

	private readonly Option<bool> _includeAdditionalDataOption = new(
		"--include-additional-data",
		getDefaultValue: () => true,
		description: "Add an AdditionalData dictionary to generated models.");

	private readonly Option<bool> _excludeBackwardCompatibleOption = new(
		"--exclude-backward-compatible",
		getDefaultValue: () => false,
		description: "Skip emission of deprecated backward-compatibility code.");

	private readonly Option<string> _typeAccessModifierOption = new(
		"--type-access-modifier",
		getDefaultValue: () => "Public",
		description: "Access modifier applied to all generated types (Public or Internal).");

	private readonly Option<string[]> _includePatternsOption = new(
		"--include-patterns",
		getDefaultValue: () => [],
		description: "Glob patterns selecting which API paths to include (semicolon-separated).")
	{
		AllowMultipleArgumentsPerToken = true,
	};

	private readonly Option<string[]> _excludePatternsOption = new(
		"--exclude-patterns",
		getDefaultValue: () => [],
		description: "Glob patterns selecting which API paths to exclude (semicolon-separated).")
	{
		AllowMultipleArgumentsPerToken = true,
	};

	private readonly Option<string[]> _serializersOption = new(
		"--serializers",
		getDefaultValue: () => [],
		description: "Fully-qualified ISerializationWriterFactory class names (semicolon-separated).")
	{
		AllowMultipleArgumentsPerToken = true,
	};

	private readonly Option<string[]> _deserializersOption = new(
		"--deserializers",
		getDefaultValue: () => [],
		description: "Fully-qualified IParseNodeFactory class names (semicolon-separated).")
	{
		AllowMultipleArgumentsPerToken = true,
	};

	private readonly Option<string[]> _structuredMimeTypesOption = new(
		"--structured-mime-types",
		getDefaultValue: () => [],
		description: "Structured MIME types the client can handle (semicolon-separated).")
	{
		AllowMultipleArgumentsPerToken = true,
	};

	private readonly Option<bool> _cleanOutputOption = new(
		"--clean-output",
		getDefaultValue: () => false,
		description: "Delete the output directory before generating new files.");

	private readonly Option<string[]> _disableValidationRulesOption = new(
		"--disable-validation-rules",
		getDefaultValue: () => [],
		description: "Names of OpenAPI validation rules to suppress (semicolon-separated).")
	{
		AllowMultipleArgumentsPerToken = true,
	};

	private readonly Option<LogLevel> _logLevelOption = new(
		"--log-level",
		getDefaultValue: () => LogLevel.Warning,
		description: "Minimum log level for generator diagnostics written to stderr.");

	// ── Constructor ──────────────────────────────────────────────────────

	/// <summary>
	/// Initializes a new instance of the <see cref="KiotaGeneratorCommand"/> class,
	/// registering all CLI options and the async handler.
	/// </summary>
	/// <param name="loggerFactory">
	/// Logger factory used to create the <see cref="ILogger{KiotaBuilder}"/>.
	/// When <see langword="null"/>, a default console logger is created that
	/// formats output as MSBuild-parseable messages.
	/// </param>
	public KiotaGeneratorCommand(ILoggerFactory? loggerFactory = null)
		: base("Generates C# client code from an OpenAPI description using Kiota.")
	{
		AddOption(_openApiOption);
		AddOption(_outputOption);
		AddOption(_classNameOption);
		AddOption(_namespaceOption);
		AddOption(_usesBackingStoreOption);
		AddOption(_includeAdditionalDataOption);
		AddOption(_excludeBackwardCompatibleOption);
		AddOption(_typeAccessModifierOption);
		AddOption(_includePatternsOption);
		AddOption(_excludePatternsOption);
		AddOption(_serializersOption);
		AddOption(_deserializersOption);
		AddOption(_structuredMimeTypesOption);
		AddOption(_cleanOutputOption);
		AddOption(_disableValidationRulesOption);
		AddOption(_logLevelOption);

		this.SetHandler(ExecuteAsync);
	}

	// ── Handler ──────────────────────────────────────────────────────────

	private async Task<int> ExecuteAsync(InvocationContext context)
	{
		var ct = context.GetCancellationToken();

		// 1. Parse all option values into GeneratorOptions.
		var options = ParseOptions(context);

		// 2. Build GenerationConfiguration from the parsed options.
		var config = BuildConfiguration(options);

		// 3. Create logger and invoke KiotaBuilder.
		var logLevel = context.ParseResult.GetValueForOption(_logLevelOption);
		var loggerFactory = new MsBuildLoggerFactory(logLevel);
		var logger = loggerFactory.CreateLogger<KiotaBuilder>();

		using var httpClient = new HttpClient();
		var kiotaBuilder = new KiotaBuilder(
			logger,
			config,
			httpClient,
			useKiotaConfig: false,
			new SettingsFileManagementService());

		try
		{
			var success = await kiotaBuilder.GenerateClientAsync(ct);
			if (success)
			{
				logger.LogInformation("Code generation completed successfully. Output: {OutputPath}", config.OutputPath);
				return 0;
			}

			logger.LogError("Code generation failed.");
			return 1;
		}
		catch (OperationCanceledException)
		{
			logger.LogWarning("Code generation was cancelled.");
			return 1;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Unhandled error during code generation: {Message}", ex.Message);
			return 1;
		}
	}

	// ── Private helpers ──────────────────────────────────────────────────

	private GeneratorOptions ParseOptions(InvocationContext context)
	{
		var result = context.ParseResult;

		return new GeneratorOptions
		{
			OpenApiPath = result.GetValueForOption(_openApiOption)!,
			OutputPath = result.GetValueForOption(_outputOption)!,
			ClassName = result.GetValueForOption(_classNameOption) ?? "ApiClient",
			Namespace = result.GetValueForOption(_namespaceOption) ?? "ApiSdk",
			UsesBackingStore = result.GetValueForOption(_usesBackingStoreOption),
			IncludeAdditionalData = result.GetValueForOption(_includeAdditionalDataOption),
			ExcludeBackwardCompatible = result.GetValueForOption(_excludeBackwardCompatibleOption),
			TypeAccessModifier = result.GetValueForOption(_typeAccessModifierOption) ?? "Public",
			IncludePatterns = ExpandSemicolonSeparated(result.GetValueForOption(_includePatternsOption)),
			ExcludePatterns = ExpandSemicolonSeparated(result.GetValueForOption(_excludePatternsOption)),
			Serializers = ExpandSemicolonSeparated(result.GetValueForOption(_serializersOption)),
			Deserializers = ExpandSemicolonSeparated(result.GetValueForOption(_deserializersOption)),
			StructuredMimeTypes = ExpandSemicolonSeparated(result.GetValueForOption(_structuredMimeTypesOption)),
			CleanOutput = result.GetValueForOption(_cleanOutputOption),
			DisableValidationRules = ExpandSemicolonSeparated(result.GetValueForOption(_disableValidationRulesOption)),
			LogLevel = result.GetValueForOption(_logLevelOption),
		};
	}

	/// <summary>
	/// Projects <see cref="GeneratorOptions"/> onto <see cref="GenerationConfiguration"/>,
	/// which is the canonical input type for <see cref="KiotaBuilder"/>.
	/// </summary>
	internal static GenerationConfiguration BuildConfiguration(GeneratorOptions options)
	{
		var config = new GenerationConfiguration
		{
			OpenAPIFilePath = options.OpenApiPath,
			OutputPath = options.OutputPath,
			Language = GenerationLanguage.CSharp,
			ClientClassName = options.ClassName,
			ClientNamespaceName = options.Namespace,
			UsesBackingStore = options.UsesBackingStore,
			IncludeAdditionalData = options.IncludeAdditionalData,
			ExcludeBackwardCompatible = options.ExcludeBackwardCompatible,
			TypeAccessModifier = ParseAccessModifier(options.TypeAccessModifier),
			CleanOutput = options.CleanOutput,
		};

		if (options.IncludePatterns is { Length: > 0 })
		{
			config.IncludePatterns = new HashSet<string>(options.IncludePatterns, StringComparer.OrdinalIgnoreCase);
		}

		if (options.ExcludePatterns is { Length: > 0 })
		{
			config.ExcludePatterns = new HashSet<string>(options.ExcludePatterns, StringComparer.OrdinalIgnoreCase);
		}

		if (options.Serializers is { Length: > 0 })
		{
			config.Serializers = new HashSet<string>(options.Serializers, StringComparer.Ordinal);
		}

		if (options.Deserializers is { Length: > 0 })
		{
			config.Deserializers = new HashSet<string>(options.Deserializers, StringComparer.Ordinal);
		}

		if (options.StructuredMimeTypes is { Length: > 0 })
		{
			config.StructuredMimeTypes = new StructuredMimeTypesCollection(options.StructuredMimeTypes);
		}

		if (options.DisableValidationRules is { Length: > 0 })
		{
			config.DisabledValidationRules = new HashSet<string>(options.DisableValidationRules, StringComparer.OrdinalIgnoreCase);
		}

		return config;
	}

	/// <summary>
	/// Parses the <c>--type-access-modifier</c> string value into the
	/// <see cref="AccessModifier"/> enum used by <see cref="GenerationConfiguration"/>.
	/// </summary>
	private static AccessModifier ParseAccessModifier(string value)
	{
		return value?.ToUpperInvariant() switch
		{
			"PUBLIC" => AccessModifier.Public,
			"INTERNAL" => AccessModifier.Internal,
			_ => AccessModifier.Public,
		};
	}

	/// <summary>
	/// Expands an array that may contain semicolon-separated values into a
	/// flat array of individual entries. This allows MSBuild targets to pass
	/// <c>--include-patterns "glob1;glob2"</c> as a single argument while
	/// also supporting repeated <c>--include-patterns glob1 --include-patterns glob2</c>.
	/// </summary>
	private static string[] ExpandSemicolonSeparated(string[]? values)
	{
		if (values is null or { Length: 0 })
		{
			return [];
		}

		var expanded = new List<string>();
		foreach (var value in values)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				continue;
			}

			foreach (var part in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				expanded.Add(part);
			}
		}

		return expanded.ToArray();
	}

	// ── MSBuild Logger ───────────────────────────────────────────────────

	/// <summary>
	/// Lightweight <see cref="ILoggerFactory"/> that creates loggers
	/// formatting output as MSBuild-parseable messages.
	/// Avoids a dependency on the concrete <c>Microsoft.Extensions.Logging</c> package.
	/// </summary>
	private sealed class MsBuildLoggerFactory(LogLevel minimumLevel) : ILoggerFactory
	{
		public ILogger CreateLogger(string categoryName) => new MsBuildLogger(categoryName, minimumLevel);

		public void AddProvider(ILoggerProvider provider)
		{
			// Not used — this factory only produces MsBuildLoggers.
		}

		public void Dispose()
		{
			// No resources to release.
		}
	}

	private sealed class MsBuildLogger(string categoryName, LogLevel minimumLevel) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			var message = formatter(state, exception);
			if (string.IsNullOrWhiteSpace(message))
			{
				return;
			}

			// Format in MSBuild-parseable format for ConsoleToMSBuild="true".
			switch (logLevel)
			{
				case LogLevel.Error:
				case LogLevel.Critical:
					Console.Error.WriteLine($"error KIOTA001: {message}");
					break;

				case LogLevel.Warning:
					Console.Error.WriteLine($"warning KIOTA002: {message}");
					break;

				case LogLevel.Information:
					// Standard output at normal importance.
					Console.WriteLine(message);
					break;

				default:
					// Debug/Trace go to stderr for diagnostics but not MSBuild.
					Console.Error.WriteLine($"# [{categoryName}] {message}");
					break;
			}
		}
	}
}
