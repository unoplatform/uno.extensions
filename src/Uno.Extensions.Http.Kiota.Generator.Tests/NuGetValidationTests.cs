using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Http.Kiota.Generator.Tasks;
using Uno.Extensions.Http.Kiota.SourceGenerator;

namespace Uno.Extensions.Http.Kiota.Generator.Tests;

/// <summary>
/// End-to-end NuGet validation tests that simulate how consumers use the
/// Kiota code-generation NuGet packages.
/// <para>
/// These tests verify both consumption paths:
/// <list type="bullet">
///   <item><b>Phase 1</b> — <c>Uno.Extensions.Http.Kiota.Generator</c>
///     (MSBuild task: <c>&lt;KiotaOpenApiReference&gt;</c>)</item>
///   <item><b>Phase 2</b> — <c>Uno.Extensions.Http.Kiota</c>
///     (source generator: <c>&lt;AdditionalFiles KiotaClientName="..."&gt;</c>)</item>
/// </list>
/// </para>
/// <para>
/// Covered scenarios:
/// <list type="number">
///   <item>Package structure: all expected artefacts exist in build output.</item>
///   <item>Source generator full pipeline: every test spec produces valid C#.</item>
///   <item>MSBuild task configuration: properties flow correctly to the CLI.</item>
///   <item>BuildTransitive props well-formedness: valid XML with expected MSBuild elements.</item>
///   <item>Cross-path compatibility: both paths accept the same configuration surface.</item>
///   <item>Generated code quality: auto-generated header, namespace, class name, attributes.</item>
/// </list>
/// </para>
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class NuGetValidationTests
{
	// ------------------------------------------------------------------
	// Paths
	// ------------------------------------------------------------------

	private static string TestDataDir =>
		Path.Combine(AppContext.BaseDirectory, "TestData");

	/// <summary>Root <c>src/</c> directory, resolved from test output.</summary>
	private static string SrcDir
	{
		get
		{
			var baseDir = AppContext.BaseDirectory
				.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
		}
	}

	// ==================================================================
	// 1. Package Structure Validation
	// ==================================================================

	[TestMethod]
	public void SourceGenerator_ProjectOutput_ContainsGeneratorDll()
	{
		// The source generator DLL should be present in the build output
		// so the host package can pack it into analyzers/dotnet/cs/.
		var genOutputDir = Path.Combine(SrcDir,
			"Uno.Extensions.Http.Kiota.SourceGenerator", "bin");

		Directory.Exists(genOutputDir).Should().BeTrue(
			$"source generator build output directory should exist: {genOutputDir}");

		// Find the generator DLL in any configuration/TFM subpath
		var generatorDlls = Directory.GetFiles(
			genOutputDir,
			"Uno.Extensions.Http.Kiota.SourceGenerator.dll",
			SearchOption.AllDirectories);

		generatorDlls.Should().NotBeEmpty(
			"the source generator DLL should exist in the build output");
	}

	[TestMethod]
	public void SourceGenerator_ProjectOutput_ContainsDependencyDlls()
	{
		// CopyLocalLockFileAssemblies=true means dependency DLLs appear
		// alongside the generator DLL for NuGet packaging.
		var genOutputDir = Path.Combine(SrcDir,
			"Uno.Extensions.Http.Kiota.SourceGenerator", "bin");

		if (!Directory.Exists(genOutputDir))
		{
			Assert.Inconclusive(
				"Source generator build output not found. Build the project first.");
			return;
		}

		var expectedDeps = new[] { "Microsoft.OpenApi.dll", "SharpYaml.dll", "DotNet.Glob.dll" };

		foreach (var dep in expectedDeps)
		{
			var found = Directory.GetFiles(genOutputDir, dep, SearchOption.AllDirectories);
			found.Should().NotBeEmpty(
				$"dependency DLL '{dep}' should be copied to build output " +
				"(CopyLocalLockFileAssemblies=true)");
		}
	}

	[TestMethod]
	public void HostPackage_CsProj_BundlesGeneratorInAnalyzersPath()
	{
		// Verify the host .csproj (Uno.Extensions.Http.Kiota) packs the
		// generator and its deps into analyzers/dotnet/cs/.
		var csprojPath = Path.Combine(SrcDir,
			"Uno.Extensions.Http.Kiota",
			"Uno.Extensions.Http.Kiota.csproj");

		File.Exists(csprojPath).Should().BeTrue(
			$"host package csproj should exist: {csprojPath}");

		var content = File.ReadAllText(csprojPath);

		content.Should().Contain("analyzers/dotnet/cs",
			"the host package should bundle the generator in analyzers/dotnet/cs");
		content.Should().Contain("Uno.Extensions.Http.Kiota.SourceGenerator.dll",
			"the host package should include the source generator DLL");
		content.Should().Contain("Microsoft.OpenApi.dll",
			"the host package should include the Microsoft.OpenApi dependency DLL");
		content.Should().Contain("SharpYaml.dll",
			"the host package should include the SharpYaml dependency DLL");
		content.Should().Contain("DotNet.Glob.dll",
			"the host package should include the DotNet.Glob dependency DLL");
	}

	[TestMethod]
	public void HostPackage_CsProj_IncludesBuildTransitiveProps()
	{
		// The host package should include the source generator's
		// buildTransitive props for CompilerVisibleProperty declarations.
		var csprojPath = Path.Combine(SrcDir,
			"Uno.Extensions.Http.Kiota",
			"Uno.Extensions.Http.Kiota.csproj");

		var content = File.ReadAllText(csprojPath);

		content.Should().Contain("buildTransitive",
			"the host package should include buildTransitive props");
		content.Should().Contain("Uno.Extensions.Http.Kiota.SourceGenerator.props",
			"the host package should include the source generator props file");
	}

	[TestMethod]
	public void TaskPackage_CsProj_PacksCliToolAndBuildTransitive()
	{
		// Verify the task package csproj includes CLI tool binaries
		// and buildTransitive files.
		var csprojPath = Path.Combine(SrcDir,
			"Uno.Extensions.Http.Kiota.Generator.Tasks",
			"Uno.Extensions.Http.Kiota.Generator.Tasks.csproj");

		File.Exists(csprojPath).Should().BeTrue(
			$"task package csproj should exist: {csprojPath}");

		var content = File.ReadAllText(csprojPath);

		content.Should().Contain("DevelopmentDependency",
			"the task package should be a DevelopmentDependency");
		content.Should().Contain("buildTransitive",
			"the task package should include buildTransitive files");
		content.Should().Contain("_PublishKiotaCli",
			"the task package should have a CLI publish target");
		content.Should().Contain("tools/net8.0",
			"the task package should pack CLI for net8.0");
		content.Should().Contain("tools/net9.0",
			"the task package should pack CLI for net9.0");
	}

	// ==================================================================
	// 2. BuildTransitive Props Well-Formedness
	// ==================================================================

	[TestMethod]
	public void SourceGeneratorProps_IsValidXml_WithExpectedElements()
	{
		var propsPath = Path.Combine(SrcDir,
			"Uno.Extensions.Http.Kiota.SourceGenerator",
			"buildTransitive",
			"Uno.Extensions.Http.Kiota.SourceGenerator.props");

		File.Exists(propsPath).Should().BeTrue($"props file should exist: {propsPath}");

		var doc = XDocument.Load(propsPath);
		doc.Root.Should().NotBeNull("props file should have a root element");

		var ns = doc.Root!.Name.Namespace;

		// Should declare CompilerVisibleProperty items
		var compilerVisibleProps = doc.Descendants(ns + "CompilerVisibleProperty").ToList();
		compilerVisibleProps.Should().NotBeEmpty(
			"the props should declare CompilerVisibleProperty items " +
			"for MSBuild→source generator configuration flow");

		// Must include KiotaGenerator_Enabled
		compilerVisibleProps.Select(e => e.Attribute("Include")?.Value)
			.Should().Contain("KiotaGenerator_Enabled",
			"KiotaGenerator_Enabled must be a CompilerVisibleProperty");

		// Should declare CompilerVisibleItemMetadata for AdditionalFiles
		var compilerVisibleMeta = doc.Descendants(ns + "CompilerVisibleItemMetadata").ToList();
		compilerVisibleMeta.Should().NotBeEmpty(
			"the props should declare CompilerVisibleItemMetadata for AdditionalFiles");

		compilerVisibleMeta.Select(e => e.Attribute("MetadataName")?.Value)
			.Should().Contain("KiotaClientName",
			"KiotaClientName must be a CompilerVisibleItemMetadata");

		compilerVisibleMeta.Select(e => e.Attribute("MetadataName")?.Value)
			.Should().Contain("KiotaNamespace",
			"KiotaNamespace must be a CompilerVisibleItemMetadata");
	}

	[TestMethod]
	public void TaskProps_IsValidXml_WithKiotaOpenApiReferenceDefaults()
	{
		var propsPath = Path.Combine(SrcDir,
			"Uno.Extensions.Http.Kiota.Generator.Tasks",
			"buildTransitive",
			"Uno.Extensions.Http.Kiota.Generator.props");

		File.Exists(propsPath).Should().BeTrue($"props file should exist: {propsPath}");

		var doc = XDocument.Load(propsPath);
		doc.Root.Should().NotBeNull("props file should have a root element");

		var content = File.ReadAllText(propsPath);

		// Should define KiotaGeneratorEnabled default
		content.Should().Contain("KiotaGeneratorEnabled",
			"task props should define a KiotaGeneratorEnabled property");

		// Should define ItemDefinitionGroup defaults for KiotaOpenApiReference
		content.Should().Contain("ItemDefinitionGroup",
			"task props should define ItemDefinitionGroup for defaults");
		content.Should().Contain("KiotaOpenApiReference",
			"task props should define defaults for KiotaOpenApiReference items");
		content.Should().Contain("ClientClassName",
			"task props should include ClientClassName default");
	}

	[TestMethod]
	public void TaskTargets_IsValidXml_WithKiotaGenerateTarget()
	{
		var targetsPath = Path.Combine(SrcDir,
			"Uno.Extensions.Http.Kiota.Generator.Tasks",
			"buildTransitive",
			"Uno.Extensions.Http.Kiota.Generator.targets");

		File.Exists(targetsPath).Should().BeTrue(
			$"targets file should exist: {targetsPath}");

		var doc = XDocument.Load(targetsPath);
		doc.Root.Should().NotBeNull("targets file should have a root element");

		var content = File.ReadAllText(targetsPath);

		// Should hook into CoreCompile
		content.Should().Contain("CoreCompileDependsOn",
			"targets should hook into CoreCompile for build integration");

		// Should define _KiotaGenerate target
		content.Should().Contain("_KiotaGenerate",
			"targets should define the _KiotaGenerate target");

		// Should include generated files in Compile
		content.Should().Contain("_KiotaIncludeGenerated",
			"targets should include generated files in the Compile item group");

		// Should support clean
		content.Should().Contain("_KiotaClean",
			"targets should define a clean target");

		// Should use the typed MSBuild task
		content.Should().Contain("KiotaGenerateTask",
			"targets should use the typed KiotaGenerateTask for structured error reporting");
	}

	// ==================================================================
	// 3. Source Generator (Phase 2) — Full Pipeline Validation
	// ==================================================================

	[TestMethod]
	[DataRow("petstore.json", "PetStoreClient", "TestApp.PetStore")]
	[DataRow("enums.json", "EnumClient", "TestApp.Enums")]
	[DataRow("inheritance.json", "InheritanceClient", "TestApp.Inheritance")]
	[DataRow("error-responses.json", "ErrorClient", "TestApp.Errors")]
	[DataRow("composed-types.json", "ComposedClient", "TestApp.Composed")]
	public void SourceGenerator_ProducesValidCSharp_ForAllTestSpecs(
		string specFile, string clientName, string namespaceName)
	{
		var specPath = Path.Combine(TestDataDir, specFile);
		if (!File.Exists(specPath))
		{
			Assert.Inconclusive($"Test spec not found: {specPath}");
			return;
		}

		var specContent = File.ReadAllText(specPath);

		var (outputCompilation, compilation, diagnostics) = RunSourceGenerator(
			specContent, specPath, clientName, namespaceName);

		// Generator should not report errors
		var generatorErrors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		generatorErrors.Should().BeEmpty(
			$"the source generator should not report errors for {specFile}");

		// At least one source file should be generated
		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().NotBeEmpty(
			$"the source generator should produce at least one C# file from {specFile}");

		// All generated trees should parse without syntax errors
		foreach (var tree in generatedTrees)
		{
			var syntaxDiags = tree.GetDiagnostics()
				.Where(d => d.Severity == DiagnosticSeverity.Error)
				.ToList();
			syntaxDiags.Should().BeEmpty(
				$"generated source from {specFile} should have no syntax errors: " +
				$"{tree.FilePath}");
		}
	}

	[TestMethod]
	public void SourceGenerator_GeneratedCode_HasAutoGeneratedHeader()
	{
		var (_, compilation, allSource) = RunPetstoreGenerator();

		allSource.Should().Contain("// <auto-generated/>",
			"generated code should have the auto-generated header comment");
	}

	[TestMethod]
	public void SourceGenerator_GeneratedCode_HasPragmaWarningDisable()
	{
		var (_, compilation, allSource) = RunPetstoreGenerator();

		allSource.Should().Contain("#pragma warning disable",
			"generated code should suppress warnings via pragma");
	}

	[TestMethod]
	public void SourceGenerator_GeneratedCode_HasGeneratedCodeAttribute()
	{
		var (_, compilation, allSource) = RunPetstoreGenerator();

		allSource.Should().Contain(
			"[global::System.CodeDom.Compiler.GeneratedCode(\"Kiota\"",
			"generated code should carry the [GeneratedCode] attribute");
	}

	[TestMethod]
	public void SourceGenerator_GeneratedCode_HasCorrectNamespace()
	{
		var (_, compilation, allSource) = RunPetstoreGenerator();

		allSource.Should().Contain("namespace TestApp.PetStore",
			"generated code should use the configured namespace");
	}

	[TestMethod]
	public void SourceGenerator_GeneratedCode_HasClientClass()
	{
		var (_, compilation, allSource) = RunPetstoreGenerator();

		allSource.Should().Contain("class PetStoreClient",
			"generated code should contain the configured client class name");
	}

	[TestMethod]
	public void SourceGenerator_GeneratedCode_HasModelClasses()
	{
		var (_, compilation, allSource) = RunPetstoreGenerator();

		allSource.Should().Contain("class Pet",
			"generated code should contain the Pet model from the petstore spec");
	}

	[TestMethod]
	public void SourceGenerator_GeneratedCode_HasIParsableInterface()
	{
		var (_, _, allSource) = RunPetstoreGenerator();

		allSource.Should().Contain("IParsable",
			"generated model classes should implement IParsable");
	}

	[TestMethod]
	public void SourceGenerator_GeneratedCode_HasIRequestAdapter()
	{
		var (_, _, allSource) = RunPetstoreGenerator();

		allSource.Should().Contain("IRequestAdapter",
			"generated client should reference IRequestAdapter");
	}

	[TestMethod]
	public void SourceGenerator_GeneratedCode_HasSerializationMethods()
	{
		var (_, _, allSource) = RunPetstoreGenerator();

		allSource.Should().Contain("Serialize",
			"generated models should have Serialize methods");
		allSource.Should().Contain("GetFieldDeserializers",
			"generated models should have GetFieldDeserializers methods");
	}

	[TestMethod]
	public void SourceGenerator_GeneratedCode_HasCreateFromDiscriminatorValue()
	{
		var (_, _, allSource) = RunPetstoreGenerator();

		allSource.Should().Contain("CreateFromDiscriminatorValue",
			"generated models should have the factory method");
	}

	[TestMethod]
	public void SourceGenerator_GeneratedCode_UsesGlobalTypeReferences()
	{
		var (_, _, allSource) = RunPetstoreGenerator();

		allSource.Should().Contain("global::",
			"generated code should use global:: prefix for type references to avoid conflicts");
	}

	[TestMethod]
	public void SourceGenerator_YamlSpec_ProducesOutput()
	{
		// Verify that YAML specs are also supported (not just JSON)
		var specPath = Path.Combine(TestDataDir, "petstore.yaml");
		if (!File.Exists(specPath))
		{
			Assert.Inconclusive("petstore.yaml not found in TestData");
			return;
		}

		var specContent = File.ReadAllText(specPath);

		var (outputCompilation, compilation, diagnostics) = RunSourceGenerator(
			specContent, specPath, "YamlClient", "TestApp.Yaml");

		var generatorErrors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		generatorErrors.Should().BeEmpty(
			"the source generator should not report errors for YAML specs");

		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().NotBeEmpty(
			"the source generator should produce output from YAML specs");
	}

	// ==================================================================
	// 4. MSBuild Task (Phase 1) — Configuration Flow Validation
	// ==================================================================

	[TestMethod]
	public void MsBuildTask_AcceptsAllKiotaOpenApiReferenceMetadata()
	{
		// This test verifies that the MSBuild task exposes properties
		// matching all the <KiotaOpenApiReference> metadata defined
		// in the buildTransitive .props file.
		var engine = new MockBuildEngine();
		var task = new TestableKiotaGenerateTask
		{
			BuildEngine = engine,
			KiotaToolDll = @"C:\tools\kiota-gen.dll",
			OpenApi = @"C:\specs\petstore.json",
			OutputDirectory = @"C:\out\generated",
			// All metadata-mapped properties
			ClientClassName = "PetStoreClient",
			Namespace = "MyApp.PetStore",
			UsesBackingStore = true,
			IncludeAdditionalData = true,
			ExcludeBackwardCompatible = false,
			TypeAccessModifier = "Public",
			IncludePatterns = "/pets/**;/store/**",
			ExcludePatterns = "/admin/**",
			Serializers = "Microsoft.Kiota.Serialization.Json.JsonSerializationWriterFactory",
			Deserializers = "Microsoft.Kiota.Serialization.Json.JsonParseNodeFactory",
			StructuredMimeTypes = "application/json;text/plain",
			CleanOutput = true,
			DisableValidationRules = "NoServerEntry",
		};

		// Validation should pass
		task.ExposedValidateParameters().Should().BeTrue(
			"the task should accept all KiotaOpenApiReference metadata properties");

		// Command line should contain all arguments
		var cmdLine = task.ExposedGenerateCommandLineCommands();

		cmdLine.Should().Contain("--openapi",
			"CLI command should include --openapi");
		cmdLine.Should().Contain("--output",
			"CLI command should include --output");
		cmdLine.Should().Contain("--class-name",
			"CLI command should include --class-name");
		cmdLine.Should().Contain("PetStoreClient",
			"CLI command should include the configured class name");
		cmdLine.Should().Contain("--namespace",
			"CLI command should include --namespace");
		cmdLine.Should().Contain("MyApp.PetStore",
			"CLI command should include the configured namespace");
		cmdLine.Should().Contain("--uses-backing-store",
			"CLI command should include --uses-backing-store");
		cmdLine.Should().Contain("--include-additional-data",
			"CLI command should include --include-additional-data");
		cmdLine.Should().Contain("--type-access-modifier",
			"CLI command should include --type-access-modifier");
		cmdLine.Should().Contain("--include-patterns",
			"CLI command should include --include-patterns when specified");
		cmdLine.Should().Contain("--exclude-patterns",
			"CLI command should include --exclude-patterns when specified");
		cmdLine.Should().Contain("--serializers",
			"CLI command should include --serializers when specified");
		cmdLine.Should().Contain("--deserializers",
			"CLI command should include --deserializers when specified");
		cmdLine.Should().Contain("--clean-output",
			"CLI command should include --clean-output");
	}

	[TestMethod]
	public void MsBuildTask_DefaultValues_MatchPropsFileDefaults()
	{
		// Verify that the default property values on KiotaGenerateTask
		// match the defaults defined in the buildTransitive .props file.
		var task = new TestableKiotaGenerateTask();

		task.ClientClassName.Should().Be("ApiClient",
			"default ClientClassName should match the .props default");
		task.Namespace.Should().Be("ApiSdk",
			"default Namespace should be ApiSdk (consumer overrides via .props)");
		task.UsesBackingStore.Should().BeFalse(
			"UsesBackingStore should default to false");
		task.IncludeAdditionalData.Should().BeTrue(
			"IncludeAdditionalData should default to true");
		task.ExcludeBackwardCompatible.Should().BeFalse(
			"ExcludeBackwardCompatible should default to false");
		task.TypeAccessModifier.Should().Be("Public",
			"TypeAccessModifier should default to Public");
	}

	[TestMethod]
	public void MsBuildTask_MultipleReferences_GenerateDistinctCommands()
	{
		// Simulate a consumer project with multiple KiotaOpenApiReference items.
		// Each should result in a distinct task execution with its own arguments.
		var specs = new[]
		{
			("petstore.json", "PetStoreClient", "MyApp.PetStore"),
			("enums.json", "EnumClient", "MyApp.Enums"),
			("inheritance.json", "InheritanceClient", "MyApp.Inheritance"),
		};

		var commandLines = new List<string>();

		foreach (var (specFile, className, ns) in specs)
		{
			var engine = new MockBuildEngine();
			var task = new TestableKiotaGenerateTask
			{
				BuildEngine = engine,
				KiotaToolDll = @"C:\tools\kiota-gen.dll",
				OpenApi = Path.Combine(@"C:\specs", specFile),
				OutputDirectory = Path.Combine(@"C:\out", className),
				ClientClassName = className,
				Namespace = ns,
			};

			task.ExposedValidateParameters().Should().BeTrue(
				$"validation should pass for {specFile}");

			var cmdLine = task.ExposedGenerateCommandLineCommands();
			cmdLine.Should().Contain(className,
				$"command for {specFile} should use class name {className}");
			cmdLine.Should().Contain(ns,
				$"command for {specFile} should use namespace {ns}");

			commandLines.Add(cmdLine);
		}

		commandLines.Should().OnlyHaveUniqueItems(
			"each KiotaOpenApiReference should produce a distinct CLI command");
	}

	// ==================================================================
	// 5. Cross-Path Configuration Compatibility
	// ==================================================================

	[TestMethod]
	public void BothPaths_ExposeEquivalentConfigurationSurface()
	{
		// The source generator props file should declare metadata names
		// that correspond to each MSBuild task property.

		var sgPropsPath = Path.Combine(SrcDir,
			"Uno.Extensions.Http.Kiota.SourceGenerator",
			"buildTransitive",
			"Uno.Extensions.Http.Kiota.SourceGenerator.props");
		var taskPropsPath = Path.Combine(SrcDir,
			"Uno.Extensions.Http.Kiota.Generator.Tasks",
			"buildTransitive",
			"Uno.Extensions.Http.Kiota.Generator.props");

		var sgContent = File.ReadAllText(sgPropsPath);
		var taskContent = File.ReadAllText(taskPropsPath);

		// Core configuration concepts that must exist in both paths:
		// Source generator uses "KiotaClientName", task uses "ClientClassName"
		// Source generator uses "KiotaNamespace", task uses "Namespace"
		// Source generator uses "KiotaUsesBackingStore", task uses "UsesBackingStore"

		// Source generator path: verify per-file metadata declarations
		sgContent.Should().Contain("KiotaClientName",
			"source generator props must expose KiotaClientName metadata");
		sgContent.Should().Contain("KiotaNamespace",
			"source generator props must expose KiotaNamespace metadata");
		sgContent.Should().Contain("KiotaUsesBackingStore",
			"source generator props must expose KiotaUsesBackingStore metadata");
		sgContent.Should().Contain("KiotaIncludeAdditionalData",
			"source generator props must expose KiotaIncludeAdditionalData metadata");
		sgContent.Should().Contain("KiotaExcludeBackwardCompatible",
			"source generator props must expose KiotaExcludeBackwardCompatible metadata");
		sgContent.Should().Contain("KiotaTypeAccessModifier",
			"source generator props must expose KiotaTypeAccessModifier metadata");
		sgContent.Should().Contain("KiotaIncludePatterns",
			"source generator props must expose KiotaIncludePatterns metadata");
		sgContent.Should().Contain("KiotaExcludePatterns",
			"source generator props must expose KiotaExcludePatterns metadata");

		// Task path: verify KiotaOpenApiReference metadata defaults
		taskContent.Should().Contain("ClientClassName",
			"task props must define ClientClassName default");
		taskContent.Should().Contain("Namespace",
			"task props must define Namespace default");
		taskContent.Should().Contain("UsesBackingStore",
			"task props must define UsesBackingStore default");
		taskContent.Should().Contain("IncludeAdditionalData",
			"task props must define IncludeAdditionalData default");
		taskContent.Should().Contain("ExcludeBackwardCompatible",
			"task props must define ExcludeBackwardCompatible default");
		taskContent.Should().Contain("TypeAccessModifier",
			"task props must define TypeAccessModifier default");
		taskContent.Should().Contain("IncludePatterns",
			"task props must define IncludePatterns default");
		taskContent.Should().Contain("ExcludePatterns",
			"task props must define ExcludePatterns default");
	}

	[TestMethod]
	public void BothPaths_ShareEnabledToggleConcept()
	{
		// Phase 1: KiotaGeneratorEnabled (in task .props)
		// Phase 2: KiotaGenerator_Enabled (in source generator .props)
		// Both must exist to allow consumers to disable generation.

		var sgPropsPath = Path.Combine(SrcDir,
			"Uno.Extensions.Http.Kiota.SourceGenerator",
			"buildTransitive",
			"Uno.Extensions.Http.Kiota.SourceGenerator.props");
		var taskPropsPath = Path.Combine(SrcDir,
			"Uno.Extensions.Http.Kiota.Generator.Tasks",
			"buildTransitive",
			"Uno.Extensions.Http.Kiota.Generator.props");

		var sgContent = File.ReadAllText(sgPropsPath);
		var taskContent = File.ReadAllText(taskPropsPath);

		sgContent.Should().Contain("KiotaGenerator_Enabled",
			"source generator props should define a KiotaGenerator_Enabled toggle");
		taskContent.Should().Contain("KiotaGeneratorEnabled",
			"task props should define a KiotaGeneratorEnabled toggle");
	}

	// ==================================================================
	// 6. Source Generator — Disabled Toggle
	// ==================================================================

	[TestMethod]
	public void SourceGenerator_WhenDisabled_ProducesNoOutput()
	{
		var specPath = Path.Combine(TestDataDir, "petstore.json");
		var specContent = File.ReadAllText(specPath);

		var (outputCompilation, compilation, _) = RunSourceGenerator(
			specContent, specPath, "PetStoreClient", "TestApp.PetStore",
			enabled: false);

		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();
		generatedTrees.Should().BeEmpty(
			"the source generator should produce no output when disabled via KiotaGenerator_Enabled=false");
	}

	// ==================================================================
	// 7. Source Generator — Multiple Specs in One Compilation
	// ==================================================================

	[TestMethod]
	public void SourceGenerator_MultipleSpecs_ProducesOutputForEach()
	{
		// Simulate a consumer project with two different OpenAPI specs,
		// each registered as a separate AdditionalFile.
		var specs = new[]
		{
			(File: "petstore.json", ClientName: "PetStoreClient", Namespace: "TestApp.PetStore"),
			(File: "enums.json", ClientName: "EnumClient", Namespace: "TestApp.Enums"),
		};

		var additionalTexts = new List<AdditionalText>();
		var fileOptionsMap = new Dictionary<string, ImmutableDictionary<string, string>>();

		foreach (var spec in specs)
		{
			var specPath = Path.Combine(TestDataDir, spec.File);
			if (!File.Exists(specPath))
			{
				Assert.Inconclusive($"Test spec not found: {specPath}");
				return;
			}

			var content = File.ReadAllText(specPath);
			var additionalText = new InMemoryAdditionalText(specPath, content);
			additionalTexts.Add(additionalText);

			fileOptionsMap[specPath] = ImmutableDictionary.CreateRange(new[]
			{
				KeyValuePair.Create("build_metadata.AdditionalFiles.KiotaClientName", spec.ClientName),
				KeyValuePair.Create("build_metadata.AdditionalFiles.KiotaNamespace", spec.Namespace),
				KeyValuePair.Create("build_metadata.AdditionalFiles.KiotaExcludeBackwardCompatible", "true"),
			});
		}

		var globalOptions = ImmutableDictionary.CreateRange(new[]
		{
			KeyValuePair.Create("build_property.KiotaGenerator_Enabled", "true"),
		});

		var optionsProvider = new MultiFileAnalyzerConfigOptionsProvider(
			globalOptions, fileOptionsMap);

		var generator = new KiotaSourceGenerator();
		var driver = CSharpGeneratorDriver.Create(generator)
			.AddAdditionalTexts(ImmutableArray.CreateRange(additionalTexts))
			.WithUpdatedAnalyzerConfigOptions(optionsProvider);

		var compilation = CreateMinimalCompilation();

		driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		var generatorErrors = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		generatorErrors.Should().BeEmpty(
			"the generator should handle multiple specs without errors");

		var allSource = GetAllGeneratedSource(outputCompilation, compilation);

		allSource.Should().Contain("PetStoreClient",
			"output should contain code from the petstore spec");
		allSource.Should().Contain("EnumClient",
			"output should contain code from the enums spec");
		allSource.Should().Contain("namespace TestApp.PetStore",
			"output should contain the petstore namespace");
		allSource.Should().Contain("namespace TestApp.Enums",
			"output should contain the enums namespace");
	}

	// ==================================================================
	// 8. Source Generator — Generated Code Syntax Validation
	// ==================================================================

	[TestMethod]
	public void SourceGenerator_AllGeneratedFiles_AreSyntacticallyValidCSharp()
	{
		var specPath = Path.Combine(TestDataDir, "petstore.json");
		var specContent = File.ReadAllText(specPath);

		var (outputCompilation, compilation, _) = RunSourceGenerator(
			specContent, specPath, "PetStoreClient", "TestApp.PetStore");

		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();

		foreach (var tree in generatedTrees)
		{
			var root = tree.GetRoot();

			// Should contain at least one type declaration
			var typeDeclarations = root.DescendantNodes()
				.OfType<TypeDeclarationSyntax>()
				.ToList();

			// Some files may be enum declarations
			var enumDeclarations = root.DescendantNodes()
				.OfType<EnumDeclarationSyntax>()
				.ToList();

			var totalDeclarations = typeDeclarations.Count + enumDeclarations.Count;
			totalDeclarations.Should().BeGreaterThan(0,
				$"generated file '{tree.FilePath}' should contain at least one type or enum declaration");

			// No syntax errors
			var errors = tree.GetDiagnostics()
				.Where(d => d.Severity == DiagnosticSeverity.Error)
				.ToList();
			errors.Should().BeEmpty(
				$"generated file '{tree.FilePath}' should have no syntax errors");
		}
	}

	[TestMethod]
	public void SourceGenerator_GeneratesPartialClasses()
	{
		// All generated classes should be partial to allow user extension.
		var specPath = Path.Combine(TestDataDir, "petstore.json");
		var specContent = File.ReadAllText(specPath);

		var (outputCompilation, compilation, _) = RunSourceGenerator(
			specContent, specPath, "PetStoreClient", "TestApp.PetStore");

		var generatedTrees = outputCompilation.SyntaxTrees
			.Except(compilation.SyntaxTrees)
			.ToList();

		foreach (var tree in generatedTrees)
		{
			var classDeclarations = tree.GetRoot()
				.DescendantNodes()
				.OfType<ClassDeclarationSyntax>()
				.ToList();

			foreach (var cls in classDeclarations)
			{
				cls.Modifiers.Any(SyntaxKind.PartialKeyword).Should().BeTrue(
					$"generated class '{cls.Identifier.Text}' in '{tree.FilePath}' " +
					"should be declared as partial for user extensibility");
			}
		}
	}

	// ==================================================================
	// 9. MSBuild Integration — Incremental Build Support
	// ==================================================================

	[TestMethod]
	public void TaskTargets_DefinesIncrementalBuildSupport()
	{
		// Verify that the .targets file defines Inputs/Outputs on
		// _KiotaGenerate for incremental build support.
		var targetsPath = Path.Combine(SrcDir,
			"Uno.Extensions.Http.Kiota.Generator.Tasks",
			"buildTransitive",
			"Uno.Extensions.Http.Kiota.Generator.targets");

		var content = File.ReadAllText(targetsPath);

		// The _KiotaGenerate target should have Inputs and Outputs attributes
		content.Should().Contain("Inputs=\"@(KiotaOpenApiReference)\"",
			"_KiotaGenerate should use spec files as Input for incremental build");
		content.Should().Contain(".stamp",
			"_KiotaGenerate should use stamp files for incremental build tracking");
	}

	// ==================================================================
	// Helpers
	// ==================================================================

	/// <summary>
	/// Runs the source generator against the petstore spec and returns a
	/// tuple for multiple assertions.
	/// </summary>
	private (Compilation OutputCompilation, Compilation InputCompilation, string AllSource)
		RunPetstoreGenerator()
	{
		var specPath = Path.Combine(TestDataDir, "petstore.json");
		var specContent = File.ReadAllText(specPath);

		var (outputCompilation, compilation, _) = RunSourceGenerator(
			specContent, specPath, "PetStoreClient", "TestApp.PetStore");

		var allSource = GetAllGeneratedSource(outputCompilation, compilation);
		return (outputCompilation, compilation, allSource);
	}

	/// <summary>
	/// Runs the source generator with given config and returns the output
	/// compilation, input compilation, and all diagnostics.
	/// </summary>
	private static (Compilation OutputCompilation, Compilation InputCompilation,
		ImmutableArray<Diagnostic> Diagnostics)
		RunSourceGenerator(
			string specContent,
			string specPath,
			string clientName,
			string namespaceName,
			bool enabled = true)
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
			KeyValuePair.Create(
				"build_property.KiotaGenerator_Enabled", enabled.ToString()),
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

		return (outputCompilation, compilation, diagnostics);
	}

	private static CSharpCompilation CreateMinimalCompilation()
	{
		var references = new[]
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		};

		return CSharpCompilation.Create(
			assemblyName: "NuGetValidationTests",
			syntaxTrees: Array.Empty<SyntaxTree>(),
			references: references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

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
	// Test Doubles
	// ==================================================================

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
	/// An <see cref="AnalyzerConfigOptionsProvider"/> that returns
	/// per-file options based on the <see cref="AdditionalText.Path"/>,
	/// allowing tests with multiple additional files to have distinct config.
	/// </summary>
	private sealed class MultiFileAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
	{
		private readonly DictionaryAnalyzerConfigOptions _globalOptions;
		private readonly Dictionary<string, DictionaryAnalyzerConfigOptions> _fileOptions;

		public MultiFileAnalyzerConfigOptionsProvider(
			ImmutableDictionary<string, string> globalOptions,
			Dictionary<string, ImmutableDictionary<string, string>> fileOptions)
		{
			_globalOptions = new DictionaryAnalyzerConfigOptions(globalOptions);
			_fileOptions = fileOptions.ToDictionary(
				kv => kv.Key,
				kv => new DictionaryAnalyzerConfigOptions(kv.Value),
				StringComparer.OrdinalIgnoreCase);
		}

		public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
			=> DictionaryAnalyzerConfigOptions.Empty;

		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
		{
			if (_fileOptions.TryGetValue(textFile.Path, out var options))
			{
				return options;
			}

			return DictionaryAnalyzerConfigOptions.Empty;
		}
	}

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

	/// <summary>
	/// Testable wrapper for <see cref="KiotaGenerateTask"/> that exposes
	/// protected members for assertion.
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
	}

	/// <summary>
	/// Minimal <see cref="IBuildEngine"/> capturing diagnostics.
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
			=> Errors.Add(new BuildDiagnostic(e.Code ?? "", e.Message ?? "", e.File ?? ""));

		public void LogWarningEvent(BuildWarningEventArgs e)
			=> Warnings.Add(new BuildDiagnostic(e.Code ?? "", e.Message ?? "", e.File ?? ""));

		public void LogMessageEvent(BuildMessageEventArgs e)
			=> Messages.Add(e.Message ?? "");

		public void LogCustomEvent(CustomBuildEventArgs e) { }

		public bool BuildProjectFile(
			string projectFileName,
			string[] targetNames,
			IDictionary globalProperties,
			IDictionary targetOutputs) => true;
	}

	private sealed record BuildDiagnostic(string Code, string Message, string File);
}
