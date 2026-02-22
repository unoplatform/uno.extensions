using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Http.Kiota.SourceGenerator;

namespace Uno.Extensions.Http.Kiota.Generator.Tests.Parity;

/// <summary>
/// Base class for golden-file parity tests that compare source-generator
/// output against reference files produced by the Kiota CLI.
/// <para>
/// Subclasses provide spec-specific configuration (spec path, client name,
/// namespace, golden file directory) and then call <see cref="RunGenerator"/>
/// to exercise the full pipeline (parse → CodeDOM → refine → emit). The
/// emitted sources are compared file-by-file against the golden reference
/// using <see cref="AssertParityForAllFiles"/>.
/// </para>
/// <para>
/// Normalization is applied to both golden and generated sources before
/// comparison to account for acceptable differences:
/// <list type="bullet">
///   <item>Kiota version strings in <c>[GeneratedCode]</c> attributes</item>
///   <item>Trailing whitespace and line-ending differences (CR/LF vs LF)</item>
///   <item>Trailing blank lines at end of file</item>
/// </list>
/// </para>
/// </summary>
public abstract class ParityTestBase
{
	// ------------------------------------------------------------------
	// Paths — resolved relative to the test output directory
	// ------------------------------------------------------------------

	/// <summary>Root directory containing test OpenAPI spec files.</summary>
	protected static string TestDataDir =>
		Path.Combine(AppContext.BaseDirectory, "TestData");

	/// <summary>Root directory containing golden reference files.</summary>
	protected static string GoldenFilesDir =>
		Path.Combine(AppContext.BaseDirectory, "GoldenFiles");

	// ------------------------------------------------------------------
	// Abstract members — subclasses define spec-specific configuration
	// ------------------------------------------------------------------

	/// <summary>
	/// The name of the test spec file (e.g., <c>"petstore.json"</c>).
	/// Must exist in <see cref="TestDataDir"/>.
	/// </summary>
	protected abstract string SpecFileName { get; }

	/// <summary>
	/// The client class name to pass to the generator
	/// (e.g., <c>"PetStoreClient"</c>).
	/// </summary>
	protected abstract string ClientName { get; }

	/// <summary>
	/// The root namespace for generated types
	/// (e.g., <c>"PetStore.Client"</c>).
	/// </summary>
	protected abstract string ClientNamespace { get; }

	/// <summary>
	/// The golden file subdirectory name (e.g., <c>"petstore"</c>).
	/// Must exist under <see cref="GoldenFilesDir"/>.
	/// </summary>
	protected abstract string GoldenSubdirectory { get; }

	// ------------------------------------------------------------------
	// Generator execution
	// ------------------------------------------------------------------

	/// <summary>
	/// Runs the <see cref="KiotaSourceGenerator"/> against the configured
	/// spec and returns the generated sources keyed by hint name.
	/// </summary>
	/// <returns>
	/// A dictionary mapping hint names (e.g., <c>"PetStore.Client.Models.Pet.g.cs"</c>)
	/// to their generated C# source text.
	/// </returns>
	protected GeneratorRunResult RunGenerator()
	{
		var specPath = Path.Combine(TestDataDir, SpecFileName);
		var specContent = File.ReadAllText(specPath);

		var additionalText = new InMemoryAdditionalText(specPath, specContent);

		var fileOptions = ImmutableDictionary.CreateRange(new[]
		{
			KeyValuePair.Create(
				"build_metadata.AdditionalFiles.KiotaClientName", ClientName),
			KeyValuePair.Create(
				"build_metadata.AdditionalFiles.KiotaNamespace", ClientNamespace),
			KeyValuePair.Create(
				"build_metadata.AdditionalFiles.KiotaExcludeBackwardCompatible", "true"),
		});

		var globalOptions = ImmutableDictionary.CreateRange(new[]
		{
			KeyValuePair.Create("build_property.KiotaGenerator_Enabled", "true"),
		});

		var optionsProvider = new TestAnalyzerConfigOptionsProvider(
			globalOptions, fileOptions);

		var generator = new KiotaSourceGenerator();

		var driver = CSharpGeneratorDriver.Create(generator)
			.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalText))
			.WithUpdatedAnalyzerConfigOptions(optionsProvider);

		var compilation = CreateMinimalCompilation();

		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		var generatorErrors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();

		generatorErrors.Should().BeEmpty(
			"the generator should not report errors for valid test specs");

		// Collect generated source files
		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();

		var generatedSources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var tree in generatedTrees)
		{
			// The tree.FilePath for generated sources follows the pattern
			// set by the hint name. Extract just the hint name portion.
			var hintName = Path.GetFileName(tree.FilePath);
			var source = tree.GetText().ToString();
			generatedSources[hintName] = source;
		}

		return new GeneratorRunResult(generatedSources, diagnostics);
	}

	// ------------------------------------------------------------------
	// Golden file loading
	// ------------------------------------------------------------------

	/// <summary>
	/// Loads all <c>.cs</c> golden files from the configured subdirectory.
	/// </summary>
	/// <returns>
	/// A dictionary mapping relative file paths (e.g., <c>"Models/Pet.cs"</c>)
	/// to their golden source text.
	/// </returns>
	protected Dictionary<string, string> LoadGoldenFiles()
	{
		var goldenDir = Path.Combine(GoldenFilesDir, GoldenSubdirectory);
		goldenDir = Path.GetFullPath(goldenDir);

		Directory.Exists(goldenDir).Should().BeTrue(
			$"golden file directory '{goldenDir}' should exist");

		var files = Directory.GetFiles(goldenDir, "*.cs", SearchOption.AllDirectories);

		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var file in files)
		{
			var relativePath = GetRelativePath(goldenDir, file);
			var content = File.ReadAllText(file);
			result[relativePath] = content;
		}

		return result;
	}

	// ------------------------------------------------------------------
	// Hint name → golden file path mapping
	// ------------------------------------------------------------------

	/// <summary>
	/// Converts a source-generator hint name to the corresponding golden
	/// file relative path.
	/// <para>
	/// Hint names follow the pattern <c>{Namespace}.{Type}.g.cs</c>.
	/// Golden files follow a folder structure where namespace segments
	/// beyond the root namespace become directory components.
	/// </para>
	/// <para>
	/// Example: hint name <c>"PetStore.Client.Models.Pet.g.cs"</c> with
	/// root namespace <c>"PetStore.Client"</c> maps to golden path
	/// <c>"Models/Pet.cs"</c>.
	/// </para>
	/// </summary>
	/// <param name="hintName">The generator hint name.</param>
	/// <returns>The relative golden file path, or <see langword="null"/>
	/// if the hint name does not start with the expected namespace.</returns>
	protected string? MapHintNameToGoldenPath(string hintName)
	{
		// Strip ".g.cs" suffix
		const string suffix = ".g.cs";
		if (!hintName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		var qualifiedName = hintName.Substring(0, hintName.Length - suffix.Length);

		// Strip the root namespace prefix
		var nsPrefix = ClientNamespace + ".";
		if (!qualifiedName.StartsWith(nsPrefix, StringComparison.Ordinal))
		{
			// Could be the client class itself at the root namespace level
			if (qualifiedName.Equals(ClientNamespace, StringComparison.Ordinal))
			{
				return null; // Should not happen (namespace itself is not a file)
			}

			return null;
		}

		var remainingDots = qualifiedName.Substring(nsPrefix.Length);

		// Convert dot-separated segments to path separators
		var relativePath = remainingDots.Replace('.', Path.DirectorySeparatorChar) + ".cs";
		return relativePath;
	}

	/// <summary>
	/// Builds a mapping from golden file relative paths to generated
	/// source content, using <see cref="MapHintNameToGoldenPath"/> for
	/// the key translation.
	/// </summary>
	/// <param name="generatedSources">
	/// The generated sources keyed by hint name from <see cref="RunGenerator"/>.
	/// </param>
	/// <returns>
	/// Generated sources re-keyed by golden file relative paths.
	/// Entries whose hint names could not be mapped are excluded.
	/// </returns>
	protected Dictionary<string, string> MapGeneratedToGoldenPaths(
		Dictionary<string, string> generatedSources)
	{
		var mapped = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var (hintName, source) in generatedSources)
		{
			var goldenPath = MapHintNameToGoldenPath(hintName);
			if (goldenPath != null)
			{
				mapped[goldenPath] = source;
			}
		}

		return mapped;
	}

	// ------------------------------------------------------------------
	// Normalization
	// ------------------------------------------------------------------

	/// <summary>
	/// Regex matching Kiota version strings in <c>[GeneratedCode("Kiota", "...")]</c>
	/// attributes. Both the CLI and the source generator may emit different
	/// version numbers, which is an acceptable difference.
	/// </summary>
	private static readonly Regex VersionPattern = new(
		@"\[global::System\.CodeDom\.Compiler\.GeneratedCode\(""Kiota"",\s*""[^""]*""\)\]",
		RegexOptions.Compiled);

	/// <summary>
	/// Regex matching <c>if(ReferenceEquals(x, null)) throw new ArgumentNullException(nameof(x));</c>
	/// null guard pattern used by older Kiota CLI versions. The newer pattern is
	/// <c>_ = x ?? throw new ArgumentNullException(nameof(x));</c>. Both are
	/// semantically identical and should not cause parity failures.
	/// </summary>
	private static readonly Regex ReferenceEqualsNullGuardPattern = new(
		@"if\(ReferenceEquals\((\w+),\s*null\)\)\s*throw\s+new\s+ArgumentNullException\(nameof\(\1\)\);",
		RegexOptions.Compiled);

	/// <summary>
	/// Regex matching deprecated backward-compatibility <c>RequestConfiguration</c>
	/// inner classes emitted by the Kiota CLI but intentionally omitted by the
	/// source generator. These are empty shims marked <c>[Obsolete]</c> that
	/// extend <c>RequestConfiguration&lt;T&gt;</c>. Format:
	/// <code>
	/// /// &lt;summary&gt;...&lt;/summary&gt;
	/// [Obsolete("This class is deprecated...")]
	/// [GeneratedCode(...)]
	/// public partial class XxxRequestConfiguration : RequestConfiguration&lt;T&gt;
	/// {
	/// }
	/// </code>
	/// </summary>
	private static readonly Regex DeprecatedRequestConfigPattern = new(
		@"[ \t]*///\s*<summary>\s*\n"
		+ @"[ \t]*///.*?\n"
		+ @"[ \t]*///\s*</summary>\s*\n"
		+ @"[ \t]*\[Obsolete\(""This class is deprecated.*?""\)\]\s*\n"
		+ @"[ \t]*\[global::System\.CodeDom\.Compiler\.GeneratedCode\([^)]*\)\]\s*\n"
		+ @"[ \t]*public partial class \w+RequestConfiguration\s*:.*?\n"
		+ @"[ \t]*\{\s*\n"
		+ @"[ \t]*\}\s*\n",
		RegexOptions.Compiled);

	/// <summary>
	/// Regex matching deprecated query parameter property pairs emitted by
	/// the Kiota CLI. These are <c>[Obsolete]</c> string-typed properties
	/// paired with a typed enum replacement (e.g., <c>Status</c> deprecated
	/// in favour of <c>StatusAsPetStatus</c>). The source generator emits
	/// only the typed enum property.
	/// </summary>
	private static readonly Regex DeprecatedQueryParamPropertyPattern = new(
		@"[ \t]*///.*?\n"
		+ @"[ \t]*\[Obsolete\(""This property is deprecated.*?""\)\]\s*\n"
		+ @"(?:"
		+   @"[ \t]*#if .*?\n"
		+   @"[ \t]*#nullable enable\s*\n"
		+   @"[ \t]*\[QueryParameter\([^)]*\)\]\s*\n"
		+   @"[ \t]*public string\?\s+\w+\s*\{\s*get;\s*set;\s*\}\s*\n"
		+   @"[ \t]*#nullable restore\s*\n"
		+   @"[ \t]*#else\s*\n"
		+   @"[ \t]*\[QueryParameter\([^)]*\)\]\s*\n"
		+   @"[ \t]*public string\s+\w+\s*\{\s*get;\s*set;\s*\}\s*\n"
		+   @"[ \t]*#endif\s*\n"
		+ @"|"
		+   @"[ \t]*\[QueryParameter\([^)]*\)\]\s*\n"
		+   @"[ \t]*public string\s+\w+\s*\{\s*get;\s*set;\s*\}\s*\n"
		+ @")",
		RegexOptions.Compiled);

	/// <summary>
	/// Normalizes source text for comparison by removing acceptable
	/// differences between golden files and generator output.
	/// <list type="bullet">
	///   <item>Normalizes line endings to <c>\n</c> (LF).</item>
	///   <item>Removes trailing whitespace from each line.</item>
	///   <item>Replaces version strings in <c>[GeneratedCode]</c>
	///         attributes with a placeholder.</item>
	///   <item>Normalizes null guard patterns: the older <c>if(ReferenceEquals(x, null))</c>
	///         form is converted to <c>_ = x ?? throw</c> so golden files from
	///         different Kiota CLI versions compare equally.</item>
	///   <item>Strips deprecated backward-compat <c>RequestConfiguration</c>
	///         inner classes (marked <c>[Obsolete]</c>) that the Kiota CLI emits
	///         but the source generator intentionally omits.</item>
	///   <item>Strips deprecated string-typed query parameter properties
	///         (marked <c>[Obsolete]</c>) that are superseded by typed enum
	///         properties in the same class.</item>
	///   <item>Removes trailing blank lines at end of file.</item>
	/// </list>
	/// </summary>
	/// <param name="source">The source text to normalize.</param>
	/// <returns>The normalized source text.</returns>
	public static string Normalize(string source)
	{
		if (string.IsNullOrEmpty(source))
		{
			return string.Empty;
		}

		// Normalize line endings to LF
		var normalized = source.Replace("\r\n", "\n").Replace("\r", "\n");

		// Replace version strings with placeholder
		normalized = VersionPattern.Replace(
			normalized,
			@"[global::System.CodeDom.Compiler.GeneratedCode(""Kiota"", ""VERSION"")]");

		// Normalize null guard patterns: convert older ReferenceEquals form
		// to the modern discard-throw form so golden files from different
		// Kiota CLI versions match our emitter output.
		normalized = ReferenceEqualsNullGuardPattern.Replace(
			normalized,
			"_ = $1 ?? throw new ArgumentNullException(nameof($1));");

		// Strip deprecated backward-compat RequestConfiguration inner classes.
		normalized = DeprecatedRequestConfigPattern.Replace(normalized, string.Empty);

		// Strip deprecated string-typed query parameter properties.
		normalized = DeprecatedQueryParamPropertyPattern.Replace(normalized, string.Empty);

		// Normalize enum QP property names: Kiota CLI emits "StatusAsPetStatus"
		// to avoid collision with the deprecated string "Status" property. After
		// stripping the deprecated property above, our generator simply emits
		// "Status". Detect the "XAsEnumType" pattern and normalize to "X".
		normalized = NormalizeEnumQueryParameterNames(normalized);

		// Trim trailing whitespace per line and trailing blank lines
		var lines = normalized.Split('\n');
		var trimmedLines = lines.Select(l => l.TrimEnd()).ToList();

		// Remove trailing empty lines
		while (trimmedLines.Count > 0 && string.IsNullOrEmpty(trimmedLines[trimmedLines.Count - 1]))
		{
			trimmedLines.RemoveAt(trimmedLines.Count - 1);
		}

		return string.Join("\n", trimmedLines);
	}

	/// <summary>
	/// Regex matching the Kiota CLI enum query-parameter naming convention
	/// where a typed property is named <c>XxxAsEnumType</c> to avoid
	/// colliding with a deprecated string-typed <c>Xxx</c> property.
	/// After the deprecated property is stripped, we normalize the name
	/// to just the original query parameter name.
	/// <para>
	/// Pattern: <c>public global::Ns.EnumType? StatusAsPetStatus { get; set; }</c>
	/// becomes: <c>public global::Ns.EnumType? Status { get; set; }</c>.
	/// </para>
	/// </summary>
	private static readonly Regex EnumAsTypePropertyPattern = new(
		@"(public\s+global::[.\w]+\?\s+)(\w+)As\w+(\s*\{)",
		RegexOptions.Compiled);

	/// <summary>
	/// Normalizes enum query-parameter property names by removing the
	/// <c>AsEnumType</c> suffix that Kiota CLI adds for backward
	/// compatibility with deprecated string-typed properties.
	/// </summary>
	private static string NormalizeEnumQueryParameterNames(string source)
	{
		return EnumAsTypePropertyPattern.Replace(source, "$1$2$3");
	}

	// ------------------------------------------------------------------
	// Assertions
	// ------------------------------------------------------------------

	/// <summary>
	/// Asserts that every golden file has a matching generated source file
	/// and that content matches after normalization.
	/// <para>
	/// Reports all mismatches in a single assertion failure with a
	/// detailed diff for each file.
	/// </para>
	/// </summary>
	/// <param name="generatedSources">
	/// Generated sources keyed by hint name from <see cref="RunGenerator"/>.
	/// </param>
	/// <param name="goldenFiles">
	/// Golden files keyed by relative path from <see cref="LoadGoldenFiles"/>.
	/// </param>
	protected void AssertParityForAllFiles(
		Dictionary<string, string> generatedSources,
		Dictionary<string, string> goldenFiles)
	{
		var mappedGenerated = MapGeneratedToGoldenPaths(generatedSources);
		var failures = new List<string>();

		foreach (var (goldenPath, goldenSource) in goldenFiles)
		{
			if (!mappedGenerated.TryGetValue(goldenPath, out var generatedSource))
			{
				failures.Add($"MISSING: Golden file '{goldenPath}' has no corresponding generated source.");
				continue;
			}

			var normalizedGolden = Normalize(goldenSource);
			var normalizedGenerated = Normalize(generatedSource);

			if (!string.Equals(normalizedGolden, normalizedGenerated, StringComparison.Ordinal))
			{
				var diff = CreateDiffSummary(goldenPath, normalizedGolden, normalizedGenerated);
				failures.Add(diff);
			}
		}

		if (failures.Count > 0)
		{
			var message = $"Parity check failed for {failures.Count} file(s):\n\n"
				+ string.Join("\n\n---\n\n", failures);
			Assert.Fail(message);
		}
	}

	/// <summary>
	/// Asserts parity for a single golden file against generated output.
	/// Useful for testing one file at a time during iterative development.
	/// </summary>
	/// <param name="goldenRelativePath">
	/// The relative path of the golden file (e.g., <c>"Models/Pet.cs"</c>).
	/// </param>
	/// <param name="generatedSources">
	/// Generated sources keyed by hint name from <see cref="RunGenerator"/>.
	/// </param>
	protected void AssertParityForFile(
		string goldenRelativePath,
		Dictionary<string, string> generatedSources)
	{
		var goldenDir = Path.Combine(GoldenFilesDir, GoldenSubdirectory);
		var goldenFilePath = Path.Combine(goldenDir, goldenRelativePath);
		File.Exists(goldenFilePath).Should().BeTrue(
			$"golden file '{goldenFilePath}' should exist");

		var goldenSource = File.ReadAllText(goldenFilePath);
		var mappedGenerated = MapGeneratedToGoldenPaths(generatedSources);

		// Normalize the path separator for lookup
		var normalizedLookup = goldenRelativePath.Replace('/', Path.DirectorySeparatorChar);
		mappedGenerated.Should().ContainKey(normalizedLookup,
			$"the generator should produce output for '{goldenRelativePath}'");

		var generatedSource = mappedGenerated[normalizedLookup];

		var normalizedGolden = Normalize(goldenSource);
		var normalizedGenerated = Normalize(generatedSource);

		normalizedGenerated.Should().Be(normalizedGolden,
			$"generated source for '{goldenRelativePath}' should match the golden file");
	}

	// ------------------------------------------------------------------
	// Convenience: file count assertions
	// ------------------------------------------------------------------

	/// <summary>
	/// Asserts that the generator produced at least the expected number of
	/// source files (i.e., at least as many as golden files exist).
	/// </summary>
	protected void AssertMinimumFileCount(
		Dictionary<string, string> generatedSources,
		Dictionary<string, string> goldenFiles)
	{
		var mappedGenerated = MapGeneratedToGoldenPaths(generatedSources);
		mappedGenerated.Count.Should().BeGreaterThanOrEqualTo(
			goldenFiles.Count,
			$"the generator should produce at least {goldenFiles.Count} source file(s) " +
			$"to match golden files, but produced {mappedGenerated.Count}");
	}

	/// <summary>
	/// Asserts that every golden file has a matching generated source
	/// (without comparing content). Useful as a quick structural check.
	/// </summary>
	protected void AssertAllGoldenFilesHaveGeneratedCounterparts(
		Dictionary<string, string> generatedSources,
		Dictionary<string, string> goldenFiles)
	{
		var mappedGenerated = MapGeneratedToGoldenPaths(generatedSources);
		var missing = goldenFiles.Keys
			.Where(k => !mappedGenerated.ContainsKey(k))
			.ToList();

		missing.Should().BeEmpty(
			$"all {goldenFiles.Count} golden file(s) should have generated counterparts, " +
			$"but {missing.Count} are missing: {string.Join(", ", missing)}");
	}

	// ------------------------------------------------------------------
	// Diff helper
	// ------------------------------------------------------------------

	/// <summary>
	/// Creates a human-readable diff summary between golden and generated
	/// content, showing the first few differing lines for quick diagnosis.
	/// </summary>
	private static string CreateDiffSummary(
		string filePath,
		string expected,
		string actual)
	{
		var expectedLines = expected.Split('\n');
		var actualLines = actual.Split('\n');
		var maxLines = Math.Max(expectedLines.Length, actualLines.Length);
		var diffLines = new List<string>();
		const int maxDiffLines = 20;

		for (var i = 0; i < maxLines && diffLines.Count < maxDiffLines; i++)
		{
			var expectedLine = i < expectedLines.Length ? expectedLines[i] : "<EOF>";
			var actualLine = i < actualLines.Length ? actualLines[i] : "<EOF>";

			if (!string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
			{
				diffLines.Add($"  Line {i + 1}:");
				diffLines.Add($"    expected: {Truncate(expectedLine, 120)}");
				diffLines.Add($"    actual:   {Truncate(actualLine, 120)}");
			}
		}

		var summary = $"DIFF: '{filePath}' " +
			$"(golden: {expectedLines.Length} lines, generated: {actualLines.Length} lines)";

		if (diffLines.Count > 0)
		{
			summary += "\n" + string.Join("\n", diffLines);
		}

		if (maxLines > maxDiffLines)
		{
			summary += $"\n  ... (showing first {maxDiffLines} differences)";
		}

		return summary;
	}

	/// <summary>
	/// Truncates a string to the given maximum length, appending "…" if
	/// truncated.
	/// </summary>
	private static string Truncate(string value, int maxLength)
	{
		if (value.Length <= maxLength)
		{
			return value;
		}

		return value.Substring(0, maxLength - 1) + "…";
	}

	// ------------------------------------------------------------------
	// Path utility
	// ------------------------------------------------------------------

	/// <summary>
	/// Computes a relative path from <paramref name="basePath"/> to
	/// <paramref name="fullPath"/>. Uses <see cref="Path.GetRelativePath"/>
	/// on .NET 6+ or manual prefix stripping as fallback.
	/// </summary>
	private static string GetRelativePath(string basePath, string fullPath)
	{
		return Path.GetRelativePath(basePath, fullPath);
	}

	// ------------------------------------------------------------------
	// Minimal compilation
	// ------------------------------------------------------------------

	/// <summary>
	/// Creates a minimal <see cref="CSharpCompilation"/> with only the
	/// core reference assemblies. Sufficient for the generator to run
	/// without errors (generated code may have missing type references
	/// for Kiota runtime types, which is expected).
	/// </summary>
	private static CSharpCompilation CreateMinimalCompilation()
	{
		var references = new[]
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		};

		return CSharpCompilation.Create(
			assemblyName: "ParityTests",
			syntaxTrees: Array.Empty<SyntaxTree>(),
			references: references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	// ==================================================================
	// Result type
	// ==================================================================

	/// <summary>
	/// Holds the results of running the source generator, including
	/// all generated source files and any diagnostics.
	/// </summary>
	protected sealed class GeneratorRunResult
	{
		/// <summary>
		/// Generated sources keyed by hint name.
		/// </summary>
		public Dictionary<string, string> GeneratedSources { get; }

		/// <summary>
		/// All diagnostics reported during generation.
		/// </summary>
		public ImmutableArray<Diagnostic> Diagnostics { get; }

		public GeneratorRunResult(
			Dictionary<string, string> generatedSources,
			ImmutableArray<Diagnostic> diagnostics)
		{
			GeneratedSources = generatedSources;
			Diagnostics = diagnostics;
		}
	}

	// ==================================================================
	// Test doubles (shared with GeneratorIntegrationTests)
	// ==================================================================

	/// <summary>
	/// In-memory <see cref="AdditionalText"/> implementation for testing.
	/// </summary>
	private sealed class InMemoryAdditionalText : AdditionalText
	{
		private readonly SourceText _text;

		public InMemoryAdditionalText(string path, string content)
		{
			Path = path;
			_text = SourceText.From(content);
		}

		public override string Path { get; }

		public override SourceText? GetText(CancellationToken cancellationToken = default)
			=> _text;
	}

	/// <summary>
	/// Test <see cref="AnalyzerConfigOptionsProvider"/> that returns
	/// configured global options and per-file metadata.
	/// </summary>
	private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
	{
		private readonly DictionaryAnalyzerConfigOptions _globalOptions;
		private readonly DictionaryAnalyzerConfigOptions _fileOptions;

		public TestAnalyzerConfigOptionsProvider(
			ImmutableDictionary<string, string> globalOptions,
			ImmutableDictionary<string, string> fileOptions)
		{
			_globalOptions = new DictionaryAnalyzerConfigOptions(globalOptions);
			_fileOptions = new DictionaryAnalyzerConfigOptions(fileOptions);
		}

		public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
			=> DictionaryAnalyzerConfigOptions.Empty;

		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
			=> _fileOptions;
	}

	/// <summary>
	/// <see cref="AnalyzerConfigOptions"/> backed by an
	/// <see cref="ImmutableDictionary{TKey, TValue}"/>.
	/// </summary>
	private sealed class DictionaryAnalyzerConfigOptions : AnalyzerConfigOptions
	{
		public static readonly DictionaryAnalyzerConfigOptions Empty =
			new(ImmutableDictionary<string, string>.Empty);

		private readonly ImmutableDictionary<string, string> _options;

		public DictionaryAnalyzerConfigOptions(ImmutableDictionary<string, string> options)
		{
			_options = options;
		}

		public override bool TryGetValue(string key, out string value)
			=> _options.TryGetValue(key, out value!);
	}
}
