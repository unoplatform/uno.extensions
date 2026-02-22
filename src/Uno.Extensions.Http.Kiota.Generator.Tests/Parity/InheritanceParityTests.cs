using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Http.Kiota.Generator.Tests.Parity;

/// <summary>
/// Parity tests that compare source-generator output against Kiota CLI
/// golden files for the <c>inheritance.json</c> OpenAPI spec.
/// <para>
/// The inheritance spec exercises allOf-based class inheritance with
/// discriminator mappings, multi-level inheritance chains, and multiple
/// independent inheritance hierarchies:
/// <list type="bullet">
///   <item>Base classes with discriminator factories (Animal, Vehicle)</item>
///   <item>Derived classes via allOf composition (Cat, Dog, Bird, Car, Truck)</item>
///   <item>Multi-level inheritance (DomesticCat → Cat → Animal)</item>
///   <item>Derived class serializer/deserializer calling base methods</item>
///   <item>Factory method with discriminator switch on base class only</item>
///   <item>Child factory method using <c>static new</c> modifier</item>
///   <item>Error response model extending ApiException</item>
///   <item>Request builders with collection return types and error mappings</item>
///   <item>Item request builders with path parameter substitution</item>
/// </list>
/// </para>
/// </summary>
[TestClass]
[TestCategory("Parity")]
public class InheritanceParityTests : ParityTestBase
{
	// ------------------------------------------------------------------
	// Configuration — inheritance spec
	// ------------------------------------------------------------------

	/// <inheritdoc/>
	protected override string SpecFileName => "inheritance.json";

	/// <inheritdoc/>
	protected override string ClientName => "InheritanceClient";

	/// <inheritdoc/>
	protected override string ClientNamespace => "Inheritance.Client";

	/// <inheritdoc/>
	protected override string GoldenSubdirectory => "inheritance";

	// ------------------------------------------------------------------
	// Cached results — avoid re-running the generator for each test
	// ------------------------------------------------------------------

	private static GeneratorRunResult? _cachedResult;
	private static Dictionary<string, string>? _cachedGoldenFiles;

	private GeneratorRunResult GetOrRunGenerator()
	{
		if (_cachedResult == null)
		{
			_cachedResult = RunGenerator();
		}
		return _cachedResult;
	}

	private Dictionary<string, string> GetOrLoadGoldenFiles()
	{
		if (_cachedGoldenFiles == null)
		{
			_cachedGoldenFiles = LoadGoldenFiles();
		}
		return _cachedGoldenFiles;
	}

	[ClassCleanup]
	public static void Cleanup()
	{
		_cachedResult = null;
		_cachedGoldenFiles = null;
	}

	// ==================================================================
	// Structural tests — verify the generator produces the expected
	// set of files before comparing content
	// ==================================================================

	[TestMethod]
	public void Generator_ProducesNoErrors()
	{
		var result = GetOrRunGenerator();

		var errors = result.Diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();

		errors.Should().BeEmpty(
			"the generator should not report any errors for the inheritance spec");
	}

	[TestMethod]
	public void Generator_ProducesAtLeastAsManyFilesAsGoldenFiles()
	{
		var result = GetOrRunGenerator();
		var goldenFiles = GetOrLoadGoldenFiles();

		AssertMinimumFileCount(result.GeneratedSources, goldenFiles);
	}

	[TestMethod]
	public void Generator_ProducesAllExpectedFiles()
	{
		var result = GetOrRunGenerator();
		var goldenFiles = GetOrLoadGoldenFiles();

		AssertAllGoldenFilesHaveGeneratedCounterparts(
			result.GeneratedSources, goldenFiles);
	}

	// ==================================================================
	// Full parity — compare every golden file against generated output
	// ==================================================================

	[TestMethod]
	public void AllFiles_MatchGoldenFiles()
	{
		var result = GetOrRunGenerator();
		var goldenFiles = GetOrLoadGoldenFiles();

		AssertParityForAllFiles(result.GeneratedSources, goldenFiles);
	}

	// ==================================================================
	// Individual file parity tests — for targeted debugging when the
	// full parity test fails. Each test isolates one golden file.
	// ==================================================================

	// --- Root client ---

	[TestMethod]
	public void InheritanceClient_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("InheritanceClient.cs", result.GeneratedSources);
	}

	// --- Base model classes (with discriminator factories) ---

	[TestMethod]
	public void Models_Animal_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\Animal.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_Vehicle_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\Vehicle.cs", result.GeneratedSources);
	}

	// --- Derived model classes (Animal hierarchy) ---

	[TestMethod]
	public void Models_Cat_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\Cat.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_Dog_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\Dog.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_Bird_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\Bird.cs", result.GeneratedSources);
	}

	// --- Multi-level inheritance ---

	[TestMethod]
	public void Models_DomesticCat_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\DomesticCat.cs", result.GeneratedSources);
	}

	// --- Derived model classes (Vehicle hierarchy) ---

	[TestMethod]
	public void Models_Car_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\Car.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_Truck_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\Truck.cs", result.GeneratedSources);
	}

	// --- Error response model ---

	[TestMethod]
	public void Models_ApiError_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\ApiError.cs", result.GeneratedSources);
	}

	// --- Request builders ---

	[TestMethod]
	public void Animals_AnimalsRequestBuilder_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Animals\\AnimalsRequestBuilder.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Animals_Item_WithAnimalItemRequestBuilder_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile(
			"Animals\\Item\\WithAnimalItemRequestBuilder.cs",
			result.GeneratedSources);
	}

	[TestMethod]
	public void Vehicles_VehiclesRequestBuilder_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Vehicles\\VehiclesRequestBuilder.cs", result.GeneratedSources);
	}
}
