using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;
using Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;
using Uno.Extensions.Http.Kiota.SourceGenerator.Emitter;
using Uno.Extensions.Http.Kiota.SourceGenerator.Refinement;

namespace Uno.Extensions.Http.Kiota.Generator.Tests.Phase2;

/// <summary>
/// Unit tests for the C# emitter sub-emitters, verifying that each emitter
/// produces correct C# code patterns from hand-built CodeDOM trees:
/// <list type="bullet">
///   <item><see cref="EnumEmitter"/> — simple and flags enum declarations</item>
///   <item><see cref="ClassDeclarationEmitter"/> — class signatures, attributes</item>
///   <item><see cref="PropertyEmitter"/> — auto-properties, nullable guards, navigation</item>
///   <item><see cref="ConstructorEmitter"/> — model constructors, request builder constructors</item>
///   <item><see cref="MethodEmitter"/> — indexers, executor methods, request generators</item>
///   <item><see cref="SerializerEmitter"/> — Serialize() method body</item>
///   <item><see cref="DeserializerEmitter"/> — GetFieldDeserializers() body</item>
///   <item><see cref="FactoryMethodEmitter"/> — CreateFromDiscriminatorValue</item>
///   <item><see cref="CSharpEmitter"/> — full tree walk producing hint-name + source pairs</item>
/// </list>
/// </summary>
[TestClass]
public class CSharpEmitterTests
{
	// ==================================================================
	// Shared config — default (non-backing-store, with additional data)
	// ==================================================================

	private static KiotaGeneratorConfig DefaultConfig => new KiotaGeneratorConfig(
		clientClassName: "PetStoreClient",
		clientNamespaceName: "TestApp.Client",
		usesBackingStore: false,
		includeAdditionalData: true,
		excludeBackwardCompatible: false,
		typeAccessModifier: "Public",
		includePatterns: ImmutableArray<string>.Empty,
		excludePatterns: ImmutableArray<string>.Empty);

	// ==================================================================
	// EnumEmitter tests
	// ==================================================================

	[TestMethod]
	public void EnumEmitter_SimpleEnum_EmitsGeneratedCodeAndEnumMemberAttributes()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new EnumEmitter(config);
		var writer = new CodeWriter();

		var en = new CodeEnum("PetStatus");
		en.AddOption(new CodeEnumOption("Available", "available"));
		en.AddOption(new CodeEnumOption("Pending", "pending"));
		en.AddOption(new CodeEnumOption("Sold", "sold"));

		// Act
		emitter.Emit(writer, en);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("[global::System.CodeDom.Compiler.GeneratedCode(\"Kiota\"");
		output.Should().Contain("public enum PetStatus");
		output.Should().Contain("[EnumMember(Value = \"available\")]");
		output.Should().Contain("Available,");
		output.Should().Contain("[EnumMember(Value = \"pending\")]");
		output.Should().Contain("Pending,");
		output.Should().Contain("[EnumMember(Value = \"sold\")]");
		output.Should().Contain("Sold,");
		output.Should().NotContain("[Flags]");
	}

	[TestMethod]
	public void EnumEmitter_FlagsEnum_EmitsFlagsAttributeAndExplicitValues()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new EnumEmitter(config);
		var writer = new CodeWriter();

		var en = new CodeEnum("Permissions", isFlags: true);
		en.AddOption(new CodeEnumOption("Read", "read") { Value = 1 });
		en.AddOption(new CodeEnumOption("Write", "write") { Value = 2 });
		en.AddOption(new CodeEnumOption("Execute", "execute") { Value = 4 });

		// Act
		emitter.Emit(writer, en);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("[Flags]");
		output.Should().Contain("public enum Permissions");
		output.Should().Contain("Read = 1,");
		output.Should().Contain("Write = 2,");
		output.Should().Contain("Execute = 4,");
	}

	[TestMethod]
	public void EnumEmitter_WithDescription_EmitsXmlDocSummary()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new EnumEmitter(config);
		var writer = new CodeWriter();

		var en = new CodeEnum("PetStatus")
		{
			Description = "The status of a pet in the store."
		};
		en.AddOption(new CodeEnumOption("Available", "available"));

		// Act
		emitter.Emit(writer, en);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("/// <summary>The status of a pet in the store.</summary>");
	}

	[TestMethod]
	public void EnumEmitter_NullWriter_ThrowsArgumentNullException()
	{
		var config = DefaultConfig;
		var emitter = new EnumEmitter(config);
		var en = new CodeEnum("Test");

		var act = () => emitter.Emit(null!, en);
		act.Should().Throw<ArgumentNullException>().WithParameterName("writer");
	}

	[TestMethod]
	public void EnumEmitter_NullEnum_ThrowsArgumentNullException()
	{
		var config = DefaultConfig;
		var emitter = new EnumEmitter(config);
		var writer = new CodeWriter();

		var act = () => emitter.Emit(writer, null!);
		act.Should().Throw<ArgumentNullException>().WithParameterName("en");
	}

	// ==================================================================
	// ClassDeclarationEmitter tests
	// ==================================================================

	[TestMethod]
	public void ClassDeclarationEmitter_ModelClass_EmitsPartialClassWithGeneratedCodeAttribute()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new ClassDeclarationEmitter(config);
		var writer = new CodeWriter();

		var cls = new CodeClass("Pet", CodeClassKind.Model);
		cls.AddInterface(new CodeType("IParsable", isExternal: true));

		// Act
		emitter.EmitClassOpen(writer, cls);
		emitter.EmitClassClose(writer);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("[global::System.CodeDom.Compiler.GeneratedCode(\"Kiota\"");
		output.Should().Contain("public partial class Pet");
		output.Should().Contain("{");
		output.Should().Contain("}");
	}

	[TestMethod]
	public void ClassDeclarationEmitter_RequestConfigurationClass_EmitsObsoleteAttribute()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new ClassDeclarationEmitter(config);
		var writer = new CodeWriter();

		var cls = new CodeClass("GetRequestConfiguration", CodeClassKind.RequestConfiguration);

		// Act
		emitter.EmitClassOpen(writer, cls);
		emitter.EmitClassClose(writer);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("[Obsolete(");
		output.Should().Contain("This class is deprecated");
	}

	[TestMethod]
	public void ClassDeclarationEmitter_ClassWithBaseAndInterfaces_EmitsInheritanceList()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new ClassDeclarationEmitter(config);
		var writer = new CodeWriter();

		var cls = new CodeClass("Pet", CodeClassKind.Model);
		cls.BaseClass = new CodeType("Animal", isExternal: false);
		cls.AddInterface(new CodeType("IParsable", isExternal: true));
		cls.AddInterface(new CodeType("IAdditionalDataHolder", isExternal: true));

		// Act
		emitter.EmitClassOpen(writer, cls);
		emitter.EmitClassClose(writer);
		var output = writer.ToString();

		// Assert — must contain base class and interfaces separated by commas
		output.Should().Contain("public partial class Pet :");
		output.Should().Contain("IParsable");
		output.Should().Contain("IAdditionalDataHolder");
	}

	[TestMethod]
	public void ClassDeclarationEmitter_FileStart_EmitsHeaderUsingsAndNamespace()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new ClassDeclarationEmitter(config);
		var writer = new CodeWriter();

		// Act
		emitter.EmitFileStart(writer, CSharpConventionService.ModelBaseUsings, "TestApp.Models");
		emitter.EmitFileEnd(writer);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("// <auto-generated/>");
		output.Should().Contain("#pragma warning disable CS0618");
		output.Should().Contain("using Microsoft.Kiota.Abstractions.Serialization;");
		output.Should().Contain("namespace TestApp.Models");
		output.Should().Contain("{");
		output.Should().Contain("}");
	}

	[TestMethod]
	public void ClassDeclarationEmitter_GetUsingsForClass_ReturnsModelUsingsForModel()
	{
		var config = DefaultConfig;
		var emitter = new ClassDeclarationEmitter(config);
		var cls = new CodeClass("Pet", CodeClassKind.Model);

		var usings = emitter.GetUsingsForClass(cls);

		usings.Should().BeEquivalentTo(CSharpConventionService.ModelBaseUsings);
	}

	[TestMethod]
	public void ClassDeclarationEmitter_GetUsingsForClass_ReturnsRequestBuilderUsingsForNonRoot()
	{
		var config = DefaultConfig;
		var emitter = new ClassDeclarationEmitter(config);
		var cls = new CodeClass("PetsRequestBuilder", CodeClassKind.RequestBuilder);

		var usings = emitter.GetUsingsForClass(cls);

		usings.Should().BeEquivalentTo(CSharpConventionService.RequestBuilderUsings);
	}

	[TestMethod]
	public void ClassDeclarationEmitter_GetUsingsForClass_ReturnsClientRootUsingsForRootClient()
	{
		var config = DefaultConfig;
		var emitter = new ClassDeclarationEmitter(config);
		// Root client class name matches config.ClientClassName
		var cls = new CodeClass("PetStoreClient", CodeClassKind.RequestBuilder);

		var usings = emitter.GetUsingsForClass(cls);

		usings.Should().BeEquivalentTo(CSharpConventionService.ClientRootUsings);
	}

	// ==================================================================
	// PropertyEmitter tests
	// ==================================================================

	[TestMethod]
	public void PropertyEmitter_CustomStringProperty_EmitsNullableGuardedAutoProperty()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new PropertyEmitter(config);
		var writer = new CodeWriter();

		var prop = new CodeProperty("Name", CodePropertyKind.Custom,
			new CodeType("string") { IsNullable = true });

		var cls = new CodeClass("Pet", CodeClassKind.Model);
		cls.AddProperty(prop);

		// Act
		emitter.Emit(writer, prop, cls);
		var output = writer.ToString();

		// Assert — string properties need nullable guards
		output.Should().Contain("#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER");
		output.Should().Contain("#nullable enable");
		output.Should().Contain("string?");
		output.Should().Contain("#nullable restore");
		output.Should().Contain("#else");
		output.Should().Contain("#endif");
		output.Should().Contain("{ get; set; }");
	}

	[TestMethod]
	public void PropertyEmitter_ValueTypeProperty_EmitsNullableQuestionMarkWithoutGuard()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new PropertyEmitter(config);
		var writer = new CodeWriter();

		var prop = new CodeProperty("Age", CodePropertyKind.Custom,
			new CodeType("int") { IsNullable = true });

		var cls = new CodeClass("Pet", CodeClassKind.Model);
		cls.AddProperty(prop);

		// Act
		emitter.Emit(writer, prop, cls);
		var output = writer.ToString();

		// Assert — value types do NOT need #if guards
		output.Should().NotContain("#if");
		output.Should().Contain("int?");
		output.Should().Contain("{ get; set; }");
	}

	[TestMethod]
	public void PropertyEmitter_NavigationProperty_EmitsGetterWithConstructor()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new PropertyEmitter(config);
		var writer = new CodeWriter();

		var returnType = new CodeType("PetsRequestBuilder") { IsExternal = false };
		var prop = new CodeProperty("Pets", CodePropertyKind.Navigation, returnType);

		var cls = new CodeClass("PetStoreClient", CodeClassKind.RequestBuilder);
		cls.AddProperty(prop);

		// Act
		emitter.Emit(writer, prop, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("Pets");
		output.Should().Contain("get =>");
		output.Should().Contain("(PathParameters, RequestAdapter)");
	}

	[TestMethod]
	public void PropertyEmitter_QueryParameterProperty_EmitsQueryParameterAttribute()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new PropertyEmitter(config);
		var writer = new CodeWriter();

		var prop = new CodeProperty("Filter", CodePropertyKind.QueryParameter,
			new CodeType("string") { IsNullable = true });
		prop.SerializedName = "$filter";

		var cls = new CodeClass("QueryParameters", CodeClassKind.QueryParameters);
		cls.AddProperty(prop);

		// Act
		emitter.Emit(writer, prop, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("[QueryParameter(\"$filter\")]");
	}

	[TestMethod]
	public void PropertyEmitter_BackingStoreMode_EmitsDelegateGetterSetter()
	{
		// Arrange
		var config = new KiotaGeneratorConfig(
			clientClassName: "PetStoreClient",
			clientNamespaceName: "TestApp.Client",
			usesBackingStore: true,
			includeAdditionalData: true,
			excludeBackwardCompatible: false,
			typeAccessModifier: "Public",
			includePatterns: ImmutableArray<string>.Empty,
			excludePatterns: ImmutableArray<string>.Empty);

		var emitter = new PropertyEmitter(config);
		var writer = new CodeWriter();

		var prop = new CodeProperty("Name", CodePropertyKind.Custom,
			new CodeType("string") { IsNullable = true });
		prop.SerializedName = "name";

		var cls = new CodeClass("Pet", CodeClassKind.Model);
		cls.AddProperty(prop);

		// Act
		emitter.Emit(writer, prop, cls);
		var output = writer.ToString();

		// Assert — backing store delegate pattern
		output.Should().Contain("BackingStore?.Get<");
		output.Should().Contain("BackingStore?.Set(");
	}

	[TestMethod]
	public void PropertyEmitter_UrlTemplateProperty_EmitsReadOnlyWithStringDefault()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new PropertyEmitter(config);
		var writer = new CodeWriter();

		var prop = new CodeProperty("UrlTemplate", CodePropertyKind.UrlTemplate,
			new CodeType("string") { IsNullable = true })
		{
			DefaultValue = "{+baseurl}/pets{?limit}",
			IsReadOnly = true,
		};

		var cls = new CodeClass("PetsRequestBuilder", CodeClassKind.RequestBuilder);
		cls.AddProperty(prop);

		// Act
		emitter.Emit(writer, prop, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("{ get; }");
		output.Should().Contain("= \"{+baseurl}/pets{?limit}\"");
	}

	// ==================================================================
	// ConstructorEmitter tests
	// ==================================================================

	[TestMethod]
	public void ConstructorEmitter_ModelConstructor_EmitsParameterlessConstructor()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new ConstructorEmitter(config);
		var writer = new CodeWriter();

		var ns = new CodeNamespace("TestApp.Client.Models");
		var cls = new CodeClass("Pet", CodeClassKind.Model);
		ns.AddClass(cls);

		var ctor = new CodeMethod(".ctor", CodeMethodKind.Constructor);
		cls.AddMethod(ctor);

		// Act
		emitter.Emit(writer, ctor, cls);
		var output = writer.ToString();

		// Assert — model constructors are emitted for model classes.
		// The constructor should be present (may be empty or initialize AdditionalData).
		output.Should().Contain("Pet(");
	}

	[TestMethod]
	public void ConstructorEmitter_RequestBuilderPathParametersConstructor_EmitsBaseCall()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new ConstructorEmitter(config);
		var writer = new CodeWriter();

		var ns = new CodeNamespace("TestApp.Client");
		var cls = new CodeClass("PetsRequestBuilder", CodeClassKind.RequestBuilder);
		ns.AddClass(cls);

		// UrlTemplate property needed by the constructor emitter
		cls.AddProperty(new CodeProperty("UrlTemplate", CodePropertyKind.UrlTemplate,
			new CodeType("string")) { DefaultValue = "{+baseurl}/pets" });

		var ctor = new CodeMethod(".ctor", CodeMethodKind.Constructor);
		ctor.AddParameter(new CodeParameter("pathParameters", CodeParameterKind.Path,
			new CodeType("Dictionary<string, object>", isExternal: true)));
		ctor.AddParameter(new CodeParameter("requestAdapter", CodeParameterKind.RequestAdapter,
			new CodeType("IRequestAdapter", isExternal: true)));
		cls.AddMethod(ctor);

		// Act
		emitter.Emit(writer, ctor, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("PetsRequestBuilder(");
		output.Should().Contain(": base(requestAdapter,");
		output.Should().Contain("pathParameters)");
	}

	[TestMethod]
	public void ConstructorEmitter_RootClientConstructor_EmitsSerializerRegistrations()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new ConstructorEmitter(config);
		var writer = new CodeWriter();

		var ns = new CodeNamespace("TestApp.Client");
		var cls = new CodeClass("PetStoreClient", CodeClassKind.RequestBuilder);
		ns.AddClass(cls);

		cls.AddProperty(new CodeProperty("UrlTemplate", CodePropertyKind.UrlTemplate,
			new CodeType("string")) { DefaultValue = "{+baseurl}" });

		var ctor = new CodeMethod(".ctor", CodeMethodKind.Constructor);
		ctor.AddParameter(new CodeParameter("requestAdapter", CodeParameterKind.RequestAdapter,
			new CodeType("IRequestAdapter", isExternal: true)));
		ctor.BaseUrl = "https://petstore.example.com/v1";
		cls.AddMethod(ctor);

		// Act
		emitter.Emit(writer, ctor, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("ApiClientBuilder.RegisterDefaultSerializer<JsonSerializationWriterFactory>();");
		output.Should().Contain("ApiClientBuilder.RegisterDefaultDeserializer<JsonParseNodeFactory>();");
		output.Should().Contain("RequestAdapter.BaseUrl");
		output.Should().Contain("PathParameters.TryAdd(\"baseurl\"");
	}

	// ==================================================================
	// MethodEmitter tests
	// ==================================================================

	[TestMethod]
	public void MethodEmitter_Indexer_EmitsIndexerWithPathParameterHandling()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new MethodEmitter(config);
		var writer = new CodeWriter();

		var childType = new CodeType("PetsItemRequestBuilder");
		var indexer = new CodeIndexer("ByPetId", childType, "petId", "{+petId}");

		var cls = new CodeClass("PetsRequestBuilder", CodeClassKind.RequestBuilder);
		cls.AddIndexer(indexer);

		// Act
		emitter.EmitIndexer(writer, indexer, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("this[string position]");
		output.Should().Contain("get");
		output.Should().Contain("new Dictionary<string, object>(PathParameters)");
		output.Should().Contain("urlTplParams.Add(");
		output.Should().Contain("return new");
	}

	[TestMethod]
	public void MethodEmitter_ExecutorMethod_EmitsAsyncMethodWithReturnType()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new MethodEmitter(config);
		var writer = new CodeWriter();

		var returnType = new CodeType("Pet") { IsNullable = true };
		var method = new CodeMethod("GetAsync", CodeMethodKind.RequestExecutor, returnType)
		{
			IsAsync = true,
			HttpMethod = "GET",
		};
		method.AddAcceptedResponseType("application/json");
		method.AddParameter(new CodeParameter("cancellationToken", CodeParameterKind.Cancellation,
			new CodeType("CancellationToken", isExternal: true)) { Optional = true, DefaultValue = "default" });

		var cls = new CodeClass("PetsItemRequestBuilder", CodeClassKind.RequestBuilder);
		cls.AddMethod(method);

		// Act
		emitter.EmitExecutorMethod(writer, method, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("async");
		output.Should().Contain("GetAsync");
		output.Should().Contain("cancellationToken");
	}

	[TestMethod]
	public void MethodEmitter_RequestGenerator_EmitsRequestInformationBuilder()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new MethodEmitter(config);
		var writer = new CodeWriter();

		var method = new CodeMethod("ToGetRequestInformation", CodeMethodKind.RequestGenerator,
			new CodeType("RequestInformation", isExternal: true))
		{
			HttpMethod = "GET",
		};
		method.AddAcceptedResponseType("application/json");

		var cls = new CodeClass("PetsRequestBuilder", CodeClassKind.RequestBuilder);
		cls.AddMethod(method);

		// Act
		emitter.EmitRequestGeneratorMethod(writer, method, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("ToGetRequestInformation");
		output.Should().Contain("RequestInformation");
	}

	[TestMethod]
	public void MethodEmitter_WithUrlMethod_EmitsNewInstanceCreation()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new MethodEmitter(config);
		var writer = new CodeWriter();

		var method = new CodeMethod("WithUrl", CodeMethodKind.WithUrl,
			new CodeType("PetsRequestBuilder"));
		method.AddParameter(new CodeParameter("rawUrl", CodeParameterKind.RawUrl,
			new CodeType("string", isExternal: true)));

		var cls = new CodeClass("PetsRequestBuilder", CodeClassKind.RequestBuilder);
		cls.AddMethod(method);

		// Act
		emitter.EmitWithUrlMethod(writer, method, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("WithUrl");
		output.Should().Contain("rawUrl");
	}

	// ==================================================================
	// SerializerEmitter tests
	// ==================================================================

	[TestMethod]
	public void SerializerEmitter_ModelWithStringProperty_EmitsWriteStringValueCall()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new SerializerEmitter(config);
		var writer = new CodeWriter();

		var ns = new CodeNamespace("TestApp.Models");
		var cls = new CodeClass("Pet", CodeClassKind.Model);
		ns.AddClass(cls);

		cls.AddProperty(new CodeProperty("Name", CodePropertyKind.Custom,
			new CodeType("string") { IsNullable = true })
		{
			SerializedName = "name"
		});

		var serMethod = new CodeMethod("Serialize", CodeMethodKind.Serializer);
		serMethod.AddParameter(new CodeParameter("writer", CodeParameterKind.Body,
			new CodeType("ISerializationWriter", isExternal: true)));
		cls.AddMethod(serMethod);

		// Act
		emitter.Emit(writer, serMethod, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("Serialize");
		output.Should().Contain("writer");
		output.Should().Contain("WriteStringValue");
		output.Should().Contain("\"name\"");
	}

	[TestMethod]
	public void SerializerEmitter_ModelWithIntProperty_EmitsWriteIntValueCall()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new SerializerEmitter(config);
		var writer = new CodeWriter();

		var ns = new CodeNamespace("TestApp.Models");
		var cls = new CodeClass("Pet", CodeClassKind.Model);
		ns.AddClass(cls);

		cls.AddProperty(new CodeProperty("Age", CodePropertyKind.Custom,
			new CodeType("int") { IsNullable = true })
		{
			SerializedName = "age"
		});

		var serMethod = new CodeMethod("Serialize", CodeMethodKind.Serializer);
		serMethod.AddParameter(new CodeParameter("writer", CodeParameterKind.Body,
			new CodeType("ISerializationWriter", isExternal: true)));
		cls.AddMethod(serMethod);

		// Act
		emitter.Emit(writer, serMethod, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("WriteIntValue");
		output.Should().Contain("\"age\"");
	}

	[TestMethod]
	public void SerializerEmitter_NullGuard_EmitsArgumentNullCheck()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new SerializerEmitter(config);
		var writer = new CodeWriter();

		var ns = new CodeNamespace("TestApp.Models");
		var cls = new CodeClass("Pet", CodeClassKind.Model);
		ns.AddClass(cls);

		var serMethod = new CodeMethod("Serialize", CodeMethodKind.Serializer);
		serMethod.AddParameter(new CodeParameter("writer", CodeParameterKind.Body,
			new CodeType("ISerializationWriter", isExternal: true)));
		cls.AddMethod(serMethod);

		// Act
		emitter.Emit(writer, serMethod, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("ArgumentNullException");
	}

	// ==================================================================
	// DeserializerEmitter tests
	// ==================================================================

	[TestMethod]
	public void DeserializerEmitter_ModelWithStringProperty_EmitsGetStringValueCall()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new DeserializerEmitter(config);
		var writer = new CodeWriter();

		var ns = new CodeNamespace("TestApp.Models");
		var cls = new CodeClass("Pet", CodeClassKind.Model);
		ns.AddClass(cls);

		cls.AddProperty(new CodeProperty("Name", CodePropertyKind.Custom,
			new CodeType("string") { IsNullable = true })
		{
			SerializedName = "name"
		});

		var desMethod = new CodeMethod("GetFieldDeserializers", CodeMethodKind.Deserializer);
		cls.AddMethod(desMethod);

		// Act
		emitter.Emit(writer, desMethod, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("GetFieldDeserializers");
		output.Should().Contain("\"name\"");
		output.Should().Contain("GetStringValue");
	}

	[TestMethod]
	public void DeserializerEmitter_ModelWithIntProperty_EmitsGetIntValueCall()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new DeserializerEmitter(config);
		var writer = new CodeWriter();

		var ns = new CodeNamespace("TestApp.Models");
		var cls = new CodeClass("Pet", CodeClassKind.Model);
		ns.AddClass(cls);

		cls.AddProperty(new CodeProperty("Age", CodePropertyKind.Custom,
			new CodeType("int") { IsNullable = true })
		{
			SerializedName = "age"
		});

		var desMethod = new CodeMethod("GetFieldDeserializers", CodeMethodKind.Deserializer);
		cls.AddMethod(desMethod);

		// Act
		emitter.Emit(writer, desMethod, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("\"age\"");
		output.Should().Contain("GetIntValue");
	}

	[TestMethod]
	public void DeserializerEmitter_ReturnsDictionaryType()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new DeserializerEmitter(config);
		var writer = new CodeWriter();

		var ns = new CodeNamespace("TestApp.Models");
		var cls = new CodeClass("Pet", CodeClassKind.Model);
		ns.AddClass(cls);

		var desMethod = new CodeMethod("GetFieldDeserializers", CodeMethodKind.Deserializer);
		cls.AddMethod(desMethod);

		// Act
		emitter.Emit(writer, desMethod, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("IDictionary<string, Action<IParseNode>>");
	}

	// ==================================================================
	// FactoryMethodEmitter tests
	// ==================================================================

	[TestMethod]
	public void FactoryMethodEmitter_SimpleModel_EmitsCreateWithNewInstance()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new FactoryMethodEmitter(config);
		var writer = new CodeWriter();

		var ns = new CodeNamespace("TestApp.Client.Models");
		var cls = new CodeClass("Pet", CodeClassKind.Model);
		ns.AddClass(cls);

		var factoryMethod = new CodeMethod("CreateFromDiscriminatorValue", CodeMethodKind.Factory)
		{
			IsStatic = true
		};
		factoryMethod.AddParameter(new CodeParameter("parseNode", CodeParameterKind.Path,
			new CodeType("IParseNode", isExternal: true)));
		cls.AddMethod(factoryMethod);

		// Act
		emitter.Emit(writer, factoryMethod, cls);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("CreateFromDiscriminatorValue");
		output.Should().Contain("static");
		output.Should().Contain("new");
	}

	[TestMethod]
	public void FactoryMethodEmitter_DiscriminatedModel_EmitsSwitchStatement()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new FactoryMethodEmitter(config);
		var writer = new CodeWriter();

		var ns = new CodeNamespace("TestApp.Client.Models");
		var baseClass = new CodeClass("Animal", CodeClassKind.Model);
		ns.AddClass(baseClass);
		baseClass.DiscriminatorPropertyName = "@odata.type";

		var catType = new CodeType("Cat") { IsExternal = false };
		var dogType = new CodeType("Dog") { IsExternal = false };
		baseClass.AddDiscriminatorMapping("#cat", catType);
		baseClass.AddDiscriminatorMapping("#dog", dogType);

		var factoryMethod = new CodeMethod("CreateFromDiscriminatorValue", CodeMethodKind.Factory)
		{
			IsStatic = true
		};
		factoryMethod.AddParameter(new CodeParameter("parseNode", CodeParameterKind.Path,
			new CodeType("IParseNode", isExternal: true)));
		baseClass.AddMethod(factoryMethod);

		// Act
		emitter.Emit(writer, factoryMethod, baseClass);
		var output = writer.ToString();

		// Assert
		output.Should().Contain("CreateFromDiscriminatorValue");
		output.Should().Contain("@odata.type");
		output.Should().Contain("#cat");
		output.Should().Contain("#dog");
	}

	// ==================================================================
	// CSharpEmitter (full pipeline) tests
	// ==================================================================

	[TestMethod]
	public void CSharpEmitter_EmptyNamespace_ReturnsNoOutput()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new CSharpEmitter(config);
		var ns = new CodeNamespace("TestApp");

		// Act
		var results = emitter.Emit(ns).ToList();

		// Assert
		results.Should().BeEmpty();
	}

	[TestMethod]
	public void CSharpEmitter_SingleEnum_EmitsOneFile()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new CSharpEmitter(config);

		var root = new CodeNamespace("TestApp");
		var modelsNs = new CodeNamespace("Models");
		root.AddNamespace(modelsNs);

		var en = new CodeEnum("PetStatus");
		en.AddOption(new CodeEnumOption("Available", "available"));
		en.AddOption(new CodeEnumOption("Sold", "sold"));
		modelsNs.AddEnum(en);

		// Act
		var results = emitter.Emit(root).ToList();

		// Assert
		results.Should().HaveCount(1);
		results[0].HintName.Should().Contain("PetStatus");
		results[0].HintName.Should().EndWith(".g.cs");
		results[0].Source.Should().Contain("// <auto-generated/>");
		results[0].Source.Should().Contain("enum PetStatus");
		results[0].Source.Should().Contain("[EnumMember(Value = \"available\")]");
	}

	[TestMethod]
	public void CSharpEmitter_SingleModelClass_EmitsOneFileWithExpectedStructure()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new CSharpEmitter(config);

		var root = new CodeNamespace("TestApp");
		var modelsNs = new CodeNamespace("Models");
		root.AddNamespace(modelsNs);

		var cls = new CodeClass("Pet", CodeClassKind.Model);
		cls.AddInterface(new CodeType("IParsable", isExternal: true));

		cls.AddProperty(new CodeProperty("Name", CodePropertyKind.Custom,
			new CodeType("string") { IsNullable = true })
		{
			SerializedName = "name"
		});

		modelsNs.AddClass(cls);

		// Act
		var results = emitter.Emit(root).ToList();

		// Assert
		results.Should().HaveCount(1);
		results[0].HintName.Should().Contain("Pet");

		var source = results[0].Source;
		source.Should().Contain("// <auto-generated/>");
		source.Should().Contain("#pragma warning disable CS0618");
		source.Should().Contain("using Microsoft.Kiota.Abstractions.Serialization;");
		source.Should().Contain("namespace ");
		source.Should().Contain("[global::System.CodeDom.Compiler.GeneratedCode(\"Kiota\"");
		source.Should().Contain("public partial class Pet");
		source.Should().Contain("Name");
	}

	[TestMethod]
	public void CSharpEmitter_MultipleTypesInMultipleNamespaces_EmitsAllFiles()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new CSharpEmitter(config);

		var root = new CodeNamespace("TestApp");

		// Models namespace with a class and an enum.
		var modelsNs = new CodeNamespace("Models");
		root.AddNamespace(modelsNs);

		var petClass = new CodeClass("Pet", CodeClassKind.Model);
		modelsNs.AddClass(petClass);

		var statusEnum = new CodeEnum("PetStatus");
		statusEnum.AddOption(new CodeEnumOption("Available", "available"));
		modelsNs.AddEnum(statusEnum);

		// Client namespace with a request builder.
		var clientNs = new CodeNamespace("Client");
		root.AddNamespace(clientNs);

		var rbClass = new CodeClass("PetsRequestBuilder", CodeClassKind.RequestBuilder);
		clientNs.AddClass(rbClass);

		// Act
		var results = emitter.Emit(root).ToList();

		// Assert — should produce 3 files total
		results.Should().HaveCount(3);
		results.Select(r => r.HintName).Should().Contain(h => h.Contains("Pet"));
		results.Select(r => r.HintName).Should().Contain(h => h.Contains("PetStatus"));
		results.Select(r => r.HintName).Should().Contain(h => h.Contains("PetsRequestBuilder"));
	}

	[TestMethod]
	public void CSharpEmitter_NullRoot_ThrowsArgumentNullException()
	{
		var config = DefaultConfig;
		var emitter = new CSharpEmitter(config);

		var act = () => emitter.Emit(null!);
		act.Should().Throw<ArgumentNullException>().WithParameterName("root");
	}

	[TestMethod]
	public void CSharpEmitter_ClassWithInnerClasses_EmitsNestedTypes()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new CSharpEmitter(config);

		var root = new CodeNamespace("TestApp");
		var clientNs = new CodeNamespace("Client");
		root.AddNamespace(clientNs);

		var rbClass = new CodeClass("PetsRequestBuilder", CodeClassKind.RequestBuilder);

		// Add inner QueryParameters class
		var queryParams = new CodeClass("GetQueryParameters", CodeClassKind.QueryParameters);
		queryParams.AddProperty(new CodeProperty("Limit", CodePropertyKind.QueryParameter,
			new CodeType("int") { IsNullable = true }));
		rbClass.AddInnerClass(queryParams);

		clientNs.AddClass(rbClass);

		// Act
		var results = emitter.Emit(root).ToList();

		// Assert — only 1 file for the outer class; inner classes are nested
		results.Should().HaveCount(1);
		var source = results[0].Source;
		source.Should().Contain("PetsRequestBuilder");
		source.Should().Contain("GetQueryParameters");
		source.Should().Contain("Limit");
	}

	[TestMethod]
	public void CSharpEmitter_ModelWithSerializerAndDeserializer_EmitsCompleteMethods()
	{
		// Arrange
		var config = DefaultConfig;
		var emitter = new CSharpEmitter(config);

		var root = new CodeNamespace("TestApp");
		var modelsNs = new CodeNamespace("Models");
		root.AddNamespace(modelsNs);

		var cls = new CodeClass("Pet", CodeClassKind.Model);
		cls.AddInterface(new CodeType("IParsable", isExternal: true));

		// Add property
		cls.AddProperty(new CodeProperty("Name", CodePropertyKind.Custom,
			new CodeType("string") { IsNullable = true })
		{
			SerializedName = "name"
		});

		// Add serializer
		var serMethod = new CodeMethod("Serialize", CodeMethodKind.Serializer);
		serMethod.AddParameter(new CodeParameter("writer", CodeParameterKind.Body,
			new CodeType("ISerializationWriter", isExternal: true)));
		cls.AddMethod(serMethod);

		// Add deserializer
		var desMethod = new CodeMethod("GetFieldDeserializers", CodeMethodKind.Deserializer);
		cls.AddMethod(desMethod);

		// Add factory
		var factoryMethod = new CodeMethod("CreateFromDiscriminatorValue", CodeMethodKind.Factory)
		{
			IsStatic = true
		};
		factoryMethod.AddParameter(new CodeParameter("parseNode", CodeParameterKind.Path,
			new CodeType("IParseNode", isExternal: true)));
		cls.AddMethod(factoryMethod);

		modelsNs.AddClass(cls);

		// Act
		var results = emitter.Emit(root).ToList();

		// Assert
		results.Should().HaveCount(1);
		var source = results[0].Source;

		source.Should().Contain("Serialize");
		source.Should().Contain("WriteStringValue");
		source.Should().Contain("GetFieldDeserializers");
		source.Should().Contain("GetStringValue");
		source.Should().Contain("CreateFromDiscriminatorValue");
	}

	// ==================================================================
	// CodeWriter tests (basic validation of the shared code writer)
	// ==================================================================

	[TestMethod]
	public void CodeWriter_IndentationTracking_MaintainsCorrectLevels()
	{
		var writer = new CodeWriter();
		writer.IndentLevel.Should().Be(0);

		writer.IncreaseIndent();
		writer.IndentLevel.Should().Be(1);

		writer.IncreaseIndent();
		writer.IndentLevel.Should().Be(2);

		writer.DecreaseIndent();
		writer.IndentLevel.Should().Be(1);

		writer.DecreaseIndent();
		writer.IndentLevel.Should().Be(0);

		// Should not go below zero
		writer.DecreaseIndent();
		writer.IndentLevel.Should().Be(0);
	}

	[TestMethod]
	public void CodeWriter_OpenAndCloseBlock_WriteBracesAndAdjustIndent()
	{
		var writer = new CodeWriter();
		writer.WriteLine("namespace Foo");
		writer.OpenBlock();
		writer.WriteLine("public class Bar");
		writer.OpenBlock();
		writer.CloseBlock();
		writer.CloseBlock();

		var output = writer.ToString();
		output.Should().Contain("namespace Foo");
		output.Should().Contain("{");
		output.Should().Contain("}");
		output.Should().Contain("    public class Bar");
	}
}
