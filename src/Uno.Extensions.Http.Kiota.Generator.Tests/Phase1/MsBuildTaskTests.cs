using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Http.Kiota.Generator.Tasks;

namespace Uno.Extensions.Http.Kiota.Generator.Tests.Phase1;

/// <summary>
/// Unit and integration tests for <see cref="KiotaGenerateTask"/>.
/// <para>
/// These tests validate:
/// <list type="bullet">
///   <item><b>Parameter validation</b>: required inputs trigger structured
///     MSBuild errors when missing.</item>
///   <item><b>Command line generation</b>: all task properties are correctly
///     projected onto the CLI argument string.</item>
///   <item><b>Diagnostic parsing</b>: structured <c>error KIOTAXXX</c> /
///     <c>warning KIOTAXXX</c> lines are converted into MSBuild diagnostics.</item>
///   <item><b>Tool resolution</b>: framework-dependent (DLL) vs self-contained
///     (EXE) modes resolve the correct tool name and path.</item>
///   <item><b>Error handling</b>: missing tool, missing spec, invalid inputs
///     produce appropriate MSBuild errors.</item>
/// </list>
/// </para>
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class MsBuildTaskTests
{
	// ==================================================================
	// Tool resolution — ToolName property
	// ==================================================================

	[TestMethod]
	public void ToolName_WhenDllSet_ReturnsDotnet()
	{
		var task = CreateTask(t => t.KiotaToolDll = @"C:\tools\kiota-gen.dll");

		task.ExposedToolName.Should().Be("dotnet",
			"framework-dependent mode should use 'dotnet' as the tool");
	}

	[TestMethod]
	public void ToolName_WhenExeSet_ReturnsExeFileName()
	{
		var task = CreateTask(t => t.KiotaToolExe = @"C:\tools\win-x64\kiota-gen.exe");

		task.ExposedToolName.Should().Be("kiota-gen.exe",
			"self-contained mode should use the executable file name");
	}

	[TestMethod]
	public void ToolName_WhenBothSet_PrefersDll()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.KiotaToolExe = @"C:\tools\kiota-gen.exe";
		});

		task.ExposedToolName.Should().Be("dotnet",
			"when both DLL and EXE are set, framework-dependent (DLL) should take priority");
	}

	[TestMethod]
	public void ToolName_WhenNeitherSet_ReturnsFallbackName()
	{
		var task = CreateTask();

		task.ExposedToolName.Should().Be("kiota-gen",
			"when neither tool path is set, should return the fallback tool name");
	}

	// ==================================================================
	// Tool resolution — GenerateFullPathToTool
	// ==================================================================

	[TestMethod]
	public void GenerateFullPathToTool_WhenDllSet_ReturnsDotnet()
	{
		var task = CreateTask(t => t.KiotaToolDll = @"C:\tools\kiota-gen.dll");

		task.ExposedGenerateFullPathToTool().Should().Be("dotnet",
			"framework-dependent mode should resolve to 'dotnet'");
	}

	[TestMethod]
	public void GenerateFullPathToTool_WhenExeSet_ReturnsExePath()
	{
		var task = CreateTask(t => t.KiotaToolExe = @"C:\tools\win-x64\kiota-gen.exe");

		task.ExposedGenerateFullPathToTool().Should().Be(@"C:\tools\win-x64\kiota-gen.exe",
			"self-contained mode should return the full EXE path");
	}

	[TestMethod]
	public void GenerateFullPathToTool_WhenNeitherSet_LogsErrorAndReturnsEmpty()
	{
		var engine = new MockBuildEngine();
		var task = CreateTask(engine: engine);

		var result = task.ExposedGenerateFullPathToTool();

		result.Should().BeEmpty("when no tool path is set, should return empty");
		engine.Errors.Should().ContainSingle(e => e.Code == "KIOTA003",
			"should log KIOTA003 error when tool cannot be resolved");
	}

	// ==================================================================
	// Parameter validation
	// ==================================================================

	[TestMethod]
	public void ValidateParameters_AllRequired_ReturnsTrue()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = @"C:\specs\petstore.json";
			t.OutputDirectory = @"C:\out\generated";
		});

		task.ExposedValidateParameters().Should().BeTrue(
			"validation should pass when all required parameters are set");
	}

	[TestMethod]
	public void ValidateParameters_MissingOpenApi_ReturnsFalse()
	{
		var engine = new MockBuildEngine();
		var task = CreateTask(engine: engine, configure: t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = "";
			t.OutputDirectory = @"C:\out\generated";
		});

		task.ExposedValidateParameters().Should().BeFalse(
			"validation should fail when OpenApi is empty");
		engine.Errors.Should().ContainSingle(e => e.Code == "KIOTA004",
			"should log KIOTA004 for missing OpenApi");
	}

	[TestMethod]
	public void ValidateParameters_NullOpenApi_ReturnsFalse()
	{
		var engine = new MockBuildEngine();
		var task = CreateTask(engine: engine, configure: t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = null!;
			t.OutputDirectory = @"C:\out\generated";
		});

		task.ExposedValidateParameters().Should().BeFalse(
			"validation should fail when OpenApi is null");
		engine.Errors.Should().ContainSingle(e => e.Code == "KIOTA004");
	}

	[TestMethod]
	public void ValidateParameters_MissingOutputDirectory_ReturnsFalse()
	{
		var engine = new MockBuildEngine();
		var task = CreateTask(engine: engine, configure: t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = @"C:\specs\petstore.json";
			t.OutputDirectory = "";
		});

		task.ExposedValidateParameters().Should().BeFalse(
			"validation should fail when OutputDirectory is empty");
		engine.Errors.Should().ContainSingle(e => e.Code == "KIOTA005",
			"should log KIOTA005 for missing OutputDirectory");
	}

	[TestMethod]
	public void ValidateParameters_MissingToolPaths_ReturnsFalse()
	{
		var engine = new MockBuildEngine();
		var task = CreateTask(engine: engine, configure: t =>
		{
			t.OpenApi = @"C:\specs\petstore.json";
			t.OutputDirectory = @"C:\out\generated";
			// Neither KiotaToolDll nor KiotaToolExe set
		});

		task.ExposedValidateParameters().Should().BeFalse(
			"validation should fail when neither tool DLL nor EXE is set");
		engine.Errors.Should().ContainSingle(e => e.Code == "KIOTA003",
			"should log KIOTA003 for missing tool paths");
	}

	[TestMethod]
	public void ValidateParameters_WithExeInsteadOfDll_ReturnsTrue()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolExe = @"C:\tools\win-x64\kiota-gen.exe";
			t.OpenApi = @"C:\specs\petstore.json";
			t.OutputDirectory = @"C:\out\generated";
		});

		task.ExposedValidateParameters().Should().BeTrue(
			"validation should pass when KiotaToolExe is set instead of KiotaToolDll");
	}

	// ==================================================================
	// Command line generation — framework-dependent mode
	// ==================================================================

	[TestMethod]
	public void GenerateCommandLine_DllMode_StartsWithExecDll()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = @"C:\specs\petstore.json";
			t.OutputDirectory = @"C:\out";
		});

		var cmdLine = task.ExposedGenerateCommandLineCommands();

		cmdLine.Should().StartWith("exec",
			"framework-dependent mode should begin with 'exec'");
		cmdLine.Should().Contain(@"C:\tools\kiota-gen.dll",
			"should include the DLL path after 'exec'");
	}

	[TestMethod]
	public void GenerateCommandLine_ExeMode_OmitsExecPrefix()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolExe = @"C:\tools\kiota-gen.exe";
			t.OpenApi = @"C:\specs\petstore.json";
			t.OutputDirectory = @"C:\out";
		});

		var cmdLine = task.ExposedGenerateCommandLineCommands();

		cmdLine.Should().NotStartWith("exec",
			"self-contained mode should not use 'exec'");
		cmdLine.Should().Contain("--openapi",
			"should include the --openapi argument");
	}

	[TestMethod]
	public void GenerateCommandLine_RequiredArgs_AlwaysPresent()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = @"C:\specs\petstore.json";
			t.OutputDirectory = @"C:\out\generated";
		});

		var cmdLine = task.ExposedGenerateCommandLineCommands();

		cmdLine.Should().Contain("--openapi");
		cmdLine.Should().Contain(@"C:\specs\petstore.json");
		cmdLine.Should().Contain("--output");
		cmdLine.Should().Contain(@"C:\out\generated");
	}

	[TestMethod]
	public void GenerateCommandLine_DefaultNaming_IncludesDefaults()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = "spec.json";
			t.OutputDirectory = "/out";
		});

		var cmdLine = task.ExposedGenerateCommandLineCommands();

		cmdLine.Should().Contain("--class-name ApiClient",
			"default class name should be ApiClient");
		cmdLine.Should().Contain("--namespace ApiSdk",
			"default namespace should be ApiSdk");
	}

	[TestMethod]
	public void GenerateCommandLine_CustomNaming_IncludesCustomValues()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = "spec.json";
			t.OutputDirectory = "/out";
			t.ClientClassName = "PetStoreClient";
			t.Namespace = "MyApp.PetStore";
		});

		var cmdLine = task.ExposedGenerateCommandLineCommands();

		cmdLine.Should().Contain("--class-name PetStoreClient");
		cmdLine.Should().Contain("--namespace MyApp.PetStore");
	}

	[TestMethod]
	public void GenerateCommandLine_BooleanFlags_DefaultValues()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = "spec.json";
			t.OutputDirectory = "/out";
		});

		var cmdLine = task.ExposedGenerateCommandLineCommands();

		// Default boolean values
		cmdLine.Should().Contain("--uses-backing-store false",
			"default UsesBackingStore should be false");
		cmdLine.Should().Contain("--include-additional-data true",
			"default IncludeAdditionalData should be true");
		cmdLine.Should().Contain("--exclude-backward-compatible false",
			"default ExcludeBackwardCompatible should be false");
		cmdLine.Should().Contain("--clean-output false",
			"default CleanOutput should be false");
	}

	[TestMethod]
	public void GenerateCommandLine_BooleanFlags_SetToTrue()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = "spec.json";
			t.OutputDirectory = "/out";
			t.UsesBackingStore = true;
			t.IncludeAdditionalData = true;
			t.ExcludeBackwardCompatible = true;
			t.CleanOutput = true;
		});

		var cmdLine = task.ExposedGenerateCommandLineCommands();

		cmdLine.Should().Contain("--uses-backing-store true");
		cmdLine.Should().Contain("--include-additional-data true");
		cmdLine.Should().Contain("--exclude-backward-compatible true");
		cmdLine.Should().Contain("--clean-output true");
	}

	[TestMethod]
	public void GenerateCommandLine_TypeAccessModifier_IncludedCorrectly()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = "spec.json";
			t.OutputDirectory = "/out";
			t.TypeAccessModifier = "Internal";
		});

		var cmdLine = task.ExposedGenerateCommandLineCommands();

		cmdLine.Should().Contain("--type-access-modifier Internal");
	}

	[TestMethod]
	public void GenerateCommandLine_EmptyOptionalArrays_NotIncluded()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = "spec.json";
			t.OutputDirectory = "/out";
			// Leave all optional array properties as empty strings (default)
		});

		var cmdLine = task.ExposedGenerateCommandLineCommands();

		cmdLine.Should().NotContain("--include-patterns",
			"empty IncludePatterns should not produce a CLI argument");
		cmdLine.Should().NotContain("--exclude-patterns",
			"empty ExcludePatterns should not produce a CLI argument");
		cmdLine.Should().NotContain("--serializers",
			"empty Serializers should not produce a CLI argument");
		cmdLine.Should().NotContain("--deserializers",
			"empty Deserializers should not produce a CLI argument");
		cmdLine.Should().NotContain("--structured-mime-types",
			"empty StructuredMimeTypes should not produce a CLI argument");
		cmdLine.Should().NotContain("--disable-validation-rules",
			"empty DisableValidationRules should not produce a CLI argument");
	}

	[TestMethod]
	public void GenerateCommandLine_PopulatedOptionalArrays_Included()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = "spec.json";
			t.OutputDirectory = "/out";
			t.IncludePatterns = "/pets/**;/owners/**";
			t.ExcludePatterns = "/admin/**";
			t.Serializers = "Ns.JsonWriter;Ns.XmlWriter";
			t.Deserializers = "Ns.JsonParser";
			t.StructuredMimeTypes = "application/json;application/xml";
			t.DisableValidationRules = "NoServerEntry";
		});

		var cmdLine = task.ExposedGenerateCommandLineCommands();

		cmdLine.Should().Contain("--include-patterns");
		cmdLine.Should().Contain("/pets/**;/owners/**");
		cmdLine.Should().Contain("--exclude-patterns");
		cmdLine.Should().Contain("/admin/**");
		cmdLine.Should().Contain("--serializers");
		cmdLine.Should().Contain("Ns.JsonWriter;Ns.XmlWriter");
		cmdLine.Should().Contain("--deserializers");
		cmdLine.Should().Contain("Ns.JsonParser");
		cmdLine.Should().Contain("--structured-mime-types");
		cmdLine.Should().Contain("application/json;application/xml");
		cmdLine.Should().Contain("--disable-validation-rules");
		cmdLine.Should().Contain("NoServerEntry");
	}

	[TestMethod]
	public void GenerateCommandLine_LogLevel_IncludedCorrectly()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = "spec.json";
			t.OutputDirectory = "/out";
			t.KiotaLogLevel = "Debug";
		});

		var cmdLine = task.ExposedGenerateCommandLineCommands();

		cmdLine.Should().Contain("--log-level Debug");
	}

	[TestMethod]
	public void GenerateCommandLine_DefaultLogLevel_IsWarning()
	{
		var task = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = "spec.json";
			t.OutputDirectory = "/out";
		});

		var cmdLine = task.ExposedGenerateCommandLineCommands();

		cmdLine.Should().Contain("--log-level Warning",
			"default log level should be Warning");
	}

	// ==================================================================
	// Diagnostic output parsing — LogEventsFromTextOutput
	// ==================================================================

	[TestMethod]
	public void LogEvents_ErrorDiagnostic_ParsedAsMsBuildError()
	{
		var engine = new MockBuildEngine();
		var task = CreateTask(engine: engine, configure: t =>
		{
			t.OpenApi = "spec.json";
		});

		task.ExposedLogEventsFromTextOutput(
			"error KIOTA001: The OpenAPI document is invalid.",
			MessageImportance.High);

		engine.Errors.Should().ContainSingle(e =>
			e.Code == "KIOTA001" &&
			e.Message == "The OpenAPI document is invalid.",
			"structured error lines should be parsed into MSBuild errors with correct code and message");
	}

	[TestMethod]
	public void LogEvents_WarningDiagnostic_ParsedAsMsBuildWarning()
	{
		var engine = new MockBuildEngine();
		var task = CreateTask(engine: engine, configure: t =>
		{
			t.OpenApi = "spec.json";
		});

		task.ExposedLogEventsFromTextOutput(
			"warning KIOTA002: Deprecated operation found.",
			MessageImportance.High);

		engine.Warnings.Should().ContainSingle(w =>
			w.Code == "KIOTA002" &&
			w.Message == "Deprecated operation found.",
			"structured warning lines should be parsed into MSBuild warnings");
	}

	[TestMethod]
	public void LogEvents_ErrorDiagnostic_IncludesOpenApiFileReference()
	{
		var engine = new MockBuildEngine();
		var task = CreateTask(engine: engine, configure: t =>
		{
			t.OpenApi = @"C:\specs\petstore.json";
		});

		task.ExposedLogEventsFromTextOutput(
			"error KIOTA010: Schema validation failed.",
			MessageImportance.High);

		engine.Errors.Should().ContainSingle();
		engine.Errors[0].File.Should().Be(@"C:\specs\petstore.json",
			"error diagnostics should reference the OpenAPI file path");
	}

	[TestMethod]
	public void LogEvents_CaseInsensitive_ParsesUppercaseSeverity()
	{
		var engine = new MockBuildEngine();
		var task = CreateTask(engine: engine, configure: t =>
		{
			t.OpenApi = "spec.json";
		});

		task.ExposedLogEventsFromTextOutput(
			"ERROR KIOTA001: Something failed.",
			MessageImportance.High);

		engine.Errors.Should().ContainSingle(e => e.Code == "KIOTA001",
			"severity parsing should be case-insensitive");
	}

	[TestMethod]
	public void LogEvents_CaseInsensitive_ParsesMixedCaseSeverity()
	{
		var engine = new MockBuildEngine();
		var task = CreateTask(engine: engine, configure: t =>
		{
			t.OpenApi = "spec.json";
		});

		task.ExposedLogEventsFromTextOutput(
			"Warning KIOTA002: Low priority issue.",
			MessageImportance.High);

		engine.Warnings.Should().ContainSingle(w => w.Code == "KIOTA002");
	}

	[TestMethod]
	public void LogEvents_UnstructuredText_PassedThrough()
	{
		var engine = new MockBuildEngine();
		var task = CreateTask(engine: engine, configure: t =>
		{
			t.OpenApi = "spec.json";
		});

		task.ExposedLogEventsFromTextOutput(
			"Generating client code for petstore.json...",
			MessageImportance.High);

		// Unstructured lines should not be parsed as errors or warnings
		engine.Errors.Should().BeEmpty();
		engine.Warnings.Should().BeEmpty();
	}

	[TestMethod]
	public void LogEvents_EmptyLine_Ignored()
	{
		var engine = new MockBuildEngine();
		var task = CreateTask(engine: engine, configure: t =>
		{
			t.OpenApi = "spec.json";
		});

		task.ExposedLogEventsFromTextOutput("", MessageImportance.High);
		task.ExposedLogEventsFromTextOutput("   ", MessageImportance.High);

		engine.Errors.Should().BeEmpty();
		engine.Warnings.Should().BeEmpty();
	}

	[TestMethod]
	public void LogEvents_MultipleDiagnostics_AllParsed()
	{
		var engine = new MockBuildEngine();
		var task = CreateTask(engine: engine, configure: t =>
		{
			t.OpenApi = "spec.json";
		});

		task.ExposedLogEventsFromTextOutput("error KIOTA001: First error.", MessageImportance.High);
		task.ExposedLogEventsFromTextOutput("warning KIOTA002: A warning.", MessageImportance.High);
		task.ExposedLogEventsFromTextOutput("error KIOTA003: Second error.", MessageImportance.High);

		engine.Errors.Should().HaveCount(2);
		engine.Warnings.Should().HaveCount(1);
	}

	// ==================================================================
	// Default property values
	// ==================================================================

	[TestMethod]
	public void DefaultProperties_HaveExpectedValues()
	{
		var task = new KiotaGenerateTask();

		task.ClientClassName.Should().Be("ApiClient");
		task.Namespace.Should().Be("ApiSdk");
		task.UsesBackingStore.Should().BeFalse();
		task.IncludeAdditionalData.Should().BeTrue();
		task.ExcludeBackwardCompatible.Should().BeFalse();
		task.TypeAccessModifier.Should().Be("Public");
		task.CleanOutput.Should().BeFalse();
		task.KiotaLogLevel.Should().Be("Warning");
		task.IncludePatterns.Should().BeEmpty();
		task.ExcludePatterns.Should().BeEmpty();
		task.Serializers.Should().BeEmpty();
		task.Deserializers.Should().BeEmpty();
		task.StructuredMimeTypes.Should().BeEmpty();
		task.DisableValidationRules.Should().BeEmpty();
	}

	[TestMethod]
	public void GeneratedFiles_DefaultsToEmpty()
	{
		var task = new KiotaGenerateTask();

		task.GeneratedFiles.Should().NotBeNull();
		task.GeneratedFiles.Should().BeEmpty();
	}

	// ==================================================================
	// Multiple KiotaOpenApiReference items scenario
	// (verifies task can be configured with different values per call)
	// ==================================================================

	[TestMethod]
	public void MultipleReferences_EachProducesDifferentCommandLine()
	{
		// Simulate what happens when MSBuild batches over multiple items:
		// the task is configured differently for each item.
		var task1 = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = @"C:\specs\petstore.json";
			t.OutputDirectory = @"C:\out\PetStoreClient";
			t.ClientClassName = "PetStoreClient";
			t.Namespace = "MyApp.PetStore";
		});

		var task2 = CreateTask(t =>
		{
			t.KiotaToolDll = @"C:\tools\kiota-gen.dll";
			t.OpenApi = @"C:\specs\weather.json";
			t.OutputDirectory = @"C:\out\WeatherClient";
			t.ClientClassName = "WeatherClient";
			t.Namespace = "MyApp.Weather";
		});

		var cmdLine1 = task1.ExposedGenerateCommandLineCommands();
		var cmdLine2 = task2.ExposedGenerateCommandLineCommands();

		cmdLine1.Should().Contain("petstore.json");
		cmdLine1.Should().Contain("PetStoreClient");
		cmdLine1.Should().Contain("MyApp.PetStore");

		cmdLine2.Should().Contain("weather.json");
		cmdLine2.Should().Contain("WeatherClient");
		cmdLine2.Should().Contain("MyApp.Weather");

		cmdLine1.Should().NotBe(cmdLine2,
			"different KiotaOpenApiReference items should produce different command lines");
	}

	// ==================================================================
	// Generated files collection (via temp directory)
	// ==================================================================

	private string _tempDir = null!;

	[TestInitialize]
	public void TestInit()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), "MsBuildTaskTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempDir);
	}

	[TestCleanup]
	public void TestCleanup()
	{
		try
		{
			if (Directory.Exists(_tempDir))
			{
				Directory.Delete(_tempDir, recursive: true);
			}
		}
		catch
		{
			// Best-effort cleanup
		}
	}

	[TestMethod]
	public void OutputDirectory_Property_CanBeSetAndRetrieved()
	{
		var task = new KiotaGenerateTask
		{
			OutputDirectory = @"C:\my\output"
		};

		task.OutputDirectory.Should().Be(@"C:\my\output");
	}

	// ==================================================================
	// Buildtransitive props/targets file structure validation
	// ==================================================================

	[TestMethod]
	public void PropsFile_Exists()
	{
		var propsPath = FindBuildTransitiveFile("Uno.Extensions.Http.Kiota.Generator.props");

		File.Exists(propsPath).Should().BeTrue(
			$"buildTransitive .props file should exist at {propsPath}");
	}

	[TestMethod]
	public void TargetsFile_Exists()
	{
		var targetsPath = FindBuildTransitiveFile("Uno.Extensions.Http.Kiota.Generator.targets");

		File.Exists(targetsPath).Should().BeTrue(
			$"buildTransitive .targets file should exist at {targetsPath}");
	}

	[TestMethod]
	public void PropsFile_DefinesKiotaGeneratorEnabled()
	{
		var content = ReadBuildTransitiveFile("Uno.Extensions.Http.Kiota.Generator.props");

		content.Should().Contain("KiotaGeneratorEnabled",
			"props file should define KiotaGeneratorEnabled property");
	}

	[TestMethod]
	public void PropsFile_DefinesKiotaOutputPath()
	{
		var content = ReadBuildTransitiveFile("Uno.Extensions.Http.Kiota.Generator.props");

		content.Should().Contain("KiotaOutputPath",
			"props file should define KiotaOutputPath property");
		content.Should().Contain("IntermediateOutputPath",
			"default KiotaOutputPath should reference IntermediateOutputPath");
	}

	[TestMethod]
	public void PropsFile_DefinesItemDefinitionGroup()
	{
		var content = ReadBuildTransitiveFile("Uno.Extensions.Http.Kiota.Generator.props");

		content.Should().Contain("ItemDefinitionGroup",
			"props file should define ItemDefinitionGroup for KiotaOpenApiReference defaults");
		content.Should().Contain("KiotaOpenApiReference",
			"props file should define defaults for KiotaOpenApiReference items");
		content.Should().Contain("ClientClassName",
			"ItemDefinitionGroup should include ClientClassName default");
		content.Should().Contain("ApiClient",
			"default ClientClassName should be ApiClient");
	}

	[TestMethod]
	public void TargetsFile_DefinesKiotaGenerateTarget()
	{
		var content = ReadBuildTransitiveFile("Uno.Extensions.Http.Kiota.Generator.targets");

		content.Should().Contain("_KiotaGenerate",
			"targets file should define the _KiotaGenerate target");
		content.Should().Contain("CoreCompileDependsOn",
			"_KiotaGenerate should be hooked into CoreCompileDependsOn");
	}

	[TestMethod]
	public void TargetsFile_DefinesKiotaIncludeGeneratedTarget()
	{
		var content = ReadBuildTransitiveFile("Uno.Extensions.Http.Kiota.Generator.targets");

		content.Should().Contain("_KiotaIncludeGenerated",
			"targets file should define _KiotaIncludeGenerated target");
		content.Should().Contain("Compile",
			"_KiotaIncludeGenerated should add files to the Compile item group");
	}

	[TestMethod]
	public void TargetsFile_DefinesKiotaCleanTarget()
	{
		var content = ReadBuildTransitiveFile("Uno.Extensions.Http.Kiota.Generator.targets");

		content.Should().Contain("_KiotaClean",
			"targets file should define _KiotaClean target");
		content.Should().Contain("RemoveDir",
			"_KiotaClean should remove the output directory");
	}

	[TestMethod]
	public void TargetsFile_SupportsIncrementalBuild()
	{
		var content = ReadBuildTransitiveFile("Uno.Extensions.Http.Kiota.Generator.targets");

		content.Should().Contain("Inputs=",
			"_KiotaGenerate target should declare Inputs for incremental build");
		content.Should().Contain("Outputs=",
			"_KiotaGenerate target should declare Outputs for incremental build");
		content.Should().Contain(".stamp",
			"incremental build should use stamp files");
	}

	[TestMethod]
	public void TargetsFile_RegistersUsingTask()
	{
		var content = ReadBuildTransitiveFile("Uno.Extensions.Http.Kiota.Generator.targets");

		content.Should().Contain("UsingTask",
			"targets file should register the KiotaGenerateTask via UsingTask");
		content.Should().Contain("Uno.Extensions.Http.Kiota.Generator.Tasks.KiotaGenerateTask",
			"UsingTask should reference the full task type name");
	}

	[TestMethod]
	public void TargetsFile_SupportsFrameworkDependentAndRidSpecificToolResolution()
	{
		var content = ReadBuildTransitiveFile("Uno.Extensions.Http.Kiota.Generator.targets");

		content.Should().Contain("_KiotaToolDll",
			"targets file should resolve framework-dependent tool DLL");
		content.Should().Contain("_KiotaToolExe",
			"targets file should resolve RID-specific tool EXE");
		content.Should().Contain("net9.0",
			"should prefer net9.0 tool");
		content.Should().Contain("net8.0",
			"should fallback to net8.0 tool");
	}

	// ==================================================================
	// Task project configuration validation
	// ==================================================================

	[TestMethod]
	public void TaskCsproj_TargetsNetstandard20()
	{
		var csproj = ReadTaskProjectFile();

		csproj.Should().Contain("netstandard2.0",
			"MSBuild task project should target netstandard2.0 for compatibility");
	}

	[TestMethod]
	public void TaskCsproj_IsDevelopmentDependency()
	{
		var csproj = ReadTaskProjectFile();

		csproj.Should().Contain("<DevelopmentDependency>true</DevelopmentDependency>",
			"NuGet package should be marked as a development dependency");
	}

	[TestMethod]
	public void TaskCsproj_SuppressesDependenciesWhenPacking()
	{
		var csproj = ReadTaskProjectFile();

		csproj.Should().Contain("<SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>",
			"NuGet package should suppress runtime dependencies");
	}

	[TestMethod]
	public void TaskCsproj_ReferencesMsBuildFramework()
	{
		var csproj = ReadTaskProjectFile();

		csproj.Should().Contain("Microsoft.Build.Framework",
			"task project should reference Microsoft.Build.Framework");
		csproj.Should().Contain("Microsoft.Build.Utilities.Core",
			"task project should reference Microsoft.Build.Utilities.Core");
	}

	[TestMethod]
	public void TaskCsproj_PacksBuildTransitiveContent()
	{
		var csproj = ReadTaskProjectFile();

		csproj.Should().Contain("buildTransitive",
			"task project should pack buildTransitive content");
	}

	// ==================================================================
	// Helper infrastructure
	// ==================================================================

	/// <summary>
	/// Creates a <see cref="TestableKiotaGenerateTask"/> with a mock build engine,
	/// optionally applying additional configuration.
	/// </summary>
	private static TestableKiotaGenerateTask CreateTask(
		Action<KiotaGenerateTask>? configure = null,
		MockBuildEngine? engine = null)
	{
		engine ??= new MockBuildEngine();
		var task = new TestableKiotaGenerateTask
		{
			BuildEngine = engine,
		};
		configure?.Invoke(task);
		return task;
	}

	/// <summary>
	/// Finds the buildTransitive file by walking up from the test output directory.
	/// </summary>
	private static string FindBuildTransitiveFile(string fileName)
	{
		var baseDir = AppContext.BaseDirectory
			.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		// Navigate up to the src/ directory from test output
		var srcDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));

		return Path.Combine(srcDir,
			"Uno.Extensions.Http.Kiota.Generator.Tasks",
			"buildTransitive",
			fileName);
	}

	/// <summary>
	/// Reads the content of a buildTransitive file.
	/// </summary>
	private static string ReadBuildTransitiveFile(string fileName)
	{
		var path = FindBuildTransitiveFile(fileName);
		File.Exists(path).Should().BeTrue($"file should exist: {path}");
		return File.ReadAllText(path);
	}

	/// <summary>
	/// Reads the content of the Generator.Tasks .csproj file.
	/// </summary>
	private static string ReadTaskProjectFile()
	{
		var baseDir = AppContext.BaseDirectory
			.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		var srcDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));

		var csprojPath = Path.Combine(srcDir,
			"Uno.Extensions.Http.Kiota.Generator.Tasks",
			"Uno.Extensions.Http.Kiota.Generator.Tasks.csproj");

		File.Exists(csprojPath).Should().BeTrue($"csproj should exist: {csprojPath}");
		return File.ReadAllText(csprojPath);
	}

	// ==================================================================
	// Internal types
	// ==================================================================

	/// <summary>
	/// Test wrapper for <see cref="KiotaGenerateTask"/> that exposes
	/// protected members for unit testing.
	/// </summary>
	private sealed class TestableKiotaGenerateTask : KiotaGenerateTask
	{
		public string ExposedToolName => ToolName;

		public string ExposedGenerateFullPathToTool() =>
			GenerateFullPathToTool();

		public string ExposedGenerateCommandLineCommands() =>
			GenerateCommandLineCommands();

		public bool ExposedValidateParameters() =>
			ValidateParameters();

		public void ExposedLogEventsFromTextOutput(string singleLine, MessageImportance importance) =>
			LogEventsFromTextOutput(singleLine, importance);
	}

	/// <summary>
	/// A minimal <see cref="IBuildEngine"/> implementation that captures
	/// MSBuild errors, warnings, and messages for assertion.
	/// </summary>
	private sealed class MockBuildEngine : IBuildEngine
	{
		public List<BuildDiagnostic> Errors { get; } = new();
		public List<BuildDiagnostic> Warnings { get; } = new();
		public List<string> Messages { get; } = new();

		public bool ContinueOnError => false;
		public int LineNumberOfTaskNode => 0;
		public int ColumnNumberOfTaskNode => 0;
		public string ProjectFileOfTaskNode => "test.csproj";

		public void LogErrorEvent(BuildErrorEventArgs e)
		{
			Errors.Add(new BuildDiagnostic(e.Code ?? "", e.Message ?? "", e.File ?? ""));
		}

		public void LogWarningEvent(BuildWarningEventArgs e)
		{
			Warnings.Add(new BuildDiagnostic(e.Code ?? "", e.Message ?? "", e.File ?? ""));
		}

		public void LogMessageEvent(BuildMessageEventArgs e)
		{
			Messages.Add(e.Message ?? "");
		}

		public void LogCustomEvent(CustomBuildEventArgs e) { }

		public bool BuildProjectFile(
			string projectFileName,
			string[] targetNames,
			IDictionary globalProperties,
			IDictionary targetOutputs) => true;
	}

	/// <summary>
	/// Captures a structured MSBuild diagnostic (error or warning).
	/// </summary>
	private sealed record BuildDiagnostic(string Code, string Message, string File);
}
