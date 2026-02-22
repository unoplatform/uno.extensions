using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Http.Kiota.Generator.Tests.Parity;

/// <summary>
/// Parity tests that compare source-generator output against Kiota CLI
/// golden files for the <c>petstore.json</c> OpenAPI spec.
/// <para>
/// The petstore spec exercises the fundamental Kiota code generation patterns:
/// <list type="bullet">
///   <item>Root client class with navigation properties and serializer registration</item>
///   <item>Request builder hierarchy (collection and item endpoints)</item>
///   <item>Model classes with primitive, complex, enum, and collection properties</item>
///   <item>Enum types with <c>[EnumMember]</c> attributes</item>
///   <item>Error response models implementing <c>IApiErrorable</c></item>
///   <item>Nullable reference type conditional compilation guards</item>
///   <item>Serialization and deserialization method bodies</item>
///   <item>Factory methods with discriminator mapping</item>
///   <item>Query parameter classes for filtered endpoints</item>
/// </list>
/// </para>
/// </summary>
[TestClass]
[TestCategory("Parity")]
public class PetstoreParityTests : ParityTestBase
{
	// ------------------------------------------------------------------
	// Configuration — petstore spec
	// ------------------------------------------------------------------

	/// <inheritdoc/>
	protected override string SpecFileName => "petstore.json";

	/// <inheritdoc/>
	protected override string ClientName => "PetStoreClient";

	/// <inheritdoc/>
	protected override string ClientNamespace => "PetStore.Client";

	/// <inheritdoc/>
	protected override string GoldenSubdirectory => "petstore";

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
			"the generator should not report any errors for the petstore spec");
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
	public void PetStoreClient_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("PetStoreClient.cs", result.GeneratedSources);
	}

	// --- Model classes ---

	[TestMethod]
	public void Models_Pet_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\Pet.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_Category_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\Category.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_Tag_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\Tag.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_Owner_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\Owner.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_Address_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\Address.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_Vaccination_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\Vaccination.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_CreatePetRequest_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\CreatePetRequest.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_UpdatePetRequest_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\UpdatePetRequest.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_PetCollection_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\PetCollection.cs", result.GeneratedSources);
	}

	// --- Enum types ---

	[TestMethod]
	public void Models_PetStatus_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\PetStatus.cs", result.GeneratedSources);
	}

	// --- Error response models ---

	[TestMethod]
	public void Models_ApiError_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\ApiError.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_ApiErrorDetail_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\ApiErrorDetail.cs", result.GeneratedSources);
	}

	// --- Request builders (collection endpoints) ---

	[TestMethod]
	public void Pets_PetsRequestBuilder_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Pets\\PetsRequestBuilder.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Owners_OwnersRequestBuilder_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Owners\\OwnersRequestBuilder.cs", result.GeneratedSources);
	}

	// --- Request builders (item endpoints) ---

	[TestMethod]
	public void Pets_Item_WithPetItemRequestBuilder_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Pets\\Item\\WithPetItemRequestBuilder.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Owners_Item_WithOwnerItemRequestBuilder_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Owners\\Item\\WithOwnerItemRequestBuilder.cs", result.GeneratedSources);
	}

	// --- Nested request builders ---

	[TestMethod]
	public void Pets_Item_Vaccinations_VaccinationsRequestBuilder_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile(
			"Pets\\Item\\Vaccinations\\VaccinationsRequestBuilder.cs",
			result.GeneratedSources);
	}

	// ==================================================================
	// Normalization-specific tests — verify that our normalization
	// handles known acceptable differences correctly
	// ==================================================================

	[TestMethod]
	public void Normalize_RemovesVersionStrings()
	{
		var source = @"[global::System.CodeDom.Compiler.GeneratedCode(""Kiota"", ""1.30.0"")]";
		var expected = @"[global::System.CodeDom.Compiler.GeneratedCode(""Kiota"", ""VERSION"")]";

		Normalize(source).Should().Be(expected);
	}

	[TestMethod]
	public void Normalize_NormalizesLineEndings()
	{
		var crlfSource = "line1\r\nline2\r\nline3";
		var lfSource = "line1\nline2\nline3";

		Normalize(crlfSource).Should().Be(Normalize(lfSource));
	}

	[TestMethod]
	public void Normalize_TrimsTrailingWhitespace()
	{
		var source = "line1   \nline2\t\nline3";
		var expected = "line1\nline2\nline3";

		Normalize(source).Should().Be(expected);
	}

	[TestMethod]
	public void Normalize_RemovesTrailingBlankLines()
	{
		var source = "line1\nline2\n\n\n";
		var expected = "line1\nline2";

		Normalize(source).Should().Be(expected);
	}
}
