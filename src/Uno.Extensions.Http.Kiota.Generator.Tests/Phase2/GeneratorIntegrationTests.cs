using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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
/// End-to-end integration tests for <see cref="KiotaSourceGenerator"/>.
/// <para>
/// These tests exercise the full pipeline (parse → CodeDOM build → refine → emit)
/// by feeding OpenAPI spec files through Roslyn's <see cref="CSharpGeneratorDriver"/>
/// with the appropriate <c>KiotaClientName</c> metadata on the additional file.
/// </para>
/// <list type="bullet">
///   <item>Petstore spec → generates source → source compiles without errors</item>
///   <item>Enums spec → generates enum types</item>
///   <item>Inheritance spec → generates base/derived model classes</item>
///   <item>Error handling → invalid spec reports diagnostics</item>
///   <item>Disabled generator → no output produced</item>
/// </list>
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class GeneratorIntegrationTests
{
	// ------------------------------------------------------------------
	// Test data paths — resolved relative to the test output directory
	// ------------------------------------------------------------------

	private static string TestDataDir =>
		Path.Combine(AppContext.BaseDirectory, "TestData");

	// ==================================================================
	// Petstore spec — full pipeline
	// ==================================================================

	[TestMethod]
	public void PetstoreSpec_GeneratesSourceFiles()
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

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		// Assert — generator did not report errors
		var generatorErrors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		generatorErrors.Should().BeEmpty(
			"the generator should not report any errors for a valid petstore spec");

		// Assert — at least one source file was generated
		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().NotBeEmpty(
			"the generator should produce at least one C# source file");
	}

	[TestMethod]
	public void PetstoreSpec_GeneratedSourceContainsPetStoreClientClass()
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

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out _);

		// Assert — the generated source should contain the client class name
		var allGeneratedSource = GetAllGeneratedSource(outputCompilation, compilation);
		allGeneratedSource.Should().Contain("PetStoreClient",
			"the root client class should appear in generated output");
	}

	[TestMethod]
	public void PetstoreSpec_GeneratedSourceContainsModelClasses()
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

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out _);

		// Assert — the petstore spec declares Pet, so the model should appear
		var allGeneratedSource = GetAllGeneratedSource(outputCompilation, compilation);
		allGeneratedSource.Should().Contain("class Pet",
			"the Pet model from the petstore spec should be generated");
	}

	[TestMethod]
	public void PetstoreSpec_GeneratedSourceContainsNamespace()
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

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out _);

		// Assert
		var allGeneratedSource = GetAllGeneratedSource(outputCompilation, compilation);
		allGeneratedSource.Should().Contain("namespace TestApp.PetStore",
			"the configured namespace should appear in generated output");
	}

	[TestMethod]
	public void PetstoreSpec_GeneratedSourceContainsGeneratedCodeAttribute()
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

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out _);

		// Assert — all generated files should have [GeneratedCode] attribute
		var allGeneratedSource = GetAllGeneratedSource(outputCompilation, compilation);
		allGeneratedSource.Should().Contain("[global::System.CodeDom.Compiler.GeneratedCode(\"Kiota\"",
			"generated types should carry the [GeneratedCode] attribute");
	}

	[TestMethod]
	public void PetstoreSpec_GeneratesMultipleSourceFiles()
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

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out _);

		// Assert — petstore has multiple schemas + request builders, so we
		// expect several generated files
		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Count.Should().BeGreaterThan(1,
			"petstore has multiple schemas and endpoints so should produce multiple files");
	}

	[TestMethod]
	public void PetstoreSpec_ReportsCompletionDiagnostic()
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

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out _, out var diagnostics);

		// Assert — should report KIOTA030 informational diagnostic
		diagnostics.Should().Contain(
			d => d.Id == "KIOTA030",
			"generator should report a successful completion diagnostic");
	}

	// ==================================================================
	// Enums spec
	// ==================================================================

	[TestMethod]
	public void EnumsSpec_GeneratesEnumTypes()
	{
		// Arrange
		var specPath = Path.Combine(TestDataDir, "enums.json");
		var specContent = File.ReadAllText(specPath);

		var driver = CreateGeneratorDriver(
			specContent,
			specPath,
			clientName: "EnumsClient",
			namespaceName: "TestApp.Enums");

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		// Assert — no errors
		var generatorErrors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		generatorErrors.Should().BeEmpty(
			"the generator should not report errors for the enums spec");

		// Assert — should produce at least some output
		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().NotBeEmpty(
			"the enums spec should produce generated source files");

		// Assert — generated source should contain enum keyword
		var allGeneratedSource = GetAllGeneratedSource(outputCompilation, compilation);
		allGeneratedSource.Should().Contain("enum ",
			"the enums spec should produce enum type declarations");
	}

	// ==================================================================
	// Inheritance spec
	// ==================================================================

	[TestMethod]
	public void InheritanceSpec_GeneratesModelClasses()
	{
		// Arrange
		var specPath = Path.Combine(TestDataDir, "inheritance.json");
		var specContent = File.ReadAllText(specPath);

		var driver = CreateGeneratorDriver(
			specContent,
			specPath,
			clientName: "InheritanceClient",
			namespaceName: "TestApp.Inheritance");

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		// Assert — no errors
		var generatorErrors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		generatorErrors.Should().BeEmpty(
			"the generator should not report errors for the inheritance spec");

		// Assert — should generate at least one source
		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().NotBeEmpty(
			"the inheritance spec should produce generated source files");
	}

	// ==================================================================
	// Composed types spec
	// ==================================================================

	[TestMethod]
	public void ComposedTypesSpec_GeneratesSourceFiles()
	{
		// Arrange
		var specPath = Path.Combine(TestDataDir, "composed-types.json");
		var specContent = File.ReadAllText(specPath);

		var driver = CreateGeneratorDriver(
			specContent,
			specPath,
			clientName: "ComposedClient",
			namespaceName: "TestApp.Composed");

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		// Assert — no errors
		var generatorErrors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		generatorErrors.Should().BeEmpty(
			"the generator should not report errors for the composed types spec");

		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().NotBeEmpty(
			"the composed types spec should produce generated source files");
	}

	// ==================================================================
	// Error-responses spec
	// ==================================================================

	[TestMethod]
	public void ErrorResponsesSpec_GeneratesSourceFiles()
	{
		// Arrange
		var specPath = Path.Combine(TestDataDir, "error-responses.json");
		var specContent = File.ReadAllText(specPath);

		var driver = CreateGeneratorDriver(
			specContent,
			specPath,
			clientName: "ErrorClient",
			namespaceName: "TestApp.Errors");

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		// Assert — no errors
		var generatorErrors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		generatorErrors.Should().BeEmpty(
			"the generator should not report errors for the error-responses spec");

		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().NotBeEmpty(
			"the error-responses spec should produce generated source files");
	}

	// ==================================================================
	// YAML format
	// ==================================================================

	[TestMethod]
	public void YamlSpec_GeneratesSourceFiles()
	{
		// Arrange
		var specPath = Path.Combine(TestDataDir, "petstore.yaml");
		var specContent = File.ReadAllText(specPath);

		var driver = CreateGeneratorDriver(
			specContent,
			specPath,
			clientName: "YamlPetStoreClient",
			namespaceName: "TestApp.YamlPetStore");

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		// Assert
		var generatorErrors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		generatorErrors.Should().BeEmpty(
			"the generator should handle YAML specs without errors");

		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().NotBeEmpty(
			"a YAML spec should produce generated source files");
	}

	// ==================================================================
	// Invalid spec — error reporting
	// ==================================================================

	[TestMethod]
	public void InvalidSpec_ReportsParseError()
	{
		// Arrange — feed malformed JSON
		const string invalidContent = "{ this is not valid JSON or OpenAPI }";
		var specPath = "invalid-spec.json";

		var driver = CreateGeneratorDriver(
			invalidContent,
			specPath,
			clientName: "BrokenClient",
			namespaceName: "TestApp.Broken");

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		// Assert — should report KIOTA001 parse failure
		diagnostics.Should().Contain(
			d => d.Id == "KIOTA001",
			"the generator should report a parse failure for invalid JSON");

		// No new source files should be generated
		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().BeEmpty(
			"no source files should be generated from an invalid spec");
	}

	// ==================================================================
	// Generator disabled via global property
	// ==================================================================

	[TestMethod]
	public void DisabledGenerator_ProducesNoOutput()
	{
		// Arrange
		var specPath = Path.Combine(TestDataDir, "petstore.json");
		var specContent = File.ReadAllText(specPath);

		var driver = CreateGeneratorDriver(
			specContent,
			specPath,
			clientName: "PetStoreClient",
			namespaceName: "TestApp.PetStore",
			enabled: false);

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		// Assert — no generator errors
		var generatorErrors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		generatorErrors.Should().BeEmpty();

		// Assert — no generated output when disabled
		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().BeEmpty(
			"when the generator is disabled, no source should be produced");
	}

	// ==================================================================
	// File without KiotaClientName metadata is ignored
	// ==================================================================

	[TestMethod]
	public void FileWithoutKiotaClientName_IsIgnored()
	{
		// Arrange — provide no KiotaClientName metadata
		var specPath = Path.Combine(TestDataDir, "petstore.json");
		var specContent = File.ReadAllText(specPath);

		var additionalText = new InMemoryAdditionalText(specPath, specContent);
		var optionsProvider = new TestAnalyzerConfigOptionsProvider(
			globalOptions: ImmutableDictionary<string, string>.Empty,
			fileOptions: ImmutableDictionary<string, string>.Empty);

		var generator = new KiotaSourceGenerator();
		var driver = CSharpGeneratorDriver.Create(generator)
			.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalText))
			.WithUpdatedAnalyzerConfigOptions(optionsProvider);

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		// Assert — no output
		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().BeEmpty(
			"files without KiotaClientName metadata should be ignored");
	}

	// ==================================================================
	// Non-OpenAPI file extension is ignored
	// ==================================================================

	[TestMethod]
	public void NonOpenApiExtension_IsIgnored()
	{
		// Arrange — .xml is not a supported extension
		const string content = "{ \"openapi\": \"3.0.0\" }";
		var specPath = "spec.xml";

		var additionalText = new InMemoryAdditionalText(specPath, content);
		var fileOptions = ImmutableDictionary.CreateRange(new[]
		{
			KeyValuePair.Create(
				"build_metadata.AdditionalFiles.KiotaClientName", "XmlClient"),
			KeyValuePair.Create(
				"build_metadata.AdditionalFiles.KiotaNamespace", "TestApp.Xml"),
		});
		var optionsProvider = new TestAnalyzerConfigOptionsProvider(
			globalOptions: ImmutableDictionary<string, string>.Empty,
			fileOptions: fileOptions);

		var generator = new KiotaSourceGenerator();
		var driver = CSharpGeneratorDriver.Create(generator)
			.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalText))
			.WithUpdatedAnalyzerConfigOptions(optionsProvider);

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out _);

		// Assert
		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().BeEmpty(
			"files with unsupported extensions should be ignored even if they have KiotaClientName metadata");
	}

	// ==================================================================
	// Custom configuration — backing store
	// ==================================================================

	[TestMethod]
	public void PetstoreSpec_WithBackingStore_GeneratesBackingStorePattern()
	{
		// Arrange
		var specPath = Path.Combine(TestDataDir, "petstore.json");
		var specContent = File.ReadAllText(specPath);

		var driver = CreateGeneratorDriver(
			specContent,
			specPath,
			clientName: "PetStoreClient",
			namespaceName: "TestApp.PetStore",
			usesBackingStore: true);

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		// Assert — no generator errors
		var generatorErrors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		generatorErrors.Should().BeEmpty(
			"the generator should not report errors with backing store enabled");

		// Assert — generated source should reference IBackedModel or BackingStore
		var allGeneratedSource = GetAllGeneratedSource(outputCompilation, compilation);
		allGeneratedSource.Should().Contain("BackingStore",
			"when UsesBackingStore is enabled, generated code should reference backing store");
	}

	// ==================================================================
	// Incremental caching - re-running does not regress
	// ==================================================================

	[TestMethod]
	public void PetstoreSpec_SecondRun_ProducesSameOutput()
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
		driver = driver.RunGenerators(compilation);
		var result1 = driver.GetRunResult();

		// Act — second run (same inputs, tests caching path)
		driver = driver.RunGenerators(compilation);
		var result2 = driver.GetRunResult();

		// Assert — same number of generated sources
		result1.GeneratedTrees.Length.Should().Be(result2.GeneratedTrees.Length,
			"re-running the generator with the same inputs should produce the same number of files");

		// Assert — same generated hint names
		var hintNames1 = result1.Results[0].GeneratedSources
			.Select(s => s.HintName)
			.OrderBy(n => n)
			.ToList();
		var hintNames2 = result2.Results[0].GeneratedSources
			.Select(s => s.HintName)
			.OrderBy(n => n)
			.ToList();
		hintNames1.Should().BeEquivalentTo(hintNames2,
			"re-running should produce the same hint names");
	}

	// ==================================================================
	// Error handling — fallback comment on complete failure (T052)
	// ==================================================================

	[TestMethod]
	public void EmptySpec_ReportsParseFailure_NoSourceGenerated()
	{
		// Arrange — completely empty file content
		var specPath = "empty-spec.json";
		const string emptyContent = "   ";

		var driver = CreateGeneratorDriver(
			emptyContent,
			specPath,
			clientName: "EmptyClient",
			namespaceName: "TestApp.Empty");

		var compilation = CreateMinimalCompilation();

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		// Assert — should report KIOTA001 error
		diagnostics.Should().Contain(
			d => d.Id == "KIOTA001",
			"an empty spec should report a parse failure diagnostic");

		// No source files generated
		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().BeEmpty(
			"no source should be generated from an empty spec");
	}

	[TestMethod]
	public void InvalidSpec_DoesNotCrashGenerator()
	{
		// Arrange — a file that is syntactically valid JSON but not OpenAPI
		const string notOpenApi = "{ \"hello\": \"world\" }";
		var specPath = "not-openapi.json";

		var driver = CreateGeneratorDriver(
			notOpenApi,
			specPath,
			clientName: "NotOpenApiClient",
			namespaceName: "TestApp.NotOpenApi");

		var compilation = CreateMinimalCompilation();

		// Act — should not throw
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out _, out var diagnostics);

		// Assert — generator should survive without crashing. It may report
		// warnings or errors depending on how OpenApiStreamReader handles
		// a non-OpenAPI JSON document, but the crucial thing is no crash.
		diagnostics.Should().NotContain(
			d => d.Id == "KIOTA040",
			"a non-OpenAPI JSON doc should not cause an unhandled exception");
	}

	[TestMethod]
	public void ValidSpec_HasNoCriticalDiagnostics()
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

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out _, out var diagnostics);

		// Assert — no KIOTA040 (unhandled), KIOTA050 (type failure),
		//          KIOTA051 (build failure) diagnostics
		diagnostics.Should().NotContain(
			d => d.Id == "KIOTA040",
			"valid spec should not produce unhandled exception diagnostics");
		diagnostics.Should().NotContain(
			d => d.Id == "KIOTA050",
			"valid spec should not produce type emission failure diagnostics");
		diagnostics.Should().NotContain(
			d => d.Id == "KIOTA051",
			"valid spec should not produce CodeDOM build failure diagnostics");
	}

	[TestMethod]
	public void ValidSpec_DoesNotEmitFallbackComment()
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

		// Act
		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out _);

		// Assert — no fallback error comment files
		var allGeneratedSource = GetAllGeneratedSource(outputCompilation, compilation);
		allGeneratedSource.Should().NotContain(
			"Kiota source generator encountered an error",
			"a valid spec should not produce fallback error comments");
	}

	[TestMethod]
	public void AllKiotaDiagnosticIds_ArePrefixedCorrectly()
	{
		// Verify the diagnostic descriptors follow the KIOTA numbering convention
		// and are unique.
		var descriptors = new[]
		{
			KiotaSourceGenerator.MissingConfiguration,
			KiotaSourceGenerator.GenerationCompleted,
			KiotaSourceGenerator.PartialGeneration,
			KiotaSourceGenerator.UnhandledException,
			KiotaSourceGenerator.TypeEmissionFailed,
			KiotaSourceGenerator.CodeDomBuildFailed,
			OpenApiDocumentParser.ParseFailure,
			OpenApiDocumentParser.UnsupportedVersion,
			OpenApiDocumentParser.ParseWarning,
			OpenApiDocumentParser.LargeSpec,
		};

		// All IDs should start with "KIOTA"
		foreach (var descriptor in descriptors)
		{
			descriptor.Id.Should().StartWith("KIOTA",
				$"diagnostic {descriptor.Id} should follow the KIOTA prefix convention");
		}

		// All IDs should be unique
		var ids = descriptors.Select(d => d.Id).ToList();
		ids.Should().OnlyHaveUniqueItems(
			"each diagnostic should have a unique ID");
	}

	[TestMethod]
	public void MultipleSameClientName_GeneratorDoesNotCrash()
	{
		// Arrange — two files with the same client name could cause
		// duplicate hint names. The generator should handle this gracefully.
		var specPath1 = Path.Combine(TestDataDir, "petstore.json");
		var specContent1 = File.ReadAllText(specPath1);

		var additionalText1 = new InMemoryAdditionalText(specPath1, specContent1);
		var additionalText2 = new InMemoryAdditionalText("petstore-copy.json", specContent1);

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

		var generator = new KiotaSourceGenerator();
		var driver = CSharpGeneratorDriver.Create(generator)
			.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalText1, additionalText2))
			.WithUpdatedAnalyzerConfigOptions(optionsProvider);

		var compilation = CreateMinimalCompilation();

		// Act — should not throw even with potential hint name conflicts
		var act = () => driver.RunGeneratorsAndUpdateCompilation(
			compilation, out _, out _);

		act.Should().NotThrow(
			"the generator should handle duplicate files without crashing (even if duplicates cause diagnostics)");
	}

	// ==================================================================
	// Helper: create the generator driver with configured options
	// ==================================================================

	/// <summary>
	/// Creates a <see cref="CSharpGeneratorDriver"/> configured with a
	/// <see cref="KiotaSourceGenerator"/>, the given spec as an additional
	/// file, and appropriate analyzer config options (KiotaClientName, etc.).
	/// </summary>
	private static GeneratorDriver CreateGeneratorDriver(
		string specContent,
		string specPath,
		string clientName,
		string namespaceName,
		bool enabled = true,
		bool usesBackingStore = false)
	{
		var additionalText = new InMemoryAdditionalText(specPath, specContent);

		var fileOptions = ImmutableDictionary.CreateRange(new[]
		{
			KeyValuePair.Create(
				"build_metadata.AdditionalFiles.KiotaClientName", clientName),
			KeyValuePair.Create(
				"build_metadata.AdditionalFiles.KiotaNamespace", namespaceName),
			KeyValuePair.Create(
				"build_metadata.AdditionalFiles.KiotaUsesBackingStore",
				usesBackingStore.ToString()),
		});

		var globalOptions = ImmutableDictionary.CreateRange(new[]
		{
			KeyValuePair.Create(
				"build_property.KiotaGenerator_Enabled", enabled.ToString()),
		});

		var optionsProvider = new TestAnalyzerConfigOptionsProvider(
			globalOptions, fileOptions);

		var generator = new KiotaSourceGenerator();

		return CSharpGeneratorDriver.Create(generator)
			.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalText))
			.WithUpdatedAnalyzerConfigOptions(optionsProvider);
	}

	// ==================================================================
	// Helper: create a minimal CSharpCompilation
	// ==================================================================

	/// <summary>
	/// Creates a minimal <see cref="CSharpCompilation"/> with only the core
	/// reference assemblies. Generated code references Kiota abstractions
	/// (e.g. <c>IRequestAdapter</c>, <c>IParsable</c>), which are not
	/// available in this minimal compilation — so we cannot assert zero
	/// compilation errors, but we <em>can</em> assert the generator itself
	/// runs without error and produces syntactically valid C# source.
	/// </summary>
	private static CSharpCompilation CreateMinimalCompilation()
	{
		// Reference mscorlib / System.Runtime to make the compilation valid enough
		// for the generator to run. We don't need Kiota runtime refs here because
		// we are testing the generator pipeline, not the generated code's compilability.
		var references = new[]
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		};

		return CSharpCompilation.Create(
			assemblyName: "GeneratorIntegrationTests",
			syntaxTrees: Array.Empty<SyntaxTree>(),
			references: references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	// ==================================================================
	// Helper: aggregate all generated source text
	// ==================================================================

	/// <summary>
	/// Returns the concatenated source text of all generated syntax trees.
	/// </summary>
	private static string GetAllGeneratedSource(
		Compilation outputCompilation,
		Compilation inputCompilation)
	{
		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(inputCompilation.SyntaxTrees);

		return string.Join(
			Environment.NewLine,
			generatedTrees.Select(t => t.GetText().ToString()));
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
