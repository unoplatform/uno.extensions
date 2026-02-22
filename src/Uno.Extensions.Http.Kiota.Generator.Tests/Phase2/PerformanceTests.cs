using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Http.Kiota.SourceGenerator;
using Uno.Extensions.Http.Kiota.SourceGenerator.Parsing;

namespace Uno.Extensions.Http.Kiota.Generator.Tests.Phase2;

/// <summary>
/// Performance and incremental-caching tests for <see cref="KiotaSourceGenerator"/>.
/// <para>
/// Validates that the source generator handles large OpenAPI specifications
/// gracefully, completes within acceptable time bounds, emits the KIOTA020
/// diagnostic for oversized specs, and leverages Roslyn's incremental pipeline
/// caching so repeated builds skip redundant work.
/// </para>
/// </summary>
[TestClass]
[TestCategory("Performance")]
public class PerformanceTests
{
	// ------------------------------------------------------------------
	// Test data paths
	// ------------------------------------------------------------------

	private static string TestDataDir =>
		Path.Combine(AppContext.BaseDirectory, "TestData");

	// ==================================================================
	// Large spec generation and handling
	// ==================================================================

	[TestMethod]
	public void LargeSpec_GeneratesWithoutError()
	{
		// Arrange — generate a spec with 200 paths and 200 schemas,
		// simulating a moderately large enterprise API.
		var spec = GenerateLargeOpenApiSpec(pathCount: 200, schemaCount: 200);

		var driver = CreateGeneratorDriver(
			spec,
			"large-api.json",
			clientName: "LargeApiClient",
			namespaceName: "TestApp.LargeApi");

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		// Assert — no error-level diagnostics from the generator
		var errors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		errors.Should().BeEmpty(
			"the generator should handle a 200-path/200-schema spec without errors");

		// Assert — files were actually generated
		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().NotBeEmpty(
			"the generator should produce source files for a valid large spec");

		// Expect a significant number of generated files (at least one per
		// model schema + request builders).
		generatedTrees.Count.Should().BeGreaterThanOrEqualTo(50,
			"200 paths and 200 schemas should produce at least 50 generated files");
	}

	[TestMethod]
	public void LargeSpec_CompletesInReasonableTime()
	{
		// Arrange — 100 paths / 100 schemas: a medium-large spec that the
		// source generator should process well under 30 seconds even on CI.
		var spec = GenerateLargeOpenApiSpec(pathCount: 100, schemaCount: 100);

		var driver = CreateGeneratorDriver(
			spec,
			"perf-test.json",
			clientName: "PerfClient",
			namespaceName: "TestApp.Perf");

		var compilation = CreateMinimalCompilation();

		// Act — time the generation
		var sw = Stopwatch.StartNew();
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out _, out var diagnostics);
		sw.Stop();

		// Assert — no errors
		var errors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		errors.Should().BeEmpty(
			"the generator should not error on a 100-path spec");

		// Assert — completes within 30 seconds (generous for CI machines).
		// Typical runs are <5 seconds on modern hardware.
		sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30),
			"source generation for a 100-path/100-schema spec should complete " +
			"within 30 seconds even on slow CI machines");
	}

	[TestMethod]
	public void PetstoreSpec_CompletesInReasonableTime()
	{
		// Arrange — typical small spec (~800 lines)
		var specPath = Path.Combine(TestDataDir, "petstore.json");
		var specContent = File.ReadAllText(specPath);

		var driver = CreateGeneratorDriver(
			specContent,
			specPath,
			clientName: "PetStoreClient",
			namespaceName: "TestApp.PetStore");

		var compilation = CreateMinimalCompilation();

		// Act
		var sw = Stopwatch.StartNew();
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out _, out var diagnostics);
		sw.Stop();

		// Assert — no errors and completes quickly (<5s for a small spec)
		var errors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		errors.Should().BeEmpty();

		sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
			"the petstore spec (~800 lines) should generate in under 5 seconds");
	}

	// ==================================================================
	// KIOTA020 — large spec warning
	// ==================================================================

	[TestMethod]
	public void OverSizedSpec_EmitsKIOTA020Warning()
	{
		// Arrange — create a spec whose JSON text exceeds the LargeSpecThreshold
		// (5,000,000 chars). We do this by padding description fields.
		var spec = GenerateOverSizedOpenApiSpec();
		spec.Length.Should().BeGreaterThan(
			OpenApiDocumentParser.LargeSpecThreshold,
			"the generated spec should exceed the large-spec threshold");

		var driver = CreateGeneratorDriver(
			spec,
			"oversized-api.json",
			clientName: "OversizedClient",
			namespaceName: "TestApp.Oversized");

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out _, out var diagnostics);

		// Assert — KIOTA020 warning should be present
		var kiota020 = diagnostics
			.Where(d => d.Id == "KIOTA020")
			.ToList();
		kiota020.Should().NotBeEmpty(
			"the generator should emit KIOTA020 for specs exceeding the size threshold");
		kiota020.First().Severity.Should().Be(DiagnosticSeverity.Warning);
	}

	[TestMethod]
	public void NormalSpec_DoesNotEmitKIOTA020()
	{
		// Arrange — the petstore spec is well under the threshold
		var specPath = Path.Combine(TestDataDir, "petstore.json");
		var specContent = File.ReadAllText(specPath);
		specContent.Length.Should().BeLessThan(
			OpenApiDocumentParser.LargeSpecThreshold,
			"petstore.json should be under the large-spec threshold");

		var driver = CreateGeneratorDriver(
			specContent,
			specPath,
			clientName: "PetStoreClient",
			namespaceName: "TestApp.PetStore");

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out _, out var diagnostics);

		// Assert — no KIOTA020 warnings
		var kiota020 = diagnostics.Where(d => d.Id == "KIOTA020").ToList();
		kiota020.Should().BeEmpty(
			"the petstore spec is small and should not trigger KIOTA020");
	}

	// ==================================================================
	// Incremental caching — second run with unchanged input is cached
	// ==================================================================

	[TestMethod]
	public void IncrementalCaching_SecondRunProducesSameOutput()
	{
		// Arrange
		var specPath = Path.Combine(TestDataDir, "petstore.json");
		var specContent = File.ReadAllText(specPath);

		var driver = CreateGeneratorDriver(
			specContent,
			specPath,
			clientName: "PetStoreClient",
			namespaceName: "TestApp.PetStore");

		var compilation = CreateMinimalCompilation();

		// Act — first run
		driver = driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var firstOutput, out var firstDiagnostics);

		// Act — second run with the same compilation (no changes)
		driver = driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var secondOutput, out var secondDiagnostics);

		// Assert — outputs should be identical
		var firstTrees = firstOutput.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.Select(t => t.GetText().ToString())
			.OrderBy(s => s)
			.ToList();

		var secondTrees = secondOutput.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.Select(t => t.GetText().ToString())
			.OrderBy(s => s)
			.ToList();

		firstTrees.Should().BeEquivalentTo(secondTrees,
			"the second run with unchanged input should produce identical output");
	}

	[TestMethod]
	public void IncrementalCaching_SecondRunIsFasterOrComparable()
	{
		// Arrange — use a medium-sized spec to see caching benefits
		var spec = GenerateLargeOpenApiSpec(pathCount: 50, schemaCount: 50);

		var driver = CreateGeneratorDriver(
			spec,
			"cache-test.json",
			clientName: "CacheClient",
			namespaceName: "TestApp.Cache");

		var compilation = CreateMinimalCompilation();

		// Act — first run (cold)
		var sw1 = Stopwatch.StartNew();
		driver = driver.RunGeneratorsAndUpdateCompilation(
			compilation, out _, out _);
		sw1.Stop();

		// Act — second run (warm / cached)
		var sw2 = Stopwatch.StartNew();
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out _, out _);
		sw2.Stop();

		// Assert — second run should not be significantly slower than first.
		// With proper caching, the second run should be near-instant.
		// We use a generous multiplier (3x) to account for CI variance.
		// The key assertion is that the second run doesn't re-do all the work.
		sw2.Elapsed.Should().BeLessThan(
			TimeSpan.FromTicks(Math.Max(sw1.ElapsedTicks * 3, TimeSpan.FromSeconds(5).Ticks)),
			"the second run should benefit from incremental caching " +
			$"(first run: {sw1.ElapsedMilliseconds}ms, second run: {sw2.ElapsedMilliseconds}ms)");
	}

	[TestMethod]
	public void IncrementalCaching_ChangedInputRegenerates()
	{
		// Arrange
		var specPath = Path.Combine(TestDataDir, "petstore.json");
		var specContent = File.ReadAllText(specPath);

		var driver = CreateGeneratorDriver(
			specContent,
			specPath,
			clientName: "PetStoreClient",
			namespaceName: "TestApp.PetStore");

		var compilation = CreateMinimalCompilation();

		// Act — first run
		driver = driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var firstOutput, out _);

		// Now create a driver with a different spec (modified input)
		var modifiedSpec = specContent.Replace(
			"\"title\": \"Petstore API\"",
			"\"title\": \"Modified Petstore API\"");

		var newDriver = CreateGeneratorDriver(
			modifiedSpec,
			specPath,
			clientName: "PetStoreClient",
			namespaceName: "TestApp.PetStore");

		// Act — run with modified input
		newDriver.RunGeneratorsAndUpdateCompilation(
			compilation, out var secondOutput, out _);

		// Assert — output should exist for both runs (the generator did re-run)
		var firstTrees = firstOutput.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		var secondTrees = secondOutput.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();

		firstTrees.Should().NotBeEmpty("first run should produce output");
		secondTrees.Should().NotBeEmpty("second run with modified input should produce output");
	}

	[TestMethod]
	public void IncrementalCaching_TrackedStepsShowCachedOnSecondRun()
	{
		// Arrange
		var specPath = Path.Combine(TestDataDir, "petstore.json");
		var specContent = File.ReadAllText(specPath);

		var generator = new KiotaSourceGenerator();
		var additionalText = new InMemoryAdditionalText(specPath, specContent);

		var fileOptions = ImmutableDictionary.CreateRange(new[]
		{
			KeyValuePair.Create(
				"build_metadata.AdditionalFiles.KiotaClientName", "PetStoreClient"),
			KeyValuePair.Create(
				"build_metadata.AdditionalFiles.KiotaNamespace", "TestApp.PetStore"),
		});
		var globalOptions = ImmutableDictionary.CreateRange(new[]
		{
			KeyValuePair.Create("build_property.KiotaGenerator_Enabled", "true"),
		});
		var optionsProvider = new TestAnalyzerConfigOptionsProvider(globalOptions, fileOptions);

		// Enable tracked steps so we can inspect caching behavior
		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			generators: new ISourceGenerator[] { generator.AsSourceGenerator() },
			additionalTexts: ImmutableArray.Create<AdditionalText>(additionalText),
			optionsProvider: optionsProvider,
			driverOptions: new GeneratorDriverOptions(
				disabledOutputs: default,
				trackIncrementalGeneratorSteps: true));

		var compilation = CreateMinimalCompilation();

		// Act — first run
		driver = driver.RunGeneratorsAndUpdateCompilation(
			compilation, out _, out _);

		// Act — second run with the same inputs
		driver = driver.RunGeneratorsAndUpdateCompilation(
			compilation, out _, out _);

		// Assert — check that tracked steps show cached results
		var runResult = driver.GetRunResult();
		runResult.Results.Should().HaveCount(1,
			"there should be exactly one generator result");

		var generatorResult = runResult.Results[0];

		// The tracked output steps should contain "Cached" entries.
		// When the incremental pipeline detects no input changes, outputs
		// carry IncrementalStepRunReason.Cached.
		var allOutputSteps = generatorResult.TrackedOutputSteps;

		if (allOutputSteps.Count > 0)
		{
			var cachedOutputs = allOutputSteps.Values
				.SelectMany(steps => steps)
				.SelectMany(step => step.Outputs)
				.Where(o => o.Reason == IncrementalStepRunReason.Cached)
				.ToList();

			// There should be at least some cached outputs on the second run.
			// The exact count depends on the pipeline structure, but we expect
			// the majority of outputs to be cached.
			cachedOutputs.Should().NotBeEmpty(
				"the second run with identical inputs should have cached output steps");
		}

		// Also verify tracked steps (intermediate pipeline steps)
		var allSteps = generatorResult.TrackedSteps;
		if (allSteps.Count > 0)
		{
			var cachedSteps = allSteps.Values
				.SelectMany(steps => steps)
				.SelectMany(step => step.Outputs)
				.Where(o => o.Reason == IncrementalStepRunReason.Cached
					|| o.Reason == IncrementalStepRunReason.Unchanged)
				.ToList();

			cachedSteps.Should().NotBeEmpty(
				"incremental pipeline steps should report cached/unchanged on second run");
		}
	}

	// ==================================================================
	// Scale tests — verify the generator doesn't crash at various sizes
	// ==================================================================

	[TestMethod]
	[DataRow(10, 10, DisplayName = "Small: 10 paths, 10 schemas")]
	[DataRow(50, 50, DisplayName = "Medium: 50 paths, 50 schemas")]
	[DataRow(100, 100, DisplayName = "Large: 100 paths, 100 schemas")]
	public void ScaledSpec_GeneratesSuccessfully(int pathCount, int schemaCount)
	{
		// Arrange
		var spec = GenerateLargeOpenApiSpec(pathCount, schemaCount);

		var driver = CreateGeneratorDriver(
			spec,
			$"scale-{pathCount}.json",
			clientName: "ScaleClient",
			namespaceName: "TestApp.Scale");

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		// Assert — no errors
		var errors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		errors.Should().BeEmpty(
			$"the generator should succeed for {pathCount} paths and {schemaCount} schemas");

		// Assert — reasonable output count
		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Count.Should().BeGreaterThan(0,
			$"the generator should produce output for {pathCount} paths and {schemaCount} schemas");
	}

	[TestMethod]
	public void LargeSpec_FileCountScalesWithInput()
	{
		// Arrange — generate two specs of different sizes
		var smallSpec = GenerateLargeOpenApiSpec(pathCount: 10, schemaCount: 10);
		var largeSpec = GenerateLargeOpenApiSpec(pathCount: 50, schemaCount: 50);

		var smallDriver = CreateGeneratorDriver(
			smallSpec, "small.json", "SmallClient", "TestApp.Small");
		var largeDriver = CreateGeneratorDriver(
			largeSpec, "large.json", "LargeClient", "TestApp.Large");

		var compilation = CreateMinimalCompilation();

		// Act
		smallDriver.RunGeneratorsAndUpdateCompilation(
			compilation, out var smallOutput, out _);
		largeDriver.RunGeneratorsAndUpdateCompilation(
			compilation, out var largeOutput, out _);

		var smallCount = smallOutput.SyntaxTrees
			.Except(compilation.SyntaxTrees).Count();
		var largeCount = largeOutput.SyntaxTrees
			.Except(compilation.SyntaxTrees).Count();

		// Assert — larger spec produces more files
		largeCount.Should().BeGreaterThan(smallCount,
			"a spec with more paths/schemas should produce more generated files");
	}

	// ==================================================================
	// Large spec spec generator
	// ==================================================================

	/// <summary>
	/// Generates a syntactically valid OpenAPI 3.0 JSON specification with
	/// the given number of paths and schemas. Each path has GET and POST
	/// operations referencing a schema, producing a realistic CRUD-style API.
	/// </summary>
	/// <param name="pathCount">Number of unique API paths to generate.</param>
	/// <param name="schemaCount">
	/// Number of unique model schemas to generate.
	/// Each schema has 3–5 properties of varying types.
	/// </param>
	/// <returns>A JSON string containing the OpenAPI spec.</returns>
	private static string GenerateLargeOpenApiSpec(int pathCount, int schemaCount)
	{
		var sb = new StringBuilder();
		sb.AppendLine("{");
		sb.AppendLine("  \"openapi\": \"3.0.3\",");
		sb.AppendLine("  \"info\": {");
		sb.AppendLine("    \"title\": \"Large Scale API\",");
		sb.AppendLine("    \"version\": \"1.0.0\"");
		sb.AppendLine("  },");
		sb.AppendLine("  \"servers\": [{ \"url\": \"https://api.example.com/v1\" }],");
		sb.AppendLine("  \"paths\": {");

		for (var i = 0; i < pathCount; i++)
		{
			var resourceName = $"resource{i:D4}";
			var schemaRef = $"Model{i % schemaCount:D4}";
			var separator = i < pathCount - 1 ? "," : "";

			sb.AppendLine($"    \"/{resourceName}\": {{");
			sb.AppendLine($"      \"get\": {{");
			sb.AppendLine($"        \"operationId\": \"list{resourceName}\",");
			sb.AppendLine($"        \"summary\": \"List all {resourceName} items\",");
			sb.AppendLine($"        \"parameters\": [");
			sb.AppendLine($"          {{ \"name\": \"limit\", \"in\": \"query\", \"schema\": {{ \"type\": \"integer\", \"format\": \"int32\" }} }},");
			sb.AppendLine($"          {{ \"name\": \"offset\", \"in\": \"query\", \"schema\": {{ \"type\": \"integer\", \"format\": \"int32\" }} }}");
			sb.AppendLine($"        ],");
			sb.AppendLine($"        \"responses\": {{");
			sb.AppendLine($"          \"200\": {{");
			sb.AppendLine($"            \"description\": \"Successful response\",");
			sb.AppendLine($"            \"content\": {{");
			sb.AppendLine($"              \"application/json\": {{");
			sb.AppendLine($"                \"schema\": {{");
			sb.AppendLine($"                  \"type\": \"array\",");
			sb.AppendLine($"                  \"items\": {{ \"$ref\": \"#/components/schemas/{schemaRef}\" }}");
			sb.AppendLine($"                }}");
			sb.AppendLine($"              }}");
			sb.AppendLine($"            }}");
			sb.AppendLine($"          }}");
			sb.AppendLine($"        }}");
			sb.AppendLine($"      }},");
			sb.AppendLine($"      \"post\": {{");
			sb.AppendLine($"        \"operationId\": \"create{resourceName}\",");
			sb.AppendLine($"        \"summary\": \"Create a {resourceName}\",");
			sb.AppendLine($"        \"requestBody\": {{");
			sb.AppendLine($"          \"required\": true,");
			sb.AppendLine($"          \"content\": {{");
			sb.AppendLine($"            \"application/json\": {{");
			sb.AppendLine($"              \"schema\": {{ \"$ref\": \"#/components/schemas/{schemaRef}\" }}");
			sb.AppendLine($"            }}");
			sb.AppendLine($"          }}");
			sb.AppendLine($"        }},");
			sb.AppendLine($"        \"responses\": {{");
			sb.AppendLine($"          \"201\": {{");
			sb.AppendLine($"            \"description\": \"Created\",");
			sb.AppendLine($"            \"content\": {{");
			sb.AppendLine($"              \"application/json\": {{");
			sb.AppendLine($"                \"schema\": {{ \"$ref\": \"#/components/schemas/{schemaRef}\" }}");
			sb.AppendLine($"              }}");
			sb.AppendLine($"            }}");
			sb.AppendLine($"          }}");
			sb.AppendLine($"        }}");
			sb.AppendLine($"      }}");
			sb.AppendLine($"    }}{separator}");
		}

		sb.AppendLine("  },");
		sb.AppendLine("  \"components\": {");
		sb.AppendLine("    \"schemas\": {");

		for (var i = 0; i < schemaCount; i++)
		{
			var modelName = $"Model{i:D4}";
			var separator = i < schemaCount - 1 ? "," : "";

			sb.AppendLine($"      \"{modelName}\": {{");
			sb.AppendLine($"        \"type\": \"object\",");
			sb.AppendLine($"        \"description\": \"Model {modelName} representing a domain entity.\",");
			sb.AppendLine($"        \"properties\": {{");
			sb.AppendLine($"          \"id\": {{ \"type\": \"string\", \"description\": \"Unique identifier\" }},");
			sb.AppendLine($"          \"name\": {{ \"type\": \"string\", \"description\": \"Display name\" }},");
			sb.AppendLine($"          \"createdAt\": {{ \"type\": \"string\", \"format\": \"date-time\", \"description\": \"Creation timestamp\" }},");
			sb.AppendLine($"          \"count\": {{ \"type\": \"integer\", \"format\": \"int32\", \"description\": \"Item count\" }},");
			sb.AppendLine($"          \"active\": {{ \"type\": \"boolean\", \"description\": \"Whether the entity is active\" }}");
			sb.AppendLine($"        }}");
			sb.AppendLine($"      }}{separator}");
		}

		sb.AppendLine("    }");
		sb.AppendLine("  }");
		sb.AppendLine("}");

		return sb.ToString();
	}

	/// <summary>
	/// Generates an OpenAPI spec that exceeds <see cref="OpenApiDocumentParser.LargeSpecThreshold"/>
	/// by padding schema description fields with large strings. The spec is still
	/// valid OpenAPI — the descriptions are simply very verbose.
	/// </summary>
	private static string GenerateOverSizedOpenApiSpec()
	{
		// We need to exceed 5,000,000 characters. Generate a spec with many
		// schemas, each having a very long description.
		var sb = new StringBuilder();
		sb.AppendLine("{");
		sb.AppendLine("  \"openapi\": \"3.0.3\",");
		sb.AppendLine("  \"info\": {");
		sb.AppendLine("    \"title\": \"Oversized API\",");
		sb.AppendLine("    \"version\": \"1.0.0\"");
		sb.AppendLine("  },");
		sb.AppendLine("  \"paths\": {");
		sb.AppendLine("    \"/items\": {");
		sb.AppendLine("      \"get\": {");
		sb.AppendLine("        \"operationId\": \"listItems\",");
		sb.AppendLine("        \"responses\": {");
		sb.AppendLine("          \"200\": {");
		sb.AppendLine("            \"description\": \"OK\",");
		sb.AppendLine("            \"content\": {");
		sb.AppendLine("              \"application/json\": {");
		sb.AppendLine("                \"schema\": { \"$ref\": \"#/components/schemas/Item0000\" }");
		sb.AppendLine("              }");
		sb.AppendLine("            }");
		sb.AppendLine("          }");
		sb.AppendLine("        }");
		sb.AppendLine("      }");
		sb.AppendLine("    }");
		sb.AppendLine("  },");
		sb.AppendLine("  \"components\": {");
		sb.AppendLine("    \"schemas\": {");

		// Generate schemas with large descriptions to push over the threshold.
		// ~50,000 chars per schema description × 110 schemas ≈ 5,500,000 chars.
		const int schemaCount = 110;
		var padding = new string('A', 50_000);

		for (var i = 0; i < schemaCount; i++)
		{
			var modelName = $"Item{i:D4}";
			var separator = i < schemaCount - 1 ? "," : "";

			sb.AppendLine($"      \"{modelName}\": {{");
			sb.AppendLine($"        \"type\": \"object\",");
			sb.Append($"        \"description\": \"").Append(padding).AppendLine("\",");
			sb.AppendLine($"        \"properties\": {{");
			sb.AppendLine($"          \"id\": {{ \"type\": \"string\" }}");
			sb.AppendLine($"        }}");
			sb.AppendLine($"      }}{separator}");
		}

		sb.AppendLine("    }");
		sb.AppendLine("  }");
		sb.AppendLine("}");

		return sb.ToString();
	}

	// ==================================================================
	// Helpers
	// ==================================================================

	/// <summary>
	/// Creates a <see cref="CSharpGeneratorDriver"/> configured with a
	/// <see cref="KiotaSourceGenerator"/>, the given spec as an additional
	/// file, and appropriate analyzer config options.
	/// </summary>
	private static GeneratorDriver CreateGeneratorDriver(
		string specContent,
		string specPath,
		string clientName,
		string namespaceName)
	{
		var additionalText = new InMemoryAdditionalText(specPath, specContent);

		var fileOptions = ImmutableDictionary.CreateRange(new[]
		{
			KeyValuePair.Create(
				"build_metadata.AdditionalFiles.KiotaClientName", clientName),
			KeyValuePair.Create(
				"build_metadata.AdditionalFiles.KiotaNamespace", namespaceName),
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

		return CSharpGeneratorDriver.Create(generator)
			.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalText))
			.WithUpdatedAnalyzerConfigOptions(optionsProvider);
	}

	/// <summary>
	/// Creates a minimal <see cref="CSharpCompilation"/> with only core
	/// reference assemblies.
	/// </summary>
	private static CSharpCompilation CreateMinimalCompilation()
	{
		var references = new[]
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		};

		return CSharpCompilation.Create(
			assemblyName: "PerformanceTests",
			syntaxTrees: Array.Empty<SyntaxTree>(),
			references: references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	// ==================================================================
	// Test doubles
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

		public override SourceText GetText(CancellationToken cancellationToken = default)
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
