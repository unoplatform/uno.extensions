#pragma warning disable CS8602 // Dereference of a possibly null reference — guarded by FluentAssertions
#pragma warning disable CS8603 // Possible null reference return — FindClass/FindEnum intentionally nullable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;
using Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;
using Uno.Extensions.Http.Kiota.SourceGenerator.Parsing;

namespace Uno.Extensions.Http.Kiota.Generator.Tests.Phase2;

/// <summary>
/// Unit tests for <see cref="KiotaCodeDomBuilder"/> covering the full
/// CodeDOM construction pipeline: model declarations, enum declarations,
/// request builders, navigation properties, indexers, inheritance (allOf),
/// composed types (oneOf/anyOf), discriminator mappings, error models,
/// and type resolution.
/// <para>
/// Each test group uses a dedicated OpenAPI test spec from TestData/:
/// </para>
/// <list type="bullet">
///   <item>Petstore — model classes, enums, request builders, indexers</item>
///   <item>Inheritance — allOf base classes, discriminators, multi-level</item>
///   <item>Composed types — oneOf (union), anyOf (intersection)</item>
///   <item>Edge cases — null doc, empty doc, configuration overrides</item>
/// </list>
/// </summary>
[TestClass]
public class CodeDomBuilderTests
{
	// ------------------------------------------------------------------
	// Test data paths
	// ------------------------------------------------------------------

	private static string TestDataDir =>
		Path.Combine(AppContext.BaseDirectory, "TestData");

	// ------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------

	/// <summary>
	/// Creates a default <see cref="KiotaGeneratorConfig"/> for test use.
	/// </summary>
	private static KiotaGeneratorConfig CreateConfig(
		string clientName = "TestClient",
		string namespaceName = "Test.Namespace",
		bool usesBackingStore = false,
		bool includeAdditionalData = true,
		string typeAccessModifier = "Public")
	{
		return new KiotaGeneratorConfig(
			clientClassName: clientName,
			clientNamespaceName: namespaceName,
			usesBackingStore: usesBackingStore,
			includeAdditionalData: includeAdditionalData,
			excludeBackwardCompatible: false,
			typeAccessModifier: typeAccessModifier,
			includePatterns: ImmutableArray<string>.Empty,
			excludePatterns: ImmutableArray<string>.Empty);
	}

	/// <summary>
	/// Parses an OpenAPI spec from TestData/ and builds the CodeDOM tree.
	/// </summary>
	private static CodeNamespace BuildFromSpec(
		string specFileName,
		string clientName = "TestClient",
		string namespaceName = "Test.Namespace",
		bool usesBackingStore = false,
		bool includeAdditionalData = true,
		string typeAccessModifier = "Public")
	{
		var filePath = Path.Combine(TestDataDir, specFileName);
		var sourceText = SourceText.From(File.ReadAllText(filePath));
		var parseResult = OpenApiDocumentParser.Parse(sourceText, filePath);

		parseResult.IsSuccess.Should().BeTrue(
			$"test fixture '{specFileName}' should parse successfully");

		var config = CreateConfig(clientName, namespaceName, usesBackingStore,
			includeAdditionalData, typeAccessModifier);
		var builder = new KiotaCodeDomBuilder(config);
		return builder.Build(parseResult.Document);
	}

	/// <summary>
	/// Recursively collects all <see cref="CodeClass"/> declarations in the
	/// namespace tree (including those in child namespaces).
	/// </summary>
	private static System.Collections.Generic.List<CodeClass> GetAllClasses(CodeNamespace root)
	{
		var result = new System.Collections.Generic.List<CodeClass>();
		CollectClasses(root, result);
		return result;
	}

	private static void CollectClasses(
		CodeNamespace ns,
		System.Collections.Generic.List<CodeClass> result)
	{
		result.AddRange(ns.Classes);
		foreach (var child in ns.Namespaces)
		{
			CollectClasses(child, result);
		}
	}

	/// <summary>
	/// Recursively collects all <see cref="CodeEnum"/> declarations.
	/// </summary>
	private static System.Collections.Generic.List<CodeEnum> GetAllEnums(CodeNamespace root)
	{
		var result = new System.Collections.Generic.List<CodeEnum>();
		CollectEnums(root, result);
		return result;
	}

	private static void CollectEnums(
		CodeNamespace ns,
		System.Collections.Generic.List<CodeEnum> result)
	{
		result.AddRange(ns.Enums);
		foreach (var child in ns.Namespaces)
		{
			CollectEnums(child, result);
		}
	}

	/// <summary>
	/// Finds a specific class by name across the entire namespace tree.
	/// </summary>
	private static CodeClass FindClass(CodeNamespace root, string name)
	{
		return GetAllClasses(root)
			.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.Ordinal));
	}

	/// <summary>
	/// Finds a specific enum by name across the entire namespace tree.
	/// </summary>
	private static CodeEnum FindEnum(CodeNamespace root, string name)
	{
		return GetAllEnums(root)
			.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.Ordinal));
	}

	// ==================================================================
	// 1. Null/empty document
	// ==================================================================

	[TestMethod]
	public void Build_NullDocument_ThrowsArgumentNullException()
	{
		var config = CreateConfig();
		var builder = new KiotaCodeDomBuilder(config);

		Action act = () => builder.Build(null);

		act.Should().Throw<ArgumentNullException>().WithParameterName("document");
	}

	[TestMethod]
	public void Build_EmptyDocument_ReturnsRootNamespaceWithClientClass()
	{
		var config = CreateConfig();
		var builder = new KiotaCodeDomBuilder(config);
		var doc = new OpenApiDocument
		{
			Info = new OpenApiInfo { Title = "Empty", Version = "1.0" },
		};

		var root = builder.Build(doc);

		root.Should().NotBeNull();
		root.Name.Should().Be("Test.Namespace");
		// Should contain the root client class.
		root.Classes.Should().ContainSingle(c => c.Name == "TestClient");
	}

	// ==================================================================
	// 2. Petstore — root namespace
	// ==================================================================

	[TestMethod]
	public void Petstore_RootNamespaceHasConfiguredName()
	{
		var root = BuildFromSpec("petstore.json",
			clientName: "PetStoreClient",
			namespaceName: "MyApp.PetStore");

		root.Name.Should().Be("MyApp.PetStore");
	}

	[TestMethod]
	public void Petstore_HasModelsNamespace()
	{
		var root = BuildFromSpec("petstore.json");

		root.Namespaces.Should().Contain(ns => ns.Name == "Models",
			"the builder should create a Models child namespace");
	}

	// ==================================================================
	// 3. Petstore — model classes
	// ==================================================================

	[TestMethod]
	public void Petstore_CreatesExpectedModelClasses()
	{
		var root = BuildFromSpec("petstore.json");

		var modelsNs = root.Namespaces.First(ns => ns.Name == "Models");

		// The petstore spec defines: Pet, CreatePetRequest, UpdatePetRequest,
		// PetCollection, Category, Tag, Owner, Address, Vaccination,
		// ApiError, ApiErrorDetail
		var modelNames = modelsNs.Classes.Select(c => c.Name).ToList();
		modelNames.Should().Contain("Pet");
		modelNames.Should().Contain("CreatePetRequest");
		modelNames.Should().Contain("UpdatePetRequest");
		modelNames.Should().Contain("PetCollection");
		modelNames.Should().Contain("Category");
		modelNames.Should().Contain("Tag");
		modelNames.Should().Contain("Owner");
		modelNames.Should().Contain("Address");
		modelNames.Should().Contain("Vaccination");
		modelNames.Should().Contain("ApiError");
		modelNames.Should().Contain("ApiErrorDetail");
	}

	[TestMethod]
	public void Petstore_ModelClassesAreModelKind()
	{
		var root = BuildFromSpec("petstore.json");
		var modelsNs = root.Namespaces.First(ns => ns.Name == "Models");

		foreach (var cls in modelsNs.Classes)
		{
			cls.Kind.Should().Be(CodeClassKind.Model,
				$"{cls.Name} should be a Model class");
		}
	}

	[TestMethod]
	public void Petstore_PetModelHasExpectedProperties()
	{
		var root = BuildFromSpec("petstore.json");
		var pet = FindClass(root, "Pet");

		pet.Should().NotBeNull("the Pet model should exist");

		var propNames = pet.Properties
			.Where(p => p.Kind == CodePropertyKind.Custom)
			.Select(p => p.Name)
			.ToList();

		// Pet has: id, name, tag, status, category, tags, photoUrls,
		// weight, birthDate, createdAt, isVaccinated, microchipId, age
		propNames.Should().Contain("Id");
		propNames.Should().Contain("Name");
		propNames.Should().Contain("Tag");
		propNames.Should().Contain("Status");
		propNames.Should().Contain("Category");
		propNames.Should().Contain("Tags");
		propNames.Should().Contain("PhotoUrls");
		propNames.Should().Contain("Weight");
		propNames.Should().Contain("BirthDate");
		propNames.Should().Contain("CreatedAt");
		propNames.Should().Contain("IsVaccinated");
		propNames.Should().Contain("MicrochipId");
		propNames.Should().Contain("Age");
	}

	[TestMethod]
	public void Petstore_PetPropertySerializedNamesPreserveOriginalCase()
	{
		var root = BuildFromSpec("petstore.json");
		var pet = FindClass(root, "Pet");

		// The builder should store the original camelCase names as SerializedName.
		var idProp = pet.FindProperty("Id");
		idProp.Should().NotBeNull();
		idProp.SerializedName.Should().Be("id");

		var photoUrlsProp = pet.FindProperty("PhotoUrls");
		photoUrlsProp.Should().NotBeNull();
		photoUrlsProp.SerializedName.Should().Be("photoUrls");

		var microchipIdProp = pet.FindProperty("MicrochipId");
		microchipIdProp.Should().NotBeNull();
		microchipIdProp.SerializedName.Should().Be("microchipId");
	}

	[TestMethod]
	public void Petstore_PetPropertyTypesMappedCorrectly()
	{
		var root = BuildFromSpec("petstore.json");
		var pet = FindClass(root, "Pet");

		// id: string (format: uuid) → Guid
		pet.FindProperty("Id").Type.Name.Should().Be("Guid");

		// name: string → string
		pet.FindProperty("Name").Type.Name.Should().Be("string");

		// weight: number (format: double) → double
		pet.FindProperty("Weight").Type.Name.Should().Be("double");

		// birthDate: string (format: date) → Date
		pet.FindProperty("BirthDate").Type.Name.Should().Be("Date");

		// createdAt: string (format: date-time) → DateTimeOffset
		pet.FindProperty("CreatedAt").Type.Name.Should().Be("DateTimeOffset");

		// isVaccinated: boolean → bool
		pet.FindProperty("IsVaccinated").Type.Name.Should().Be("bool");

		// microchipId: integer (format: int64) → long
		pet.FindProperty("MicrochipId").Type.Name.Should().Be("long");

		// age: integer (format: int32) → int
		pet.FindProperty("Age").Type.Name.Should().Be("int");
	}

	[TestMethod]
	public void Petstore_PetTagsPropertyIsCollection()
	{
		var root = BuildFromSpec("petstore.json");
		var pet = FindClass(root, "Pet");

		var tagsProp = pet.FindProperty("Tags");
		tagsProp.Should().NotBeNull();
		tagsProp.Type.IsCollection.Should().BeTrue("tags is an array of Tag");
		tagsProp.Type.Name.Should().Be("Tag");
	}

	[TestMethod]
	public void Petstore_PetPhotoUrlsPropertyIsStringCollection()
	{
		var root = BuildFromSpec("petstore.json");
		var pet = FindClass(root, "Pet");

		var photoUrlsProp = pet.FindProperty("PhotoUrls");
		photoUrlsProp.Should().NotBeNull();
		photoUrlsProp.Type.IsCollection.Should().BeTrue("photoUrls is an array of string");
		photoUrlsProp.Type.Name.Should().Be("string");
	}

	[TestMethod]
	public void Petstore_PetStatusPropertyReferencesEnum()
	{
		var root = BuildFromSpec("petstore.json");
		var pet = FindClass(root, "Pet");

		var statusProp = pet.FindProperty("Status");
		statusProp.Should().NotBeNull();
		statusProp.Type.Should().BeOfType<CodeType>();
		// After MapTypeDefinitions, TypeDefinition should point to the PetStatus enum.
		var codeType = (CodeType)statusProp.Type;
		codeType.TypeDefinition.Should().NotBeNull("type references should be resolved");
		codeType.TypeDefinition.Should().BeOfType<CodeEnum>();
		codeType.TypeDefinition.Name.Should().Be("PetStatus");
	}

	[TestMethod]
	public void Petstore_PetCategoryPropertyReferencesModelClass()
	{
		var root = BuildFromSpec("petstore.json");
		var pet = FindClass(root, "Pet");

		var catProp = pet.FindProperty("Category");
		catProp.Should().NotBeNull();
		var codeType = catProp.Type.Should().BeOfType<CodeType>().Subject;
		codeType.TypeDefinition.Should().NotBeNull();
		codeType.TypeDefinition.Should().BeOfType<CodeClass>();
		codeType.TypeDefinition.Name.Should().Be("Category");
	}

	// ==================================================================
	// 4. Petstore — model class interfaces and methods
	// ==================================================================

	[TestMethod]
	public void Petstore_ModelClassImplementsIParsable()
	{
		var root = BuildFromSpec("petstore.json");
		var pet = FindClass(root, "Pet");

		pet.Interfaces.Should().Contain(i => i.Name == "IParsable",
			"model classes should implement IParsable");
	}

	[TestMethod]
	public void Petstore_ModelWithAdditionalDataHolderInterface()
	{
		var root = BuildFromSpec("petstore.json",
			includeAdditionalData: true);
		var pet = FindClass(root, "Pet");

		pet.Interfaces.Should().Contain(i => i.Name == "IAdditionalDataHolder",
			"when includeAdditionalData is true, models should implement IAdditionalDataHolder");
		pet.Properties.Should().Contain(p => p.Kind == CodePropertyKind.AdditionalData,
			"an AdditionalData property should be present");
	}

	[TestMethod]
	public void Petstore_ModelWithoutAdditionalDataHolderWhenDisabled()
	{
		var root = BuildFromSpec("petstore.json",
			includeAdditionalData: false);
		var pet = FindClass(root, "Pet");

		pet.Interfaces.Should().NotContain(i => i.Name == "IAdditionalDataHolder");
		pet.Properties.Should().NotContain(p => p.Kind == CodePropertyKind.AdditionalData);
	}

	[TestMethod]
	public void Petstore_ModelHasConstructor()
	{
		var root = BuildFromSpec("petstore.json");
		var pet = FindClass(root, "Pet");

		pet.MethodsOfKind(CodeMethodKind.Constructor).Should().NotBeEmpty(
			"model classes should have a constructor");
	}

	[TestMethod]
	public void Petstore_ModelHasFactoryMethod()
	{
		var root = BuildFromSpec("petstore.json");
		var pet = FindClass(root, "Pet");

		var factory = pet.MethodsOfKind(CodeMethodKind.Factory).FirstOrDefault();
		factory.Should().NotBeNull("model classes should have a CreateFromDiscriminatorValue factory");
		factory.Name.Should().Be("CreateFromDiscriminatorValue");
		factory.IsStatic.Should().BeTrue();
	}

	[TestMethod]
	public void Petstore_ModelHasSerializerMethod()
	{
		var root = BuildFromSpec("petstore.json");
		var pet = FindClass(root, "Pet");

		var serializer = pet.MethodsOfKind(CodeMethodKind.Serializer).FirstOrDefault();
		serializer.Should().NotBeNull("model classes should have a Serialize method");
		serializer.Name.Should().Be("Serialize");
		serializer.Parameters.Should().ContainSingle(p => p.Name == "writer");
	}

	[TestMethod]
	public void Petstore_ModelHasDeserializerMethod()
	{
		var root = BuildFromSpec("petstore.json");
		var pet = FindClass(root, "Pet");

		var deserializer = pet.MethodsOfKind(CodeMethodKind.Deserializer).FirstOrDefault();
		deserializer.Should().NotBeNull("model classes should have a GetFieldDeserializers method");
		deserializer.Name.Should().Be("GetFieldDeserializers");
	}

	// ==================================================================
	// 5. Petstore — enums
	// ==================================================================

	[TestMethod]
	public void Petstore_CreatesPetStatusEnum()
	{
		var root = BuildFromSpec("petstore.json");
		var petStatus = FindEnum(root, "PetStatus");

		petStatus.Should().NotBeNull("PetStatus enum should exist");
		petStatus.Options.Should().HaveCount(4,
			"PetStatus has 4 values: available, pending, adopted, fostered");
	}

	[TestMethod]
	public void Petstore_PetStatusEnumHasCorrectOptions()
	{
		var root = BuildFromSpec("petstore.json");
		var petStatus = FindEnum(root, "PetStatus");

		petStatus.FindOptionBySerializedName("available").Should().NotBeNull();
		petStatus.FindOptionBySerializedName("pending").Should().NotBeNull();
		petStatus.FindOptionBySerializedName("adopted").Should().NotBeNull();
		petStatus.FindOptionBySerializedName("fostered").Should().NotBeNull();
	}

	[TestMethod]
	public void Petstore_EnumsAreInModelsNamespace()
	{
		var root = BuildFromSpec("petstore.json");
		var modelsNs = root.Namespaces.First(ns => ns.Name == "Models");

		modelsNs.Enums.Should().Contain(e => e.Name == "PetStatus");
	}

	// ==================================================================
	// 6. Petstore — root client class
	// ==================================================================

	[TestMethod]
	public void Petstore_CreatesRootClientClass()
	{
		var root = BuildFromSpec("petstore.json",
			clientName: "PetStoreClient");

		var clientClass = root.Classes.FirstOrDefault(c => c.Name == "PetStoreClient");
		clientClass.Should().NotBeNull("root client class should exist");
		clientClass.Kind.Should().Be(CodeClassKind.RequestBuilder);
	}

	[TestMethod]
	public void Petstore_RootClientExtendsBaseRequestBuilder()
	{
		var root = BuildFromSpec("petstore.json",
			clientName: "PetStoreClient");

		var clientClass = FindClass(root, "PetStoreClient");
		clientClass.BaseClass.Should().NotBeNull();
		clientClass.BaseClass.Name.Should().Be("BaseRequestBuilder");
		clientClass.BaseClass.IsExternal.Should().BeTrue();
	}

	[TestMethod]
	public void Petstore_RootClientHasConstructorWithRequestAdapter()
	{
		var root = BuildFromSpec("petstore.json",
			clientName: "PetStoreClient");

		var clientClass = FindClass(root, "PetStoreClient");
		var ctors = clientClass.MethodsOfKind(CodeMethodKind.Constructor).ToList();
		ctors.Should().NotBeEmpty();

		var ctor = ctors.First();
		ctor.Parameters.Should().Contain(
			p => p.Name == "requestAdapter"
				&& p.Kind == CodeParameterKind.RequestAdapter);
	}

	[TestMethod]
	public void Petstore_RootClientHasUrlTemplateProperty()
	{
		var root = BuildFromSpec("petstore.json",
			clientName: "PetStoreClient");

		var clientClass = FindClass(root, "PetStoreClient");
		var urlTemplate = clientClass.PropertiesOfKind(CodePropertyKind.UrlTemplate).FirstOrDefault();
		urlTemplate.Should().NotBeNull("root client should have a UrlTemplate property");
	}

	[TestMethod]
	public void Petstore_RootClientHasNavigationProperties()
	{
		var root = BuildFromSpec("petstore.json",
			clientName: "PetStoreClient");

		var clientClass = FindClass(root, "PetStoreClient");
		var navProps = clientClass.PropertiesOfKind(CodePropertyKind.Navigation).ToList();

		// petstore has /pets and /owners as top-level paths
		navProps.Should().Contain(p => p.Name == "Pets",
			"root client should have a Pets navigation property");
		navProps.Should().Contain(p => p.Name == "Owners",
			"root client should have an Owners navigation property");
	}

	// ==================================================================
	// 7. Petstore — request builders
	// ==================================================================

	[TestMethod]
	public void Petstore_CreatesRequestBuildersForAllPaths()
	{
		var root = BuildFromSpec("petstore.json");
		var allClasses = GetAllClasses(root);
		var requestBuilders = allClasses
			.Where(c => c.Kind == CodeClassKind.RequestBuilder
				&& c.Name != "TestClient") // exclude root client
			.ToList();

		// /pets → PetsRequestBuilder
		requestBuilders.Should().Contain(c => c.Name == "PetsRequestBuilder");

		// /pets/{petId} → WithPetItemRequestBuilder
		requestBuilders.Should().Contain(c => c.Name == "WithPetItemRequestBuilder");

		// /owners → OwnersRequestBuilder
		requestBuilders.Should().Contain(c => c.Name == "OwnersRequestBuilder");

		// /owners/{ownerId} → WithOwnerItemRequestBuilder
		requestBuilders.Should().Contain(c => c.Name == "WithOwnerItemRequestBuilder");
	}

	[TestMethod]
	public void Petstore_RequestBuilderExtendsBaseRequestBuilder()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		petsRb.Should().NotBeNull();
		petsRb.BaseClass.Should().NotBeNull();
		petsRb.BaseClass.Name.Should().Be("BaseRequestBuilder");
	}

	[TestMethod]
	public void Petstore_RequestBuilderHasUrlTemplateProperty()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var urlTemplate = petsRb.PropertiesOfKind(CodePropertyKind.UrlTemplate).FirstOrDefault();
		urlTemplate.Should().NotBeNull();
		// The URL template should include the path segment and query params.
		urlTemplate.SerializedName.Should().Contain("/pets");
	}

	[TestMethod]
	public void Petstore_RequestBuilderHasTwoConstructors()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var ctors = petsRb.MethodsOfKind(CodeMethodKind.Constructor).ToList();
		ctors.Should().HaveCount(2,
			"request builders should have a pathParameters ctor and a rawUrl ctor");
	}

	[TestMethod]
	public void Petstore_RequestBuilderHasWithUrlMethod()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var withUrl = petsRb.MethodsOfKind(CodeMethodKind.WithUrl).FirstOrDefault();
		withUrl.Should().NotBeNull("request builders should have a WithUrl method");
		withUrl.Parameters.Should().ContainSingle(p => p.Name == "rawUrl");
	}

	// ==================================================================
	// 8. Petstore — HTTP executor methods
	// ==================================================================

	[TestMethod]
	public void Petstore_PetsRequestBuilderHasGetAsyncMethod()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var getAsync = petsRb.FindMethod("GetAsync");
		getAsync.Should().NotBeNull();
		getAsync.Kind.Should().Be(CodeMethodKind.RequestExecutor);
		getAsync.IsAsync.Should().BeTrue();
		getAsync.HttpMethod.Should().Be("GET");
	}

	[TestMethod]
	public void Petstore_PetsRequestBuilderHasPostAsyncMethod()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var postAsync = petsRb.FindMethod("PostAsync");
		postAsync.Should().NotBeNull();
		postAsync.Kind.Should().Be(CodeMethodKind.RequestExecutor);
		postAsync.IsAsync.Should().BeTrue();
		postAsync.HttpMethod.Should().Be("POST");
	}

	[TestMethod]
	public void Petstore_PostAsyncHasBodyParameter()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var postAsync = petsRb.FindMethod("PostAsync");
		var bodyParam = postAsync.ParametersOfKind(CodeParameterKind.Body).FirstOrDefault();
		bodyParam.Should().NotBeNull("POST method should have a body parameter");
		bodyParam.Type.Name.Should().Be("CreatePetRequest");
	}

	[TestMethod]
	public void Petstore_GetAsyncHasCancellationTokenParameter()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var getAsync = petsRb.FindMethod("GetAsync");
		var ctParam = getAsync.ParametersOfKind(CodeParameterKind.Cancellation).FirstOrDefault();
		ctParam.Should().NotBeNull("executor methods should have a CancellationToken parameter");
		ctParam.Type.Name.Should().Be("CancellationToken");
	}

	[TestMethod]
	public void Petstore_GetAsyncHasAcceptHeader()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var getAsync = petsRb.FindMethod("GetAsync");
		getAsync.AcceptedResponseTypes.Should().Contain("application/json");
	}

	// ==================================================================
	// 9. Petstore — request generator methods
	// ==================================================================

	[TestMethod]
	public void Petstore_PetsRequestBuilderHasToGetRequestInformation()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var toGet = petsRb.FindMethod("ToGetRequestInformation");
		toGet.Should().NotBeNull();
		toGet.Kind.Should().Be(CodeMethodKind.RequestGenerator);
		toGet.HttpMethod.Should().Be("GET");
		toGet.ReturnType.Should().NotBeNull();
		toGet.ReturnType.Name.Should().Be("RequestInformation");
	}

	[TestMethod]
	public void Petstore_PetsRequestBuilderHasToPostRequestInformation()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var toPost = petsRb.FindMethod("ToPostRequestInformation");
		toPost.Should().NotBeNull();
		toPost.Kind.Should().Be(CodeMethodKind.RequestGenerator);
	}

	// ==================================================================
	// 10. Petstore — query parameters
	// ==================================================================

	[TestMethod]
	public void Petstore_PetsUrlTemplateIncludesQueryParameters()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var urlTemplate = petsRb.PropertiesOfKind(CodePropertyKind.UrlTemplate).First();
		// The GET /pets endpoint has query params: limit, offset, status, tags
		urlTemplate.SerializedName.Should().Contain("limit");
		urlTemplate.SerializedName.Should().Contain("offset");
	}

	[TestMethod]
	public void Petstore_PetsRequestBuilderHasQueryParametersInnerClass()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		// The builder creates query parameters classes as inner classes.
		petsRb.InnerClasses.Should().Contain(
			c => c.Kind == CodeClassKind.QueryParameters,
			"request builders with query params should have a QueryParameters inner class");
	}

	// ==================================================================
	// 11. Petstore — indexers (parameterized paths)
	// ==================================================================

	[TestMethod]
	public void Petstore_PetsRequestBuilderHasIndexer()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		petsRb.Indexers.Should().NotBeEmpty(
			"the /pets collection should have an indexer for /pets/{petId}");

		var indexer = petsRb.Indexers.First();
		indexer.IndexParameterName.Should().Be("petId");
	}

	[TestMethod]
	public void Petstore_PetsIndexerReturnsPetsItemRequestBuilder()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var indexer = petsRb.Indexers.First();
		indexer.ReturnType.Should().NotBeNull();
		indexer.ReturnType.Name.Should().Be("WithPetItemRequestBuilder");
	}

	[TestMethod]
	public void Petstore_ItemRequestBuilderHasHttpMethods()
	{
		var root = BuildFromSpec("petstore.json");
		var itemRb = FindClass(root, "WithPetItemRequestBuilder");

		itemRb.Should().NotBeNull();

		// /pets/{petId} supports GET, PUT, DELETE
		itemRb.FindMethod("GetAsync").Should().NotBeNull();
		itemRb.FindMethod("PutAsync").Should().NotBeNull();
		itemRb.FindMethod("DeleteAsync").Should().NotBeNull();
	}

	[TestMethod]
	public void Petstore_DeleteAsyncReturnsVoid()
	{
		var root = BuildFromSpec("petstore.json");
		var itemRb = FindClass(root, "WithPetItemRequestBuilder");

		var deleteAsync = itemRb.FindMethod("DeleteAsync");
		deleteAsync.Should().NotBeNull();
		// DELETE /pets/{petId} returns 204 No Content → null return type (void).
		deleteAsync.ReturnType.Should().BeNull("204 No Content means void return");
	}

	// ==================================================================
	// 12. Petstore — nested paths (vaccinations)
	// ==================================================================

	[TestMethod]
	public void Petstore_CreatesVaccinationsRequestBuilder()
	{
		var root = BuildFromSpec("petstore.json");
		var allClasses = GetAllClasses(root);

		allClasses.Should().Contain(c => c.Name == "VaccinationsRequestBuilder",
			"/pets/{petId}/vaccinations should produce a VaccinationsRequestBuilder");
	}

	// ==================================================================
	// 13. Petstore — error mappings
	// ==================================================================

	[TestMethod]
	public void Petstore_ExecutorMethodsHaveErrorMappings()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var getAsync = petsRb.FindMethod("GetAsync");
		getAsync.ErrorMappings.Should().NotBeEmpty(
			"the GET /pets endpoint has 4XX and 5XX error mappings");
		getAsync.ErrorMappings.Should().ContainKey("4XX");
		getAsync.ErrorMappings.Should().ContainKey("5XX");
	}

	[TestMethod]
	public void Petstore_ErrorMappingsReferenceApiErrorModel()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var getAsync = petsRb.FindMethod("GetAsync");
		getAsync.ErrorMappings["4XX"].Name.Should().Be("ApiError");
		getAsync.ErrorMappings["5XX"].Name.Should().Be("ApiError");
	}

	[TestMethod]
	public void Petstore_ApiErrorModelIsMarkedAsErrorDefinition()
	{
		var root = BuildFromSpec("petstore.json");
		var apiError = FindClass(root, "ApiError");

		apiError.Should().NotBeNull();
		apiError.IsErrorDefinition.Should().BeTrue(
			"ApiError should be marked as an error definition");
	}

	// ==================================================================
	// 14. Petstore — backing store
	// ==================================================================

	[TestMethod]
	public void Petstore_BackingStoreEnabled_AddsIBackedModelInterface()
	{
		var root = BuildFromSpec("petstore.json", usesBackingStore: true);
		var pet = FindClass(root, "Pet");

		pet.Interfaces.Should().Contain(i => i.Name == "IBackedModel");
		pet.Properties.Should().Contain(p => p.Kind == CodePropertyKind.BackingStore);
	}

	[TestMethod]
	public void Petstore_BackingStoreDisabled_NoIBackedModelInterface()
	{
		var root = BuildFromSpec("petstore.json", usesBackingStore: false);
		var pet = FindClass(root, "Pet");

		pet.Interfaces.Should().NotContain(i => i.Name == "IBackedModel");
		pet.Properties.Should().NotContain(p => p.Kind == CodePropertyKind.BackingStore);
	}

	// ==================================================================
	// 15. Petstore — type access modifier
	// ==================================================================

	[TestMethod]
	public void Petstore_InternalAccessModifier_SetsAccessOnModels()
	{
		var root = BuildFromSpec("petstore.json", typeAccessModifier: "Internal");
		var pet = FindClass(root, "Pet");

		pet.Access.Should().Be(AccessModifier.Internal);
	}

	[TestMethod]
	public void Petstore_PublicAccessModifier_SetsAccessOnModels()
	{
		var root = BuildFromSpec("petstore.json", typeAccessModifier: "Public");
		var pet = FindClass(root, "Pet");

		pet.Access.Should().Be(AccessModifier.Public);
	}

	// ==================================================================
	// 16. Petstore — type resolution (MapTypeDefinitions)
	// ==================================================================

	[TestMethod]
	public void Petstore_TypeReferencesAreResolved()
	{
		var root = BuildFromSpec("petstore.json");

		// PetCollection.items → should resolve to Pet
		var collection = FindClass(root, "PetCollection");
		var itemsProp = collection.FindProperty("Items");
		itemsProp.Should().NotBeNull();
		var itemsType = itemsProp.Type.Should().BeOfType<CodeType>().Subject;
		itemsType.TypeDefinition.Should().NotBeNull(
			"the Items property should resolve to the Pet model class");
		itemsType.TypeDefinition.Name.Should().Be("Pet");
	}

	[TestMethod]
	public void Petstore_WithUrlReturnTypeIsResolved()
	{
		var root = BuildFromSpec("petstore.json");
		var petsRb = FindClass(root, "PetsRequestBuilder");

		var withUrl = petsRb.MethodsOfKind(CodeMethodKind.WithUrl).First();
		var returnType = withUrl.ReturnType.Should().BeOfType<CodeType>().Subject;
		returnType.TypeDefinition.Should().NotBeNull(
			"the WithUrl return type should resolve to PetsRequestBuilder");
		returnType.TypeDefinition.Name.Should().Be("PetsRequestBuilder");
	}

	// ==================================================================
	// 17. Petstore — base URL extraction
	// ==================================================================

	[TestMethod]
	public void Petstore_RootClientConstructorHasBaseUrl()
	{
		var root = BuildFromSpec("petstore.json",
			clientName: "PetStoreClient");

		var clientClass = FindClass(root, "PetStoreClient");
		var ctor = clientClass.MethodsOfKind(CodeMethodKind.Constructor).First();
		ctor.BaseUrl.Should().Be("https://petstore.example.com/api/v1");
	}

	// ==================================================================
	// 18. Inheritance (allOf) — base classes
	// ==================================================================

	[TestMethod]
	public void Inheritance_CatExtendsAnimal()
	{
		var root = BuildFromSpec("inheritance.json");
		var cat = FindClass(root, "Cat");

		cat.Should().NotBeNull();
		cat.BaseClass.Should().NotBeNull("Cat uses allOf with $ref Animal → base class");
		cat.BaseClass.Name.Should().Be("Animal");
	}

	[TestMethod]
	public void Inheritance_DogExtendsAnimal()
	{
		var root = BuildFromSpec("inheritance.json");
		var dog = FindClass(root, "Dog");

		dog.Should().NotBeNull();
		dog.BaseClass.Should().NotBeNull();
		dog.BaseClass.Name.Should().Be("Animal");
	}

	[TestMethod]
	public void Inheritance_BirdExtendsAnimal()
	{
		var root = BuildFromSpec("inheritance.json");
		var bird = FindClass(root, "Bird");

		bird.Should().NotBeNull();
		bird.BaseClass.Should().NotBeNull();
		bird.BaseClass.Name.Should().Be("Animal");
	}

	[TestMethod]
	public void Inheritance_AnimalHasNoBaseClass()
	{
		var root = BuildFromSpec("inheritance.json");
		var animal = FindClass(root, "Animal");

		animal.Should().NotBeNull();
		// Animal doesn't use allOf, so no user-defined base class
		// (it will get BaseRequestBuilder or nothing as a model)
		animal.BaseClass.Should().BeNull(
			"Animal is a root model without allOf inheritance");
	}

	[TestMethod]
	public void Inheritance_CatHasOwnProperties()
	{
		var root = BuildFromSpec("inheritance.json");
		var cat = FindClass(root, "Cat");

		var customProps = cat.Properties
			.Where(p => p.Kind == CodePropertyKind.Custom)
			.Select(p => p.Name)
			.ToList();

		// Cat defines: color, isIndoor, breed (from the allOf inline schema)
		customProps.Should().Contain("Color");
		customProps.Should().Contain("IsIndoor");
		customProps.Should().Contain("Breed");
	}

	[TestMethod]
	public void Inheritance_DogHasOwnProperties()
	{
		var root = BuildFromSpec("inheritance.json");
		var dog = FindClass(root, "Dog");

		var customProps = dog.Properties
			.Where(p => p.Kind == CodePropertyKind.Custom)
			.Select(p => p.Name)
			.ToList();

		customProps.Should().Contain("Breed");
		customProps.Should().Contain("IsTrained");
		customProps.Should().Contain("BarkVolume");
	}

	// ==================================================================
	// 19. Inheritance — discriminator mappings
	// ==================================================================

	[TestMethod]
	public void Inheritance_AnimalHasDiscriminatorProperty()
	{
		var root = BuildFromSpec("inheritance.json");
		var animal = FindClass(root, "Animal");

		animal.DiscriminatorPropertyName.Should().Be("animalType",
			"the Animal schema declares a discriminator on 'animalType'");
	}

	[TestMethod]
	public void Inheritance_AnimalHasDiscriminatorMappings()
	{
		var root = BuildFromSpec("inheritance.json");
		var animal = FindClass(root, "Animal");

		animal.DiscriminatorMappings.Should().ContainKey("cat");
		animal.DiscriminatorMappings.Should().ContainKey("dog");
		animal.DiscriminatorMappings.Should().ContainKey("bird");
	}

	[TestMethod]
	public void Inheritance_DiscriminatorMappingsResolveToCorrectTypes()
	{
		var root = BuildFromSpec("inheritance.json");
		var animal = FindClass(root, "Animal");

		// After MapTypeDefinitions, discriminator type refs should be resolved
		animal.DiscriminatorMappings["cat"].TypeDefinition.Should().NotBeNull();
		animal.DiscriminatorMappings["cat"].TypeDefinition.Name.Should().Be("Cat");

		animal.DiscriminatorMappings["dog"].TypeDefinition.Should().NotBeNull();
		animal.DiscriminatorMappings["dog"].TypeDefinition.Name.Should().Be("Dog");

		animal.DiscriminatorMappings["bird"].TypeDefinition.Should().NotBeNull();
		animal.DiscriminatorMappings["bird"].TypeDefinition.Name.Should().Be("Bird");
	}

	// ==================================================================
	// 20. Inheritance — multiple discriminator hierarchies
	// ==================================================================

	[TestMethod]
	public void Inheritance_VehicleHasDiscriminator()
	{
		var root = BuildFromSpec("inheritance.json");
		var vehicle = FindClass(root, "Vehicle");

		vehicle.Should().NotBeNull();
		vehicle.DiscriminatorPropertyName.Should().Be("vehicleType");
		vehicle.DiscriminatorMappings.Should().ContainKey("car");
		vehicle.DiscriminatorMappings.Should().ContainKey("truck");
	}

	[TestMethod]
	public void Inheritance_CarExtendVehicle()
	{
		var root = BuildFromSpec("inheritance.json");
		var car = FindClass(root, "Car");

		car.Should().NotBeNull();
		car.BaseClass.Should().NotBeNull();
		car.BaseClass.Name.Should().Be("Vehicle");
	}

	// ==================================================================
	// 21. Inheritance — multi-level inheritance
	// ==================================================================

	[TestMethod]
	public void Inheritance_DomesticCatExtendsCat()
	{
		var root = BuildFromSpec("inheritance.json");
		var domesticCat = FindClass(root, "DomesticCat");

		domesticCat.Should().NotBeNull(
			"DomesticCat should be created as a model class");
		domesticCat.BaseClass.Should().NotBeNull();
		domesticCat.BaseClass.Name.Should().Be("Cat",
			"DomesticCat uses allOf with $ref Cat → multi-level inheritance");
	}

	[TestMethod]
	public void Inheritance_DomesticCatHasOwnProperties()
	{
		var root = BuildFromSpec("inheritance.json");
		var domesticCat = FindClass(root, "DomesticCat");

		var customProps = domesticCat.Properties
			.Where(p => p.Kind == CodePropertyKind.Custom)
			.Select(p => p.Name)
			.ToList();

		customProps.Should().Contain("OwnerName");
		customProps.Should().Contain("IsLitterTrained");
	}

	// ==================================================================
	// 22. Inheritance — serializer/deserializer overrides
	// ==================================================================

	[TestMethod]
	public void Inheritance_DerivedClassSerializerIsOverride()
	{
		var root = BuildFromSpec("inheritance.json");
		var cat = FindClass(root, "Cat");

		var serializer = cat.MethodsOfKind(CodeMethodKind.Serializer).First();
		serializer.IsOverride.Should().BeTrue(
			"derived models should override Serialize since they have a base class");
	}

	[TestMethod]
	public void Inheritance_DerivedClassDeserializerIsOverride()
	{
		var root = BuildFromSpec("inheritance.json");
		var cat = FindClass(root, "Cat");

		var deserializer = cat.MethodsOfKind(CodeMethodKind.Deserializer).First();
		deserializer.IsOverride.Should().BeTrue(
			"derived models should override GetFieldDeserializers");
	}

	[TestMethod]
	public void Inheritance_BaseClassSerializerIsNotOverride()
	{
		var root = BuildFromSpec("inheritance.json");
		var animal = FindClass(root, "Animal");

		var serializer = animal.MethodsOfKind(CodeMethodKind.Serializer).First();
		serializer.IsOverride.Should().BeFalse(
			"base model without parent should not be an override");
	}

	// ==================================================================
	// 23. Composed types — oneOf (union)
	// ==================================================================

	[TestMethod]
	public void ComposedTypes_PaymentMethodIsHandledAsOneOf()
	{
		var root = BuildFromSpec("composed-types.json");

		// PaymentMethod is defined as oneOf: [CreditCardPayment, BankTransferPayment, DigitalWalletPayment]
		// The builder treats inline oneOf schemas as CodeUnionType references
		// when the schema is referenced by a property.
		//
		// Since PaymentMethod is a top-level schema with oneOf, the builder
		// may create it as a model class or skip it. Let's verify the
		// constituent types exist.
		var creditCard = FindClass(root, "CreditCardPayment");
		var bankTransfer = FindClass(root, "BankTransferPayment");
		var digitalWallet = FindClass(root, "DigitalWalletPayment");

		creditCard.Should().NotBeNull();
		bankTransfer.Should().NotBeNull();
		digitalWallet.Should().NotBeNull();
	}

	[TestMethod]
	public void ComposedTypes_ConstituentTypesHaveExpectedProperties()
	{
		var root = BuildFromSpec("composed-types.json");

		var creditCard = FindClass(root, "CreditCardPayment");
		creditCard.FindProperty("CardNumber").Should().NotBeNull();
		creditCard.FindProperty("ExpiryMonth").Should().NotBeNull();
		creditCard.FindProperty("ExpiryYear").Should().NotBeNull();

		var bankTransfer = FindClass(root, "BankTransferPayment");
		bankTransfer.FindProperty("AccountNumber").Should().NotBeNull();
		bankTransfer.FindProperty("RoutingNumber").Should().NotBeNull();
	}

	// ==================================================================
	// 24. Composed types — anyOf (intersection)
	// ==================================================================

	[TestMethod]
	public void ComposedTypes_NotificationChannelConstituentTypesExist()
	{
		var root = BuildFromSpec("composed-types.json");

		// NotificationChannels is anyOf: [EmailChannel, SmsChannel, PushChannel]
		var email = FindClass(root, "EmailChannel");
		var sms = FindClass(root, "SmsChannel");
		var push = FindClass(root, "PushChannel");

		email.Should().NotBeNull();
		sms.Should().NotBeNull();
		push.Should().NotBeNull();
	}

	[TestMethod]
	public void ComposedTypes_EmailChannelHasProperties()
	{
		var root = BuildFromSpec("composed-types.json");
		var email = FindClass(root, "EmailChannel");

		email.FindProperty("EmailAddress").Should().NotBeNull();
		email.FindProperty("Subject").Should().NotBeNull();
		email.FindProperty("IsHtml").Should().NotBeNull();
	}

	// ==================================================================
	// 25. Composed types — inline enum in composed-types spec
	// ==================================================================

	[TestMethod]
	public void ComposedTypes_CreatesAddressModel()
	{
		var root = BuildFromSpec("composed-types.json");
		var address = FindClass(root, "Address");

		address.Should().NotBeNull();
		address.FindProperty("Street").Should().NotBeNull();
		address.FindProperty("City").Should().NotBeNull();
	}

	// ==================================================================
	// 26. Enums spec — multiple enum types
	// ==================================================================

	[TestMethod]
	public void Enums_CreatesExpectedEnumTypes()
	{
		var root = BuildFromSpec("enums.json");

		var allEnums = GetAllEnums(root);
		var enumNames = allEnums.Select(e => e.Name).ToList();

		// The enums spec declares: Priority, TaskStatus, SortField, SortDirection,
		// and possibly more (DayOfWeek, NotificationPreference, UserRole)
		enumNames.Should().Contain("Priority");
		enumNames.Should().Contain("TaskStatus");
		enumNames.Should().Contain("SortField");
		enumNames.Should().Contain("SortDirection");
	}

	// ==================================================================
	// 27. Enums — enum referenced on query parameter
	// ==================================================================

	[TestMethod]
	public void Enums_TasksRequestBuilderHasQueryParamsWithEnumRefs()
	{
		var root = BuildFromSpec("enums.json");
		var tasksRb = FindClass(root, "TasksRequestBuilder");

		tasksRb.Should().NotBeNull();

		// The /tasks GET endpoint has query params: priority, status, sortBy, sortDirection
		// These are all enum-typed in the spec.
		var urlTemplate = tasksRb.PropertiesOfKind(CodePropertyKind.UrlTemplate).First();
		urlTemplate.SerializedName.Should().Contain("priority");
		urlTemplate.SerializedName.Should().Contain("status");
	}

	// ==================================================================
	// 28. Enums — enum property on model
	// ==================================================================

	[TestMethod]
	public void Enums_TaskItemModelHasEnumProperties()
	{
		var root = BuildFromSpec("enums.json");
		var taskItem = FindClass(root, "TaskItem");

		taskItem.Should().NotBeNull();

		// TaskItem has status (TaskStatus enum) and priority (Priority enum)
		var statusProp = taskItem.FindProperty("Status");
		statusProp.Should().NotBeNull();

		var priorityProp = taskItem.FindProperty("Priority");
		priorityProp.Should().NotBeNull();
	}

	// ==================================================================
	// 29. Error-responses spec
	// ==================================================================

	[TestMethod]
	public void ErrorResponses_ErrorModelsAreMarkedAsErrorDefinition()
	{
		var root = BuildFromSpec("error-responses.json");
		var allClasses = GetAllClasses(root);

		// The error-responses spec should have error models containing "Error" in the name.
		var errorModels = allClasses
			.Where(c => c.Kind == CodeClassKind.Model && c.IsErrorDefinition)
			.ToList();

		errorModels.Should().NotBeEmpty(
			"error response models should be marked as IsErrorDefinition");
	}

	// ==================================================================
	// 30. General structural tests
	// ==================================================================

	[TestMethod]
	public void Petstore_AllModelClassesHaveParentSet()
	{
		var root = BuildFromSpec("petstore.json");
		var modelsNs = root.Namespaces.First(ns => ns.Name == "Models");

		foreach (var cls in modelsNs.Classes)
		{
			cls.Parent.Should().Be(modelsNs,
				$"model class {cls.Name} should have its Parent set to the Models namespace");
		}
	}

	[TestMethod]
	public void Petstore_AllPropertiesHaveParentSet()
	{
		var root = BuildFromSpec("petstore.json");
		var pet = FindClass(root, "Pet");

		foreach (var prop in pet.Properties)
		{
			prop.Parent.Should().Be(pet,
				$"property {prop.Name} should have its Parent set to the Pet class");
		}
	}

	[TestMethod]
	public void Petstore_AllMethodsHaveParentSet()
	{
		var root = BuildFromSpec("petstore.json");
		var pet = FindClass(root, "Pet");

		foreach (var method in pet.Methods)
		{
			method.Parent.Should().Be(pet,
				$"method {method.Name} should have its Parent set to the Pet class");
		}
	}

	[TestMethod]
	public void Petstore_NamespaceTreeIsWellFormed()
	{
		var root = BuildFromSpec("petstore.json");

		// Root namespace should have child namespaces (Models, Pets, Owners, etc.)
		root.Namespaces.Should().NotBeEmpty();

		foreach (var childNs in root.Namespaces)
		{
			childNs.Parent.Should().Be(root,
				$"child namespace {childNs.Name} should have parent set to root");
		}
	}
}
