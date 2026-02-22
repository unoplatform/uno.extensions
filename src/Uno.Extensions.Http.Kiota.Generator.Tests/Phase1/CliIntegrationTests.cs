using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Http.Kiota.Generator.Cli;

namespace Uno.Extensions.Http.Kiota.Generator.Tests.Phase1;

/// <summary>
/// Integration tests for the <c>kiota-gen</c> CLI wrapper.
/// <para>
/// These tests validate three aspects of the CLI:
/// <list type="bullet">
///   <item><b>Argument mapping</b>: each CLI option is correctly projected
///     onto <c>GenerationConfiguration</c> via <see cref="GeneratorOptions"/>.
///     These run in-process and test the pure mapping logic.</item>
///   <item><b>End-to-end generation</b>: the CLI produces C# output from
///     test specs. These run the CLI as a subprocess to avoid assembly
///     version conflicts (the test project pins <c>Microsoft.OpenApi</c>
///     1.6.28 for source generator tests, while <c>Kiota.Builder</c>
///     requires v3.x).</item>
///   <item><b>Error handling</b>: invalid specs and missing arguments
///     produce non-zero exit codes.</item>
/// </list>
/// </para>
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class CliIntegrationTests
{
	// ------------------------------------------------------------------
	// Test data paths — resolved relative to the test output directory
	// ------------------------------------------------------------------

	private static string TestDataDir =>
		Path.Combine(AppContext.BaseDirectory, "TestData");

	private static string GoldenFilesDir =>
		Path.Combine(AppContext.BaseDirectory, "GoldenFiles");

	// ------------------------------------------------------------------
	// Temp output directory management
	// ------------------------------------------------------------------

	private string _tempOutputDir = null!;

	[TestInitialize]
	public void TestInit()
	{
		_tempOutputDir = Path.Combine(Path.GetTempPath(), "KiotaCliTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempOutputDir);
	}

	[TestCleanup]
	public void TestCleanup()
	{
		try
		{
			if (Directory.Exists(_tempOutputDir))
			{
				Directory.Delete(_tempOutputDir, recursive: true);
			}
		}
		catch
		{
			// Best effort cleanup — don't fail the test on cleanup errors.
		}
	}

	// ==================================================================
	// BuildConfiguration — argument mapping unit tests
	// ==================================================================

	[TestMethod]
	public void BuildConfiguration_DefaultOptions_MapsKnownDefaults()
	{
		// Arrange
		var options = new GeneratorOptions
		{
			OpenApiPath = "spec.json",
			OutputPath = "/out",
		};

		// Act
		var config = KiotaGeneratorCommand.BuildConfiguration(options);

		// Assert
		config.OpenAPIFilePath.Should().Be("spec.json");
		config.OutputPath.Should().Be("/out");
		config.Language.ToString().Should().Be("CSharp");
		config.ClientClassName.Should().Be("ApiClient");
		config.ClientNamespaceName.Should().Be("ApiSdk");
		config.UsesBackingStore.Should().BeFalse();
		config.IncludeAdditionalData.Should().BeTrue();
		config.ExcludeBackwardCompatible.Should().BeFalse();
		config.TypeAccessModifier.ToString().Should().Be("Public");
		config.CleanOutput.Should().BeFalse();
	}

	[TestMethod]
	public void BuildConfiguration_AllOptions_MapsCorrectly()
	{
		// Arrange
		var options = new GeneratorOptions
		{
			OpenApiPath = "https://api.example.com/spec.yaml",
			OutputPath = "/generated",
			ClassName = "MyClient",
			Namespace = "Contoso.Api",
			UsesBackingStore = true,
			IncludeAdditionalData = false,
			ExcludeBackwardCompatible = true,
			TypeAccessModifier = "Internal",
			IncludePatterns = new[] { "/pets/**", "/owners/**" },
			ExcludePatterns = new[] { "/admin/**" },
			Serializers = new[] { "MySerializer" },
			Deserializers = new[] { "MyDeserializer" },
			StructuredMimeTypes = new[] { "application/json;q=1" },
			CleanOutput = true,
			DisableValidationRules = new[] { "NoServerEntry" },
			LogLevel = LogLevel.Debug,
		};

		// Act
		var config = KiotaGeneratorCommand.BuildConfiguration(options);

		// Assert
		config.OpenAPIFilePath.Should().Be("https://api.example.com/spec.yaml");
		config.OutputPath.Should().Be("/generated");
		config.ClientClassName.Should().Be("MyClient");
		config.ClientNamespaceName.Should().Be("Contoso.Api");
		config.UsesBackingStore.Should().BeTrue();
		config.IncludeAdditionalData.Should().BeFalse();
		config.ExcludeBackwardCompatible.Should().BeTrue();
		config.TypeAccessModifier.ToString().Should().Be("Internal");
		config.CleanOutput.Should().BeTrue();
		config.IncludePatterns.Should().BeEquivalentTo(new[] { "/pets/**", "/owners/**" });
		config.ExcludePatterns.Should().BeEquivalentTo(new[] { "/admin/**" });
		config.Serializers.Should().BeEquivalentTo(new[] { "MySerializer" });
		config.Deserializers.Should().BeEquivalentTo(new[] { "MyDeserializer" });
		config.StructuredMimeTypes.Should().Contain(s => s.Contains("application/json"));
		config.DisabledValidationRules.Should().BeEquivalentTo(new[] { "NoServerEntry" });
	}

	[TestMethod]
	public void BuildConfiguration_AccessModifier_Internal_MapsCorrectly()
	{
		var options = new GeneratorOptions
		{
			OpenApiPath = "spec.json",
			OutputPath = "/out",
			TypeAccessModifier = "Internal",
		};

		var config = KiotaGeneratorCommand.BuildConfiguration(options);

		config.TypeAccessModifier.ToString().Should().Be("Internal");
	}

	[TestMethod]
	[DataRow("Public", "Public")]
	[DataRow("public", "Public")]
	[DataRow("PUBLIC", "Public")]
	[DataRow("Internal", "Internal")]
	[DataRow("internal", "Internal")]
	[DataRow("INTERNAL", "Internal")]
	[DataRow("Unknown", "Public")]
	[DataRow("", "Public")]
	public void BuildConfiguration_AccessModifier_ParsesCaseInsensitively(
		string input, string expectedName)
	{
		var options = new GeneratorOptions
		{
			OpenApiPath = "spec.json",
			OutputPath = "/out",
			TypeAccessModifier = input,
		};

		var config = KiotaGeneratorCommand.BuildConfiguration(options);

		config.TypeAccessModifier.ToString().Should().Be(expectedName);
	}

	[TestMethod]
	public void BuildConfiguration_EmptyArrayOptions_DoNotOverrideDefaults()
	{
		var options = new GeneratorOptions
		{
			OpenApiPath = "spec.json",
			OutputPath = "/out",
			IncludePatterns = Array.Empty<string>(),
			ExcludePatterns = Array.Empty<string>(),
			Serializers = Array.Empty<string>(),
			Deserializers = Array.Empty<string>(),
			StructuredMimeTypes = Array.Empty<string>(),
			DisableValidationRules = Array.Empty<string>(),
		};

		var config = KiotaGeneratorCommand.BuildConfiguration(options);

		// When arrays are empty, the default GenerationConfiguration values
		// should be preserved (not replaced with empty sets).
		// The exact defaults depend on the Kiota library version, but they
		// should not be empty for serializers/deserializers/mimeTypes.
		config.Serializers.Should().NotBeNull();
		config.Deserializers.Should().NotBeNull();
		config.StructuredMimeTypes.Should().NotBeNull();
	}

	[TestMethod]
	public void BuildConfiguration_IncludePatterns_ProjectedToHashSet()
	{
		var options = new GeneratorOptions
		{
			OpenApiPath = "spec.json",
			OutputPath = "/out",
			IncludePatterns = new[] { "/pets/**", "/owners/{id}" },
		};

		var config = KiotaGeneratorCommand.BuildConfiguration(options);

		config.IncludePatterns.Should().HaveCount(2);
		config.IncludePatterns.Should().Contain("/pets/**");
		config.IncludePatterns.Should().Contain("/owners/{id}");
	}

	[TestMethod]
	public void BuildConfiguration_ExcludePatterns_ProjectedToHashSet()
	{
		var options = new GeneratorOptions
		{
			OpenApiPath = "spec.json",
			OutputPath = "/out",
			ExcludePatterns = new[] { "/admin/**", "/internal/**" },
		};

		var config = KiotaGeneratorCommand.BuildConfiguration(options);

		config.ExcludePatterns.Should().HaveCount(2);
		config.ExcludePatterns.Should().Contain("/admin/**");
		config.ExcludePatterns.Should().Contain("/internal/**");
	}

	[TestMethod]
	public void BuildConfiguration_Serializers_ProjectedToHashSet()
	{
		var options = new GeneratorOptions
		{
			OpenApiPath = "spec.json",
			OutputPath = "/out",
			Serializers = new[] { "Ns.JsonWriterFactory", "Ns.XmlWriterFactory" },
		};

		var config = KiotaGeneratorCommand.BuildConfiguration(options);

		config.Serializers.Should().HaveCount(2);
		config.Serializers.Should().Contain("Ns.JsonWriterFactory");
		config.Serializers.Should().Contain("Ns.XmlWriterFactory");
	}

	[TestMethod]
	public void BuildConfiguration_Deserializers_ProjectedToHashSet()
	{
		var options = new GeneratorOptions
		{
			OpenApiPath = "spec.json",
			OutputPath = "/out",
			Deserializers = new[] { "Ns.JsonParseFactory" },
		};

		var config = KiotaGeneratorCommand.BuildConfiguration(options);

		config.Deserializers.Should().HaveCount(1);
		config.Deserializers.Should().Contain("Ns.JsonParseFactory");
	}

	[TestMethod]
	public void BuildConfiguration_StructuredMimeTypes_ProjectedToCollection()
	{
		var options = new GeneratorOptions
		{
			OpenApiPath = "spec.json",
			OutputPath = "/out",
			StructuredMimeTypes = new[] { "application/json;q=1", "application/xml;q=0.9" },
		};

		var config = KiotaGeneratorCommand.BuildConfiguration(options);

		// StructuredMimeTypesCollection may normalise the quality parameters,
		// so we verify the collection was populated with the expected count
		// and that the base MIME types are present.
		config.StructuredMimeTypes.Should().HaveCount(2);
		config.StructuredMimeTypes.Should().Contain(s => s.Contains("application/json"));
		config.StructuredMimeTypes.Should().Contain(s => s.Contains("application/xml"));
	}

	[TestMethod]
	public void BuildConfiguration_DisableValidationRules_ProjectedToHashSet()
	{
		var options = new GeneratorOptions
		{
			OpenApiPath = "spec.json",
			OutputPath = "/out",
			DisableValidationRules = new[] { "NoServerEntry", "DuplicateOperationId" },
		};

		var config = KiotaGeneratorCommand.BuildConfiguration(options);

		config.DisabledValidationRules.Should().HaveCount(2);
		config.DisabledValidationRules.Should().Contain("NoServerEntry");
		config.DisabledValidationRules.Should().Contain("DuplicateOperationId");
	}

	[TestMethod]
	public void BuildConfiguration_LanguageAlwaysCSharp()
	{
		var options = new GeneratorOptions
		{
			OpenApiPath = "spec.json",
			OutputPath = "/out",
		};

		var config = KiotaGeneratorCommand.BuildConfiguration(options);

		config.Language.ToString().Should().Be("CSharp");
	}

	// ==================================================================
	// CLI invocation — end-to-end process-based integration tests
	//
	// The CLI runs as a subprocess (dotnet <dll>) to avoid assembly
	// conflicts: the test project pins Microsoft.OpenApi 1.6.28 for
	// source generator tests, while Kiota.Builder requires v3.x.
	// ==================================================================

	[TestMethod]
	public async Task Cli_PetstoreSpec_ReturnsExitCodeZeroAsync()
	{
		var specPath = Path.Combine(TestDataDir, "petstore.json");
		File.Exists(specPath).Should().BeTrue($"test spec should exist at {specPath}");

		var result = await RunCliProcessAsync(
			"--openapi", specPath,
			"--output", _tempOutputDir,
			"--class-name", "PetStoreClient",
			"--namespace", "PetStore.Client",
			"--log-level", "Warning");

		result.ExitCode.Should().Be(0,
			$"CLI should succeed for a valid petstore spec.\nStdErr: {result.StdErr}");
	}

	[TestMethod]
	public async Task Cli_PetstoreSpec_GeneratesCSharpFilesAsync()
	{
		var specPath = Path.Combine(TestDataDir, "petstore.json");

		var result = await RunCliProcessAsync(
			"--openapi", specPath,
			"--output", _tempOutputDir,
			"--class-name", "PetStoreClient",
			"--namespace", "PetStore.Client",
			"--log-level", "Warning");

		result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");

		var generatedFiles = Directory.GetFiles(_tempOutputDir, "*.cs", SearchOption.AllDirectories);
		generatedFiles.Should().NotBeEmpty("CLI should generate .cs files from petstore spec");

		generatedFiles.Should().Contain(f =>
			Path.GetFileName(f).Equals("PetStoreClient.cs", StringComparison.OrdinalIgnoreCase),
			"a PetStoreClient.cs file should be generated");
	}

	[TestMethod]
	public async Task Cli_PetstoreSpec_GeneratesModelFilesAsync()
	{
		var specPath = Path.Combine(TestDataDir, "petstore.json");

		var result = await RunCliProcessAsync(
			"--openapi", specPath,
			"--output", _tempOutputDir,
			"--class-name", "PetStoreClient",
			"--namespace", "PetStore.Client",
			"--log-level", "Warning");

		result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");

		var modelsDir = Path.Combine(_tempOutputDir, "Models");
		Directory.Exists(modelsDir).Should().BeTrue(
			"a Models/ subdirectory should be generated for the petstore spec");

		var modelFiles = Directory.GetFiles(modelsDir, "*.cs", SearchOption.AllDirectories);
		modelFiles.Should().NotBeEmpty("model .cs files should exist in the Models/ directory");
	}

	[TestMethod]
	public async Task Cli_PetstoreSpec_ClientFileMatchesGoldenFileAsync()
	{
		var specPath = Path.Combine(TestDataDir, "petstore.json");

		var result = await RunCliProcessAsync(
			"--openapi", specPath,
			"--output", _tempOutputDir,
			"--class-name", "PetStoreClient",
			"--namespace", "PetStore.Client",
			"--log-level", "Warning");

		result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");

		var generatedClientPath = Path.Combine(_tempOutputDir, "PetStoreClient.cs");
		File.Exists(generatedClientPath).Should().BeTrue(
			"PetStoreClient.cs should be generated");

		var goldenClientPath = Path.Combine(GoldenFilesDir, "petstore", "PetStoreClient.cs");
		File.Exists(goldenClientPath).Should().BeTrue(
			"golden file PetStoreClient.cs should exist");

		var generatedContent = NormalizeSource(File.ReadAllText(generatedClientPath));
		var goldenContent = NormalizeSource(File.ReadAllText(goldenClientPath));

		generatedContent.Should().Be(goldenContent,
			"generated PetStoreClient.cs should match the golden reference file " +
			"(after normalizing version strings and whitespace)");
	}

	[TestMethod]
	public async Task Cli_PetstoreSpec_AllGoldenFilesHaveMatchingOutputAsync()
	{
		var specPath = Path.Combine(TestDataDir, "petstore.json");

		var result = await RunCliProcessAsync(
			"--openapi", specPath,
			"--output", _tempOutputDir,
			"--class-name", "PetStoreClient",
			"--namespace", "PetStore.Client",
			"--log-level", "Warning");

		result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");

		var goldenDir = Path.Combine(GoldenFilesDir, "petstore");
		Directory.Exists(goldenDir).Should().BeTrue();

		var goldenFiles = Directory.GetFiles(goldenDir, "*.cs", SearchOption.AllDirectories);
		var failures = new List<string>();

		foreach (var goldenFile in goldenFiles)
		{
			var relativePath = Path.GetRelativePath(goldenDir, goldenFile);
			var generatedFile = Path.Combine(_tempOutputDir, relativePath);

			if (!File.Exists(generatedFile))
			{
				failures.Add($"Missing: {relativePath}");
				continue;
			}

			var goldenContent = NormalizeSource(File.ReadAllText(goldenFile));
			var generatedContent = NormalizeSource(File.ReadAllText(generatedFile));

			if (goldenContent != generatedContent)
			{
				var goldenLines = goldenContent.Split('\n');
				var generatedLines = generatedContent.Split('\n');
				var firstDiff = FindFirstDifference(goldenLines, generatedLines);
				failures.Add($"Content mismatch: {relativePath} (first diff at line {firstDiff + 1})");
			}
		}

		failures.Should().BeEmpty(
			"all golden files should match CLI output. Failures:\n" +
			string.Join("\n", failures));
	}

	[TestMethod]
	public async Task Cli_CustomClassName_AppliedToOutputAsync()
	{
		var specPath = Path.Combine(TestDataDir, "petstore.json");

		var result = await RunCliProcessAsync(
			"--openapi", specPath,
			"--output", _tempOutputDir,
			"--class-name", "MyPetApi",
			"--namespace", "Test.Custom",
			"--log-level", "Warning");

		result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");

		var clientFile = Path.Combine(_tempOutputDir, "MyPetApi.cs");
		File.Exists(clientFile).Should().BeTrue(
			"the generated client file should use the custom class name");

		var content = File.ReadAllText(clientFile);
		content.Should().Contain("class MyPetApi",
			"the generated class should use the custom class name");
		content.Should().Contain("namespace Test.Custom",
			"the generated code should use the custom namespace");
	}

	[TestMethod]
	public async Task Cli_CustomNamespace_AppliedToOutputAsync()
	{
		var specPath = Path.Combine(TestDataDir, "petstore.json");

		var result = await RunCliProcessAsync(
			"--openapi", specPath,
			"--output", _tempOutputDir,
			"--class-name", "PetStoreClient",
			"--namespace", "My.Custom.Namespace",
			"--log-level", "Warning");

		result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");

		var clientFile = Path.Combine(_tempOutputDir, "PetStoreClient.cs");
		File.Exists(clientFile).Should().BeTrue();

		var content = File.ReadAllText(clientFile);
		content.Should().Contain("namespace My.Custom.Namespace",
			"the generated code should use the custom namespace");
	}

	[TestMethod]
	public async Task Cli_ExcludeBackwardCompatible_OmitsDeprecatedCodeAsync()
	{
		var specPath = Path.Combine(TestDataDir, "petstore.json");

		// Generate WITH backward compatible code
		var withBackcompat = Path.Combine(_tempOutputDir, "with_backcompat");
		Directory.CreateDirectory(withBackcompat);
		var r1 = await RunCliProcessAsync(
			"--openapi", specPath,
			"--output", withBackcompat,
			"--class-name", "PetStoreClient",
			"--namespace", "PetStore.Client",
			"--log-level", "Warning");
		r1.ExitCode.Should().Be(0, $"StdErr: {r1.StdErr}");

		// Generate WITHOUT backward compatible code
		var withoutBackcompat = Path.Combine(_tempOutputDir, "without_backcompat");
		Directory.CreateDirectory(withoutBackcompat);
		var r2 = await RunCliProcessAsync(
			"--openapi", specPath,
			"--output", withoutBackcompat,
			"--class-name", "PetStoreClient",
			"--namespace", "PetStore.Client",
			"--exclude-backward-compatible",
			"--log-level", "Warning");
		r2.ExitCode.Should().Be(0, $"StdErr: {r2.StdErr}");

		var withFiles = Directory.GetFiles(withBackcompat, "*.cs", SearchOption.AllDirectories);
		var withoutFiles = Directory.GetFiles(withoutBackcompat, "*.cs", SearchOption.AllDirectories);

		withFiles.Should().NotBeEmpty();
		withoutFiles.Should().NotBeEmpty();

		// Backward-compat-excluded output should have equal or fewer files
		withoutFiles.Length.Should().BeLessThanOrEqualTo(withFiles.Length,
			"excluding backward compatible code should not produce more files");
	}

	// ==================================================================
	// Error handling (process-based)
	// ==================================================================

	[TestMethod]
	public async Task Cli_InvalidSpec_ProducesNoUsefulOutputAsync()
	{
		// Kiota is lenient with malformed input — it may return exit code 0
		// while producing warnings instead of generated files. We verify
		// that no usable client file is produced.
		var invalidSpecPath = Path.Combine(_tempOutputDir, "invalid.json");
		File.WriteAllText(invalidSpecPath, "{ this is not valid OpenAPI }");

		var outDir = Path.Combine(_tempOutputDir, "out");
		var result = await RunCliProcessAsync(
			"--openapi", invalidSpecPath,
			"--output", outDir,
			"--class-name", "BadClient",
			"--namespace", "Test.Bad",
			"--log-level", "Warning");

		// Either a non-zero exit code or no client file generated
		var clientFile = Path.Combine(outDir, "BadClient.cs");
		if (result.ExitCode == 0)
		{
			File.Exists(clientFile).Should().BeFalse(
				"when Kiota returns 0 for invalid input, no client file should be generated");
		}
	}

	[TestMethod]
	public async Task Cli_EmptySpec_ReturnsNonZeroExitCodeAsync()
	{
		var emptySpecPath = Path.Combine(_tempOutputDir, "empty.json");
		File.WriteAllText(emptySpecPath, "");

		var result = await RunCliProcessAsync(
			"--openapi", emptySpecPath,
			"--output", Path.Combine(_tempOutputDir, "out"),
			"--class-name", "EmptyClient",
			"--namespace", "Test.Empty",
			"--log-level", "Warning");

		result.ExitCode.Should().NotBe(0,
			"empty specs should cause a non-zero exit code");
	}

	[TestMethod]
	public async Task Cli_NonExistentSpec_ReturnsNonZeroExitCodeAsync()
	{
		var result = await RunCliProcessAsync(
			"--openapi", Path.Combine(_tempOutputDir, "does_not_exist.json"),
			"--output", Path.Combine(_tempOutputDir, "out"),
			"--class-name", "MissingClient",
			"--namespace", "Test.Missing",
			"--log-level", "Warning");

		result.ExitCode.Should().NotBe(0,
			"referencing a non-existent spec file should cause a non-zero exit code");
	}

	[TestMethod]
	public async Task Cli_MissingRequiredOpenApiArg_ReturnsNonZeroExitCodeAsync()
	{
		var result = await RunCliProcessAsync(
			"--output", _tempOutputDir,
			"--class-name", "NoSpecClient",
			"--namespace", "Test.NoSpec");

		result.ExitCode.Should().NotBe(0,
			"missing required --openapi argument should produce an error exit code");
	}

	[TestMethod]
	public async Task Cli_MissingRequiredOutputArg_ReturnsNonZeroExitCodeAsync()
	{
		var specPath = Path.Combine(TestDataDir, "petstore.json");

		var result = await RunCliProcessAsync(
			"--openapi", specPath,
			"--class-name", "NoOutputClient",
			"--namespace", "Test.NoOutput");

		result.ExitCode.Should().NotBe(0,
			"missing required --output argument should produce an error exit code");
	}

	[TestMethod]
	public async Task Cli_CleanOutput_RemovesPreviousFilesAsync()
	{
		var specPath = Path.Combine(TestDataDir, "petstore.json");
		var outputDir = Path.Combine(_tempOutputDir, "clean_test");
		Directory.CreateDirectory(outputDir);

		// Create a stale file that should be removed by --clean-output
		var staleFile = Path.Combine(outputDir, "StaleFile.cs");
		File.WriteAllText(staleFile, "// Should be removed by clean-output");

		var result = await RunCliProcessAsync(
			"--openapi", specPath,
			"--output", outputDir,
			"--class-name", "PetStoreClient",
			"--namespace", "PetStore.Client",
			"--clean-output",
			"--log-level", "Warning");

		result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");
		File.Exists(staleFile).Should().BeFalse(
			"--clean-output should remove pre-existing files before generating");
	}

	[TestMethod]
	public async Task Cli_InheritanceSpec_GeneratesBaseAndDerivedClassesAsync()
	{
		var specPath = Path.Combine(TestDataDir, "inheritance.json");
		File.Exists(specPath).Should().BeTrue($"test spec should exist at {specPath}");

		var result = await RunCliProcessAsync(
			"--openapi", specPath,
			"--output", _tempOutputDir,
			"--class-name", "InheritanceClient",
			"--namespace", "Test.Inheritance",
			"--log-level", "Warning");

		result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");

		var generatedFiles = Directory.GetFiles(_tempOutputDir, "*.cs", SearchOption.AllDirectories);
		generatedFiles.Should().NotBeEmpty(
			"the inheritance spec should produce generated C# files");
	}

	[TestMethod]
	public async Task Cli_EnumsSpec_GeneratesOutputAsync()
	{
		var specPath = Path.Combine(TestDataDir, "enums.json");
		File.Exists(specPath).Should().BeTrue($"test spec should exist at {specPath}");

		var result = await RunCliProcessAsync(
			"--openapi", specPath,
			"--output", _tempOutputDir,
			"--class-name", "EnumsClient",
			"--namespace", "Test.Enums",
			"--log-level", "Warning");

		result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");

		var generatedFiles = Directory.GetFiles(_tempOutputDir, "*.cs", SearchOption.AllDirectories);
		generatedFiles.Should().NotBeEmpty(
			"the enums spec should produce generated C# files");
	}

	// ==================================================================
	// CLI process runner
	// ==================================================================

	/// <summary>
	/// Resolves the path to the <c>kiota-gen.dll</c> CLI binary in the
	/// Generator.Cli project's output directory.
	/// </summary>
	private static string FindCliDll()
	{
		// Test output follows the pattern:
		//   {src}/{TestProject}/bin/{TestProject}/{Config}/{TFM}/
		// Navigate up to {src}/ then down to the CLI project output.
		var baseDir = AppContext.BaseDirectory
			.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		var segments = baseDir.Split(Path.DirectorySeparatorChar);
		var configuration = segments[^2]; // "Debug" or "Release"

		// Up 5 directories from the TFM folder to reach the {src}/ directory
		var srcDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));

		var cliDll = Path.Combine(srcDir,
			"Uno.Extensions.Http.Kiota.Generator.Cli",
			"bin", "Uno.Extensions.Http.Kiota.Generator.Cli",
			configuration, "net9.0", "kiota-gen.dll");

		cliDll = Path.GetFullPath(cliDll);

		if (!File.Exists(cliDll))
		{
			// Fallback: try net8.0
			cliDll = Path.Combine(srcDir,
				"Uno.Extensions.Http.Kiota.Generator.Cli",
				"bin", "Uno.Extensions.Http.Kiota.Generator.Cli",
				configuration, "net8.0", "kiota-gen.dll");
			cliDll = Path.GetFullPath(cliDll);
		}

		return cliDll;
	}

	/// <summary>
	/// Runs the <c>kiota-gen</c> CLI as a subprocess via <c>dotnet &lt;dll&gt;</c>.
	/// This avoids assembly version conflicts between the test project's
	/// <c>Microsoft.OpenApi</c> 1.6.28 and <c>Kiota.Builder</c>'s v3.x dependency.
	/// </summary>
	private static async Task<CliResult> RunCliProcessAsync(params string[] args)
	{
		var cliDll = FindCliDll();

		File.Exists(cliDll).Should().BeTrue(
			$"kiota-gen.dll should exist at: {cliDll}. " +
			"Ensure the Generator.Cli project has been built.");

		var psi = new ProcessStartInfo("dotnet")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		psi.ArgumentList.Add(cliDll);
		foreach (var arg in args)
		{
			psi.ArgumentList.Add(arg);
		}

		using var process = Process.Start(psi)!;

		// Read stdout and stderr asynchronously to avoid deadlocks
		var stdoutTask = process.StandardOutput.ReadToEndAsync();
		var stderrTask = process.StandardError.ReadToEndAsync();

		await process.WaitForExitAsync();

		return new CliResult(
			process.ExitCode,
			await stdoutTask,
			await stderrTask);
	}

	/// <summary>Captures the result of a CLI process invocation.</summary>
	private sealed record CliResult(int ExitCode, string StdOut, string StdErr);

	// ==================================================================
	// Normalization helpers (mirrors ParityTestBase.Normalize)
	// ==================================================================

	/// <summary>
	/// Regex matching Kiota version strings in <c>[GeneratedCode("Kiota", "...")]</c>.
	/// </summary>
	private static readonly Regex s_versionPattern = new(
		@"\[global::System\.CodeDom\.Compiler\.GeneratedCode\(""Kiota"",\s*""[^""]*""\)\]",
		RegexOptions.Compiled);

	/// <summary>
	/// Normalizes source text for golden-file comparison by removing
	/// acceptable differences: line endings, version strings, trailing whitespace.
	/// </summary>
	private static string NormalizeSource(string source)
	{
		if (string.IsNullOrEmpty(source))
		{
			return string.Empty;
		}

		// Normalize line endings to LF
		var normalized = source.Replace("\r\n", "\n").Replace("\r", "\n");

		// Replace version strings with placeholder
		normalized = s_versionPattern.Replace(
			normalized,
			@"[global::System.CodeDom.Compiler.GeneratedCode(""Kiota"", ""VERSION"")]");

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
	/// Finds the index of the first line that differs between two arrays.
	/// </summary>
	private static int FindFirstDifference(string[] expected, string[] actual)
	{
		var minLen = Math.Min(expected.Length, actual.Length);
		for (int i = 0; i < minLen; i++)
		{
			if (expected[i] != actual[i])
			{
				return i;
			}
		}

		return minLen; // Lengths differ
	}
}
