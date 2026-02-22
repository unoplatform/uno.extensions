#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;
using Uno.Extensions.Http.Kiota.SourceGenerator.Refinement;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;

/// <summary>
/// Transforms a parsed <see cref="OpenApiDocument"/> into a language-agnostic
/// CodeDOM tree that models the generated C# source code.
/// <para>
/// The builder executes a multi-phase pipeline:
/// <list type="number">
///   <item>Create the namespace hierarchy (root → models).</item>
///   <item>Create model declarations and enums from <c>Components/Schemas</c>.</item>
///   <item>Build a URL tree from <c>Paths</c> and create request builders.</item>
///   <item>Create the root client class with serializer/deserializer registration.</item>
///   <item>Resolve all forward <see cref="CodeType"/> references via
///         <see cref="MapTypeDefinitions"/>.</item>
/// </list>
/// </para>
/// <para>
/// The resulting <see cref="CodeNamespace"/> tree is then refined by
/// <c>CSharpRefiner</c> and emitted by <c>CSharpEmitter</c>.
/// </para>
/// </summary>
internal sealed class KiotaCodeDomBuilder
{
	private readonly KiotaGeneratorConfig _config;

	// ----- CodeDOM tree roots -----
	private CodeNamespace _rootNamespace;
	private CodeNamespace _modelsNamespace;

	// ----- Schema → CodeDOM lookups (keyed by schema reference ID or name) -----
	private readonly Dictionary<string, CodeClass> _modelsByRef =
		new Dictionary<string, CodeClass>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, CodeEnum> _enumsByRef =
		new Dictionary<string, CodeEnum>(StringComparer.OrdinalIgnoreCase);

	// ----- Deferred type-resolution bookkeeping -----
	private readonly List<DeferredTypeReference> _unresolvedTypes =
		new List<DeferredTypeReference>();

	// ----- Error schema tracking -----
	/// <summary>
	/// Schema ref names (IDs) referenced as error response models (4XX/5XX).
	/// Populated before model creation by <see cref="CollectErrorSchemaNames"/>.
	/// </summary>
	private readonly HashSet<string> _errorSchemaNames =
		new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Initializes a new <see cref="KiotaCodeDomBuilder"/> with the specified
	/// generator configuration.
	/// </summary>
	/// <param name="config">
	/// The configuration for this generation run. Must not be
	/// <see langword="default"/>.
	/// </param>
	public KiotaCodeDomBuilder(KiotaGeneratorConfig config)
	{
		_config = config;
	}

	// ==================================================================
	// Public API
	// ==================================================================

	/// <summary>
	/// Builds the complete CodeDOM tree from the given
	/// <see cref="OpenApiDocument"/>.
	/// </summary>
	/// <param name="document">
	/// A parsed OpenAPI document. Must not be <see langword="null"/>.
	/// </param>
	/// <returns>
	/// The root <see cref="CodeNamespace"/> containing the full CodeDOM tree.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="document"/> is <see langword="null"/>.
	/// </exception>
	public CodeNamespace Build(OpenApiDocument document)
	{
		if (document is null)
		{
			throw new ArgumentNullException(nameof(document));
		}

		// Phase 1: Create namespace structure.
		_rootNamespace = new CodeNamespace(_config.ClientNamespaceName);
		_modelsNamespace = _rootNamespace.AddNamespace("Models");

		// Phase 1b: Pre-scan paths to collect error schema names.
		if (document.Paths != null && document.Paths.Count > 0)
		{
			CollectErrorSchemaNames(document.Paths);
		}

		// Phase 2: Create model declarations and enums from schemas.
		if (document.Components?.Schemas != null && document.Components.Schemas.Count > 0)
		{
			CreateModelDeclarations(document.Components.Schemas);
		}

		// Phase 3: Build request builders from API paths.
		if (document.Paths != null && document.Paths.Count > 0)
		{
			var baseUrl = GetBaseUrl(document);
			var urlTree = BuildUrlTree(document.Paths);
			CreateRequestBuilders(urlTree, _rootNamespace, "{+baseurl}", baseUrl);
		}

		// Phase 4: Create the root client class.
		CreateRootClientClass(document);

		// Phase 5: Resolve all forward type references.
		MapTypeDefinitions();

		return _rootNamespace;
	}

	// ==================================================================
	// Phase 2: Model Declarations
	// ==================================================================

	/// <summary>
	/// Iterates all component schemas and creates <see cref="CodeClass"/>
	/// or <see cref="CodeEnum"/> declarations in the models namespace.
	/// </summary>
	private void CreateModelDeclarations(IDictionary<string, OpenApiSchema> schemas)
	{
		// Two-pass approach:
		// Pass 1: Create stubs for all schemas so forward references resolve.
		// Pass 2: Populate properties, inheritance, and composition.

		// Pass 1: Identify enums vs. model classes vs. composed types and create stubs.
		foreach (var kvp in schemas)
		{
			var schemaName = kvp.Key;
			var schema = kvp.Value;

			if (IsEnumSchema(schema))
			{
				var codeEnum = CreateEnumStub(schemaName, schema);
				_modelsNamespace.AddEnum(codeEnum);
				_enumsByRef[schemaName] = codeEnum;
			}
			else if (IsComposedTypeSchema(schema))
			{
				// oneOf/anyOf top-level schemas → composed-type wrapper class.
				var codeClass = new CodeClass(schemaName, CodeClassKind.Model);
				codeClass.Access = ParseAccessModifier(_config.TypeAccessModifier);
				_modelsNamespace.AddClass(codeClass);
				_modelsByRef[schemaName] = codeClass;
			}
			else if (IsObjectSchema(schema))
			{
				var codeClass = new CodeClass(schemaName, CodeClassKind.Model);
				codeClass.Access = ParseAccessModifier(_config.TypeAccessModifier);
				codeClass.Description = schema.Description;
				_modelsNamespace.AddClass(codeClass);
				_modelsByRef[schemaName] = codeClass;
			}
		}

		// Pass 2: Populate model class members.
		foreach (var kvp in schemas)
		{
			var schemaName = kvp.Key;
			var schema = kvp.Value;

			if (_modelsByRef.TryGetValue(schemaName, out var codeClass))
			{
				if (IsComposedTypeSchema(schema))
				{
					PopulateComposedTypeWrapper(codeClass, schemaName, schema);
				}
				else
				{
					PopulateModelClass(codeClass, schemaName, schema);
				}
			}
		}
	}

	/// <summary>
	/// Creates an enum stub with options from the schema's enum values.
	/// </summary>
	private CodeEnum CreateEnumStub(string name, OpenApiSchema schema)
	{
		var codeEnum = new CodeEnum(name);
		codeEnum.Access = ParseAccessModifier(_config.TypeAccessModifier);
		codeEnum.Description = schema.Description;

		if (schema.Enum != null)
		{
			foreach (var enumValue in schema.Enum)
			{
				var serializedName = enumValue is Microsoft.OpenApi.Any.OpenApiString str
					? str.Value
					: enumValue?.ToString() ?? string.Empty;

				var memberName = SanitizeEnumMemberName(serializedName);
				codeEnum.AddOption(new CodeEnumOption(memberName, serializedName));
			}
		}

		return codeEnum;
	}

	/// <summary>
	/// Populates a composed-type wrapper class for a <c>oneOf</c> or
	/// <c>anyOf</c> top-level schema. Creates one property per constituent
	/// type, adds <c>IComposedTypeWrapper</c> + <c>IParsable</c> interfaces,
	/// and the standard factory/serializer/deserializer methods.
	/// </summary>
	private void PopulateComposedTypeWrapper(CodeClass codeClass, string schemaName, OpenApiSchema schema)
	{
		// Determine whether this is a union (oneOf) or intersection (anyOf).
		var isUnion = schema.OneOf != null && schema.OneOf.Count > 0;
		var constituentSchemas = isUnion ? schema.OneOf : schema.AnyOf;

		// Persist the union/intersection flag so emitters can read it.
		codeClass.IsUnionType = isUnion;

		// Build description matching the Kiota CLI pattern:
		//   "Composed type wrapper for classes <see cref="..."/>, ..."
		var objectProps = new List<(string propName, CodeType codeType, string openApiType)>();
		var primitiveProps = new List<(string propName, CodeType codeType, string openApiType)>();

		foreach (var constituent in constituentSchemas)
		{
			if (constituent.Reference != null)
			{
				// Object $ref — property name = schema name.
				var refName = constituent.Reference.Id;
				var ct = new CodeType(refName) { IsNullable = true };
				_unresolvedTypes.Add(new DeferredTypeReference(ct, refName));
				objectProps.Add((refName, ct, null));
			}
			else
			{
				// Primitive type (string, integer, boolean).
				var mapping = CSharpConventionService.GetTypeMapping(
					constituent.Type, constituent.Format);
				if (mapping != null)
				{
					// Property name = PascalCase of OpenAPI type name (e.g. "boolean" → "Boolean").
					var propName = ToPascalCase(constituent.Type);
					var ct = new CodeType(mapping.CSharpTypeName, isExternal: true)
					{
						IsNullable = true,
					};
					primitiveProps.Add((propName, ct, constituent.Type));
				}
			}
		}

		// Sort each group alphabetically by property name (Kiota convention).
		objectProps.Sort((a, b) => string.Compare(a.propName, b.propName, StringComparison.Ordinal));
		primitiveProps.Sort((a, b) => string.Compare(a.propName, b.propName, StringComparison.Ordinal));

		// Build the class description: objects first (alphabetical), then primitives (alphabetical).
		var seeCrefs = new List<string>();
		for (int i = 0; i < objectProps.Count; i++)
		{
			seeCrefs.Add("<see cref=\"global::"
				+ _config.ClientNamespaceName + ".Models." + objectProps[i].propName + "\"/>");
		}

		for (int i = 0; i < primitiveProps.Count; i++)
		{
			seeCrefs.Add("<see cref=\"" + primitiveProps[i].codeType.Name + "\"/>");
		}

		codeClass.Description = "Composed type wrapper for classes "
			+ string.Join(", ", seeCrefs);

		// Merge: objects first, then primitives (both already sorted).
		var allProps = new List<(string propName, CodeType codeType, bool isPrimitive, string openApiType)>();
		for (int i = 0; i < objectProps.Count; i++)
		{
			allProps.Add((objectProps[i].propName, objectProps[i].codeType, false, objectProps[i].openApiType));
		}

		for (int i = 0; i < primitiveProps.Count; i++)
		{
			allProps.Add((primitiveProps[i].propName, primitiveProps[i].codeType, true, primitiveProps[i].openApiType));
		}

		// Properties are added alphabetically (flat sort) for declaration order.
		var alphabeticalProps = new List<(string propName, CodeType codeType, bool isPrimitive, string openApiType)>(allProps);
		alphabeticalProps.Sort((a, b) => string.Compare(a.propName, b.propName, StringComparison.Ordinal));

		// Add one property per constituent type.
		for (int i = 0; i < alphabeticalProps.Count; i++)
		{
			var (propName, codeType, isPrimitive, _) = alphabeticalProps[i];
			var prop = new CodeProperty(propName, CodePropertyKind.Custom, codeType);
			if (isPrimitive)
			{
				prop.Description = "Composed type representation for type <see cref=\""
					+ codeType.Name + "\"/>";
			}
			else
			{
				prop.Description = "Composed type representation for type <see cref=\"global::"
					+ _config.ClientNamespaceName + ".Models." + propName + "\"/>";
			}
			codeClass.AddProperty(prop);
		}

		// Add interfaces: IComposedTypeWrapper + IParsable (no IAdditionalDataHolder).
		codeClass.AddInterface(new CodeType("IComposedTypeWrapper", isExternal: true));
		codeClass.AddInterface(new CodeType("IParsable", isExternal: true));

		// Handle discriminator if present (oneOf schemas may have discriminators).
		if (schema.Discriminator != null
			&& !string.IsNullOrEmpty(schema.Discriminator.PropertyName))
		{
			codeClass.DiscriminatorPropertyName = schema.Discriminator.PropertyName;

			if (schema.Discriminator.Mapping != null)
			{
				foreach (var mapping in schema.Discriminator.Mapping)
				{
					var refName = ExtractSchemaRefName(mapping.Value);
					var typeRef = new CodeType(refName);
					_unresolvedTypes.Add(new DeferredTypeReference(typeRef, refName));
					codeClass.AddDiscriminatorMapping(mapping.Key, typeRef);
				}
			}
		}

		// Add standard methods: Factory, Deserializer, Serializer.
		// No Constructor for composed type wrappers.
		var factoryReturn = new CodeType(codeClass.Name);
		_unresolvedTypes.Add(new DeferredTypeReference(factoryReturn, codeClass.Name));
		var factory = new CodeMethod("CreateFromDiscriminatorValue", CodeMethodKind.Factory, factoryReturn)
		{
			IsStatic = true,
		};
		factory.AddParameter(new CodeParameter(
			"parseNode",
			CodeParameterKind.Body,
			new CodeType("IParseNode", isExternal: true)));
		codeClass.AddMethod(factory);

		var deserializerReturn = new CodeType("IDictionary<string, Action<IParseNode>>", isExternal: true);
		var deserializer = new CodeMethod("GetFieldDeserializers", CodeMethodKind.Deserializer, deserializerReturn);
		codeClass.AddMethod(deserializer);

		var serializer = new CodeMethod("Serialize", CodeMethodKind.Serializer);
		serializer.AddParameter(new CodeParameter(
			"writer",
			CodeParameterKind.Body,
			new CodeType("ISerializationWriter", isExternal: true)));
		codeClass.AddMethod(serializer);
	}

	/// <summary>
	/// Populates a model class with properties, inheritance (allOf),
	/// and composition (oneOf/anyOf) information.
	/// </summary>
	private void PopulateModelClass(CodeClass codeClass, string schemaName, OpenApiSchema schema)
	{
		// Handle allOf (inheritance + merged properties).
		if (schema.AllOf != null && schema.AllOf.Count > 0)
		{
			HandleAllOfComposition(codeClass, schema);
		}
		else
		{
			// Plain object — add properties directly.
			if (schema.Properties != null)
			{
				foreach (var propKvp in schema.Properties)
				{
					AddModelProperty(codeClass, propKvp.Key, propKvp.Value);
				}
			}
		}

		// Handle discriminator if present.
		if (schema.Discriminator != null
			&& !string.IsNullOrEmpty(schema.Discriminator.PropertyName))
		{
			codeClass.DiscriminatorPropertyName = schema.Discriminator.PropertyName;

			if (schema.Discriminator.Mapping != null)
			{
				foreach (var mapping in schema.Discriminator.Mapping)
				{
					var refName = ExtractSchemaRefName(mapping.Value);
					var typeRef = new CodeType(refName);
					_unresolvedTypes.Add(new DeferredTypeReference(typeRef, refName));
					codeClass.AddDiscriminatorMapping(mapping.Key, typeRef);
				}
			}
		}

		// Mark error models.
		if (IsErrorSchema(schemaName))
		{
			codeClass.IsErrorDefinition = true;
		}

		// Add standard model interfaces.
		// Derived model classes (allOf with a base $ref) inherit
		// IAdditionalDataHolder and AdditionalData from the base class,
		// so we only add them for root model classes.
		bool isDerived = codeClass.BaseClass != null;

		// IAdditionalDataHolder is added BEFORE IParsable to match Kiota CLI
		// alphabetical interface ordering.
		if (_config.IncludeAdditionalData && !isDerived)
		{
			codeClass.AddInterface(new CodeType("IAdditionalDataHolder", isExternal: true));
			AddAdditionalDataProperty(codeClass);
		}

		codeClass.AddInterface(new CodeType("IParsable", isExternal: true));

		if (_config.UsesBackingStore)
		{
			codeClass.AddInterface(new CodeType("IBackedModel", isExternal: true));
			AddBackingStoreProperty(codeClass);
		}

		// Add standard model methods: Constructor, Serializer, Deserializer, Factory.
		AddModelMethods(codeClass, schema);
	}

	/// <summary>
	/// Handles <c>allOf</c> composition: first <c>$ref</c> becomes the base class,
	/// additional schemas merge their properties into this class.
	/// </summary>
	private void HandleAllOfComposition(CodeClass codeClass, OpenApiSchema schema)
	{
		CodeType baseClassType = null;

		foreach (var allOfItem in schema.AllOf)
		{
			if (allOfItem.Reference != null && baseClassType == null)
			{
				// First $ref → base class.
				var refName = allOfItem.Reference.Id;
				baseClassType = new CodeType(refName);
				_unresolvedTypes.Add(new DeferredTypeReference(baseClassType, refName));
				codeClass.BaseClass = baseClassType;
			}
			else
			{
				// Inline schema or additional $ref → merge properties.
				if (allOfItem.Properties != null)
				{
					foreach (var propKvp in allOfItem.Properties)
					{
						AddModelProperty(codeClass, propKvp.Key, propKvp.Value);
					}
				}
			}
		}

		// Also add properties defined directly on the schema (outside allOf).
		if (schema.Properties != null)
		{
			foreach (var propKvp in schema.Properties)
			{
				AddModelProperty(codeClass, propKvp.Key, propKvp.Value);
			}
		}
	}

	/// <summary>
	/// Adds a property to a model class from an OpenAPI schema property.
	/// Detects inline enum definitions and extracts them as separate
	/// <see cref="CodeEnum"/> declarations in the models namespace.
	/// </summary>
	private void AddModelProperty(CodeClass codeClass, string propertyName, OpenApiSchema propertySchema)
	{
		CodeTypeBase typeBase;

		// Detect inline enum: string type with enum values but no $ref.
		if (propertySchema.Reference == null
			&& IsEnumSchema(propertySchema))
		{
			// Extract as a separate enum type named {ClassName}_{propertyName}.
			var enumName = codeClass.Name + "_" + propertyName;
			if (!_enumsByRef.ContainsKey(enumName))
			{
				var codeEnum = CreateEnumStub(enumName, propertySchema);
				_modelsNamespace.AddEnum(codeEnum);
				_enumsByRef[enumName] = codeEnum;
			}

			var enumType = new CodeType(enumName) { IsNullable = true };
			_unresolvedTypes.Add(new DeferredTypeReference(enumType, enumName));
			typeBase = enumType;
		}
		else
		{
			typeBase = ResolveSchemaType(propertySchema);
		}

		var codeProp = new CodeProperty(ToPascalCase(propertyName), CodePropertyKind.Custom, typeBase);
		codeProp.SerializedName = propertyName;
		codeProp.Description = !string.IsNullOrEmpty(propertySchema.Description)
			? propertySchema.Description
			: "The " + ToLowerFirstChar(ToPascalCase(propertyName)) + " property";
		codeClass.AddProperty(codeProp);
	}

	/// <summary>
	/// Adds AdditionalData property to a model class.
	/// </summary>
	private static void AddAdditionalDataProperty(CodeClass codeClass)
	{
		var dictType = new CodeType("IDictionary<string, object>", isExternal: true);
		var prop = new CodeProperty("AdditionalData", CodePropertyKind.AdditionalData, dictType)
		{
			Description = "Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.",
		};
		codeClass.AddProperty(prop);
	}

	/// <summary>
	/// Adds BackingStore property to a model class.
	/// </summary>
	private static void AddBackingStoreProperty(CodeClass codeClass)
	{
		var bsType = new CodeType("IBackingStore", isExternal: true);
		var prop = new CodeProperty("BackingStore", CodePropertyKind.BackingStore, bsType);
		codeClass.AddProperty(prop);
	}

	/// <summary>
	/// Adds standard model methods: Constructor, Serializer, Deserializer, Factory.
	/// </summary>
	private void AddModelMethods(CodeClass codeClass, OpenApiSchema schema)
	{
		// Constructor — only for root model classes. Derived classes (allOf)
		// don't need a constructor since the base class initializes
		// AdditionalData / BackingStore.
		if (codeClass.BaseClass == null)
		{
			var ctor = new CodeMethod(codeClass.Name, CodeMethodKind.Constructor);
			codeClass.AddMethod(ctor);
		}

		// Factory method (CreateFromDiscriminatorValue)
		var factoryReturn = new CodeType(codeClass.Name);
		_unresolvedTypes.Add(new DeferredTypeReference(factoryReturn, codeClass.Name));
		var factory = new CodeMethod("CreateFromDiscriminatorValue", CodeMethodKind.Factory, factoryReturn)
		{
			IsStatic = true,
		};
		factory.AddParameter(new CodeParameter(
			"parseNode",
			CodeParameterKind.Body,
			new CodeType("IParseNode", isExternal: true)));
		codeClass.AddMethod(factory);

		// GetFieldDeserializers
		var deserializerReturn = new CodeType("IDictionary<string, Action<IParseNode>>", isExternal: true);
		var deserializer = new CodeMethod("GetFieldDeserializers", CodeMethodKind.Deserializer, deserializerReturn);
		if (codeClass.BaseClass != null)
		{
			deserializer.IsOverride = true;
		}
		codeClass.AddMethod(deserializer);

		// Serialize
		var serializer = new CodeMethod("Serialize", CodeMethodKind.Serializer);
		serializer.AddParameter(new CodeParameter(
			"writer",
			CodeParameterKind.Body,
			new CodeType("ISerializationWriter", isExternal: true)));
		if (codeClass.BaseClass != null)
		{
			serializer.IsOverride = true;
		}
		codeClass.AddMethod(serializer);
	}

	// ==================================================================
	// Phase 3: Request Builders
	// ==================================================================

	/// <summary>
	/// Builds a URL tree from the flat OpenAPI paths dictionary.
	/// </summary>
	private static UrlTreeNode BuildUrlTree(OpenApiPaths paths)
	{
		var root = new UrlTreeNode(string.Empty);

		foreach (var pathKvp in paths)
		{
			var pathString = pathKvp.Key;   // e.g., "/pets/{petId}/vaccinations"
			var pathItem = pathKvp.Value;

			var segments = pathString.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			var current = root;

			for (int i = 0; i < segments.Length; i++)
			{
				var segment = segments[i];
				if (!current.Children.TryGetValue(segment, out var child))
				{
					child = new UrlTreeNode(segment);
					current.Children[segment] = child;
				}

				current = child;
			}

			// The terminal node holds the OpenApiPathItem with its operations.
			current.PathItem = pathItem;
			current.FullPath = pathString;
		}

		return root;
	}

	/// <summary>
	/// Computes the backslash-separated path for use in request builder XML doc
	/// descriptions. Extracts the API path from the URL template and converts
	/// forward slashes to backslashes, e.g. <c>\pets</c> or <c>\pets\{petId}</c>.
	/// </summary>
	private static string ComputeRequestBuilderPath(string urlTemplate)
	{
		// urlTemplate is like "{+baseurl}/pets/{petId}{?query}" — extract the path portion.
		var path = urlTemplate;

		// Strip the base URL prefix.
		int baseEnd = path.IndexOf("}/", StringComparison.Ordinal);
		if (baseEnd >= 0)
		{
			path = path.Substring(baseEnd + 1); // Keep leading /
		}

		// Strip query parameter suffixes.
		int queryStart = path.IndexOf("{?", StringComparison.Ordinal);
		if (queryStart >= 0)
		{
			path = path.Substring(0, queryStart);
		}

		// Convert /pets/{petId} → \pets\{petId}
		return path.Replace("/", "\\");
	}

	/// <summary>
	/// Recursively creates request builder classes from the URL tree.
	/// </summary>
	private void CreateRequestBuilders(
		UrlTreeNode node,
		CodeNamespace parentNamespace,
		string parentUrlTemplate,
		string baseUrl)
	{
		foreach (var childKvp in node.Children)
		{
			var segment = childKvp.Key;
			var childNode = childKvp.Value;
			var isParameter = segment.StartsWith("{") && segment.EndsWith("}");

			// Determine naming.
			string segmentName;
			string paramName = null;
			if (isParameter)
			{
				paramName = segment.Substring(1, segment.Length - 2); // strip braces
				segmentName = "Item";
			}
			else
			{
				segmentName = ToPascalCase(segment);
			}

			// Create child namespace.
			var childNamespace = parentNamespace.GetOrAddNamespace(segmentName);

			// Build URL template for this segment.
			string urlTemplate;
			if (isParameter)
			{
				urlTemplate = parentUrlTemplate + "/{" + paramName + "}";
			}
			else
			{
				urlTemplate = parentUrlTemplate + "/" + segment;
			}

			// Collect query parameter names for this path item.
			var queryParamNames = new List<string>();
			if (childNode.PathItem != null)
			{
				foreach (var opKvp in childNode.PathItem.Operations)
				{
					if (opKvp.Value.Parameters != null)
					{
						foreach (var param in opKvp.Value.Parameters)
						{
							if (param.In == ParameterLocation.Query
								&& !queryParamNames.Contains(param.Name))
							{
								queryParamNames.Add(param.Name);
							}
						}
					}
				}
			}

			// Append query params to URL template if any.
			// Each parameter gets the RFC 6570 "explode" modifier (*) following Kiota convention.
			var fullUrlTemplate = urlTemplate;
			if (queryParamNames.Count > 0)
			{
				fullUrlTemplate += "{?" + string.Join(",", queryParamNames.ConvertAll(n => n + "*")) + "}";
			}

			// Determine request builder class name.
			string className;
			if (isParameter)
			{
				// For parameterized segments, Kiota uses "With{CleanParam}ItemRequestBuilder".
				// e.g. {petId} → WithPetItemRequestBuilder, {ownerId} → WithOwnerItemRequestBuilder.
				var cleanParam = CleanParameterName(paramName);
				className = "With" + cleanParam + "ItemRequestBuilder";
			}
			else
			{
				className = segmentName + "RequestBuilder";
			}

			// Create the request builder class.
			var requestBuilder = new CodeClass(className, CodeClassKind.RequestBuilder);
			requestBuilder.Access = ParseAccessModifier(_config.TypeAccessModifier);
			requestBuilder.BaseClass = new CodeType("BaseRequestBuilder", isExternal: true);

			// Set description for XML doc: "Builds and executes requests for operations under \path"
			// Uses backslash-separated path segments matching Kiota CLI convention.
			var descPath = ComputeRequestBuilderPath(urlTemplate);
			requestBuilder.Description = "Builds and executes requests for operations under " + descPath;

			childNamespace.AddClass(requestBuilder);

			// Add UrlTemplate property.
			var urlTemplateProp = new CodeProperty("UrlTemplate", CodePropertyKind.UrlTemplate,
				new CodeType("string", isExternal: true))
			{
				SerializedName = fullUrlTemplate, // Store the actual template value here.
			};
			requestBuilder.AddProperty(urlTemplateProp);

			// Add constructors.
			AddRequestBuilderConstructors(requestBuilder, fullUrlTemplate);

			// Add WithUrl method.
			AddWithUrlMethod(requestBuilder);

			// Add HTTP operation methods if this node has a path item.
			if (childNode.PathItem != null)
			{
				CreateOperationMethods(requestBuilder, childNode.PathItem, queryParamNames);
			}

			// If the parent is a collection endpoint (non-parameter) and this child
			// is a parameter, add an indexer on the parent's request builder.
			if (isParameter)
			{
				// Find the parent request builder in its namespace.
				var parentRequestBuilder = FindRequestBuilderInNamespace(parentNamespace);
				if (parentRequestBuilder != null)
				{
					// Try to get the path parameter description from any operation.
					string pathParamDescription = null;
					if (childNode.PathItem != null)
					{
						pathParamDescription = FindPathParameterDescription(childNode.PathItem, paramName);
					}

					AddIndexer(parentRequestBuilder, requestBuilder, paramName, childNamespace, pathParamDescription);
				}
			}

			// Recurse into children.
			CreateRequestBuilders(childNode, childNamespace, urlTemplate, baseUrl);

			// Add navigation properties for non-parameter children of
			// the current node's request builder pointing to their request builders.
			if (!isParameter)
			{
				// If the parent node has a request builder, add navigation to this child.
				var parentRb = FindRequestBuilderInNamespace(parentNamespace);
				if (parentRb != null)
				{
					AddNavigationProperty(parentRb, requestBuilder, segmentName, childNamespace);
				}
			}
		}
	}

	/// <summary>
	/// Adds the standard two-constructor pattern to a request builder.
	/// </summary>
	private void AddRequestBuilderConstructors(CodeClass requestBuilder, string urlTemplate)
	{
		// Constructor 1: (Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter)
		var ctor1 = new CodeMethod(requestBuilder.Name, CodeMethodKind.Constructor);
		ctor1.AddParameter(new CodeParameter(
			"pathParameters",
			CodeParameterKind.Path,
			new CodeType("Dictionary<string, object>", isExternal: true)));
		ctor1.AddParameter(new CodeParameter(
			"requestAdapter",
			CodeParameterKind.RequestAdapter,
			new CodeType("IRequestAdapter", isExternal: true)));
		requestBuilder.AddMethod(ctor1);

		// Constructor 2: (string rawUrl, IRequestAdapter requestAdapter)
		var ctor2 = new CodeMethod(requestBuilder.Name, CodeMethodKind.Constructor);
		ctor2.AddParameter(new CodeParameter(
			"rawUrl",
			CodeParameterKind.RawUrl,
			new CodeType("string", isExternal: true)));
		ctor2.AddParameter(new CodeParameter(
			"requestAdapter",
			CodeParameterKind.RequestAdapter,
			new CodeType("IRequestAdapter", isExternal: true)));
		requestBuilder.AddMethod(ctor2);
	}

	/// <summary>
	/// Adds the WithUrl method to a request builder.
	/// </summary>
	private void AddWithUrlMethod(CodeClass requestBuilder)
	{
		var returnType = new CodeType(requestBuilder.Name);
		_unresolvedTypes.Add(new DeferredTypeReference(returnType, requestBuilder.Name));

		var withUrl = new CodeMethod("WithUrl", CodeMethodKind.WithUrl, returnType);
		withUrl.AddParameter(new CodeParameter(
			"rawUrl",
			CodeParameterKind.RawUrl,
			new CodeType("string", isExternal: true)));
		requestBuilder.AddMethod(withUrl);
	}

	/// <summary>
	/// Creates HTTP executor and request-generator methods for each operation.
	/// </summary>
	private void CreateOperationMethods(
		CodeClass requestBuilder,
		OpenApiPathItem pathItem,
		List<string> queryParamNames)
	{
		foreach (var opKvp in pathItem.Operations)
		{
			var httpMethod = opKvp.Key;   // GET, POST, PUT, PATCH, DELETE
			var operation = opKvp.Value;
			var methodName = GetMethodNameForHttpMethod(httpMethod);

			// Determine response type.
			var responseType = GetSuccessResponseType(operation);

			// --- Request Executor method (e.g., GetAsync, PostAsync) ---
			var operationDescription = !string.IsNullOrEmpty(operation.Description)
				? operation.Description
				: operation.Summary;
			var executor = new CodeMethod(methodName + "Async", CodeMethodKind.RequestExecutor, responseType)
			{
				IsAsync = true,
				HttpMethod = httpMethod.ToString().ToUpperInvariant(),
				Description = operationDescription,
			};

			// Accept header.
			var acceptTypes = GetAcceptMediaTypes(operation);
			foreach (var accept in acceptTypes)
			{
				executor.AddAcceptedResponseType(accept);
			}

			// Request body parameter (for POST, PUT, PATCH).
			if (operation.RequestBody != null)
			{
				var bodyType = GetRequestBodyType(operation.RequestBody);
				if (bodyType != null)
				{
					var bodyParam = new CodeParameter("body", CodeParameterKind.Body, bodyType);

					// Use schema description as body parameter doc.
					var bodyDescription = GetRequestBodySchemaDescription(operation.RequestBody);
					if (!string.IsNullOrEmpty(bodyDescription))
					{
						bodyParam.Description = bodyDescription;
					}
					else
					{
						bodyParam.Description = "The request body";
					}

					executor.AddParameter(bodyParam);

					var contentType = GetRequestBodyContentType(operation.RequestBody);
					executor.RequestBodyContentType = contentType;
				}
			}

			// Query parameters → RequestConfiguration parameter.
			var queryParamsType = CreateQueryParametersClassIfNeeded(
				requestBuilder, methodName, operation);

			var configType = queryParamsType != null
				? new CodeType("RequestConfiguration<" + queryParamsType.Name + ">", isExternal: true)
				: new CodeType("RequestConfiguration<DefaultQueryParameters>", isExternal: true);

			executor.AddParameter(new CodeParameter(
				"requestConfiguration",
				CodeParameterKind.RequestConfiguration,
				configType)
			{
				Optional = true,
				DefaultValue = "default",
			});

			// Cancellation token.
			executor.AddParameter(new CodeParameter(
				"cancellationToken",
				CodeParameterKind.Cancellation,
				new CodeType("CancellationToken", isExternal: true))
			{
				Optional = true,
				DefaultValue = "default",
			});

			// Error mappings.
			AddErrorMappings(executor, operation);

			requestBuilder.AddMethod(executor);

			// --- Request Generator method (e.g., ToGetRequestInformation) ---
			var generatorReturnType = new CodeType("RequestInformation", isExternal: true);
			var generator = new CodeMethod(
				"To" + methodName + "RequestInformation",
				CodeMethodKind.RequestGenerator,
				generatorReturnType)
			{
				HttpMethod = httpMethod.ToString().ToUpperInvariant(),
				Description = operationDescription,
			};

			foreach (var accept in acceptTypes)
			{
				generator.AddAcceptedResponseType(accept);
			}

			if (operation.RequestBody != null)
			{
				var bodyType = GetRequestBodyType(operation.RequestBody);
				if (bodyType != null)
				{
					var genBodyParam = new CodeParameter("body", CodeParameterKind.Body, bodyType);
					var genBodyDesc = GetRequestBodySchemaDescription(operation.RequestBody);
					if (!string.IsNullOrEmpty(genBodyDesc))
					{
						genBodyParam.Description = genBodyDesc;
					}
					else
					{
						genBodyParam.Description = "The request body";
					}

					generator.AddParameter(genBodyParam);
					generator.RequestBodyContentType = GetRequestBodyContentType(operation.RequestBody);
				}
			}

			generator.AddParameter(new CodeParameter(
				"requestConfiguration",
				CodeParameterKind.RequestConfiguration,
				configType.Clone())
			{
				Optional = true,
				DefaultValue = "default",
			});

			requestBuilder.AddMethod(generator);
		}
	}

	/// <summary>
	/// Creates a query parameters inner class for operations that have query parameters.
	/// Returns <see langword="null"/> if the operation has no query parameters.
	/// </summary>
	private CodeClass CreateQueryParametersClassIfNeeded(
		CodeClass requestBuilder,
		string methodName,
		OpenApiOperation operation)
	{
		var queryParams = new List<OpenApiParameter>();
		if (operation.Parameters != null)
		{
			foreach (var param in operation.Parameters)
			{
				if (param.In == ParameterLocation.Query)
				{
					queryParams.Add(param);
				}
			}
		}

		if (queryParams.Count == 0)
		{
			return null;
		}

		var qpClassName = requestBuilder.Name + methodName + "QueryParameters";
		var qpClass = new CodeClass(qpClassName, CodeClassKind.QueryParameters);
		qpClass.Access = ParseAccessModifier(_config.TypeAccessModifier);

		// Set the description from the operation (shown as <summary> on the class).
		var operationDescription = !string.IsNullOrEmpty(operation.Description)
			? operation.Description
			: operation.Summary;
		qpClass.Description = operationDescription;

		foreach (var param in queryParams)
		{
			var propType = ResolveSchemaType(param.Schema);

			// Query parameter arrays use T[] syntax (not List<T>).
			if (propType is CodeType ct && ct.IsCollection)
			{
				ct.CollectionKind = CollectionKind.Array;
			}

			var prop = new CodeProperty(ToPascalCase(param.Name), CodePropertyKind.QueryParameter, propType)
			{
				SerializedName = param.Name,
				Description = param.Description,
			};
			qpClass.AddProperty(prop);
		}

		requestBuilder.AddInnerClass(qpClass);
		return qpClass;
	}

	/// <summary>
	/// Adds error mappings (4XX, 5XX) to an executor method from operation responses.
	/// </summary>
	private void AddErrorMappings(CodeMethod executor, OpenApiOperation operation)
	{
		if (operation.Responses == null)
		{
			return;
		}

		foreach (var responseKvp in operation.Responses)
		{
			var statusCode = responseKvp.Key;
			var response = responseKvp.Value;

			// Only map error status codes (4XX, 5XX, or specific 4xx/5xx codes).
			if (!IsErrorStatusCode(statusCode))
			{
				continue;
			}

			var schema = GetResponseSchema(response);
			if (schema?.Reference != null)
			{
				var refName = schema.Reference.Id;
				var errorType = new CodeType(refName);
				_unresolvedTypes.Add(new DeferredTypeReference(errorType, refName));
				executor.AddErrorMapping(statusCode, errorType);
			}
		}
	}

	/// <summary>
	/// Adds an indexer from a collection request builder to an item request builder.
	/// </summary>
	private void AddIndexer(
		CodeClass parentBuilder,
		CodeClass itemBuilder,
		string parameterName,
		CodeNamespace itemNamespace,
		string parameterDescription)
	{
		var returnType = new CodeType(itemBuilder.Name);
		_unresolvedTypes.Add(new DeferredTypeReference(returnType, itemBuilder.Name));

		var indexerName = "By" + ToPascalCase(parameterName);

		// Compute the description using the item namespace path with lowercase segments.
		// E.g. PetStore.Client.Pets.Item → "Gets an item from the PetStore.Client.pets.item collection"
		var indexerDescription = "Gets an item from the "
			+ GetLowercasePathDescription(itemNamespace)
			+ " collection";

		var indexer = new CodeIndexer(
			indexerName,
			returnType,
			parameterName,
			parameterName)
		{
			IndexParameterType = new CodeType("string", isExternal: true),
			Description = indexerDescription,
		};

		// Store the parameter description on the indexer for the emitter to use.
		if (!string.IsNullOrEmpty(parameterDescription))
		{
			indexer.ParameterDescription = parameterDescription;
		}

		parentBuilder.AddIndexer(indexer);
	}

	/// <summary>
	/// Adds a navigation property from a parent request builder to a child.
	/// </summary>
	private void AddNavigationProperty(
		CodeClass parentBuilder,
		CodeClass childBuilder,
		string navigationName,
		CodeNamespace childNamespace)
	{
		var navType = new CodeType(childBuilder.Name);
		_unresolvedTypes.Add(new DeferredTypeReference(navType, childBuilder.Name));

		var navProp = new CodeProperty(navigationName, CodePropertyKind.Navigation, navType)
		{
			Description = "The " + ToLowerFirstChar(navigationName) + " property",
		};

		parentBuilder.AddProperty(navProp);
	}

	// ==================================================================
	// Phase 4: Root Client Class
	// ==================================================================

	/// <summary>
	/// Creates the root client class that extends <c>BaseRequestBuilder</c>
	/// and registers default serializers/deserializers.
	/// </summary>
	private void CreateRootClientClass(OpenApiDocument document)
	{
		var clientName = _config.ClientClassName;
		var baseUrl = GetBaseUrl(document);

		var clientClass = new CodeClass(clientName, CodeClassKind.RequestBuilder);
		clientClass.Access = ParseAccessModifier(_config.TypeAccessModifier);
		clientClass.BaseClass = new CodeType("BaseRequestBuilder", isExternal: true);
		clientClass.Description = "The main entry point of the SDK, exposes the configuration and the fluent API.";

		// Root client constructor: (IRequestAdapter requestAdapter)
		var ctor = new CodeMethod(clientName, CodeMethodKind.Constructor)
		{
			BaseUrl = baseUrl,
		};
		ctor.AddParameter(new CodeParameter(
			"requestAdapter",
			CodeParameterKind.RequestAdapter,
			new CodeType("IRequestAdapter", isExternal: true)));
		clientClass.AddMethod(ctor);

		// URL template property.
		var urlTemplateProp = new CodeProperty("UrlTemplate", CodePropertyKind.UrlTemplate,
			new CodeType("string", isExternal: true))
		{
			SerializedName = "{+baseurl}",
		};
		clientClass.AddProperty(urlTemplateProp);

		// Add navigation properties for top-level path segments.
		// These will be added when request builders are created, but the
		// root client needs them too. We handle this by finding the
		// existing top-level request builders and adding nav properties.
		foreach (var childNs in _rootNamespace.Namespaces)
		{
			// Skip the Models namespace.
			if (string.Equals(childNs.Name, "Models", StringComparison.Ordinal))
			{
				continue;
			}

			foreach (var childClass in childNs.Classes)
			{
				if (childClass.Kind == CodeClassKind.RequestBuilder)
				{
					var navType = new CodeType(childClass.Name);
					_unresolvedTypes.Add(new DeferredTypeReference(navType, childClass.Name));

					var navProp = new CodeProperty(
						childNs.Name,
						CodePropertyKind.Navigation,
						navType)
					{
						Description = "The " + ToLowerFirstChar(childNs.Name) + " property",
					};
					clientClass.AddProperty(navProp);
				}
			}
		}

		// Insert the client class at the root namespace.
		_rootNamespace.AddClass(clientClass);
	}

	// ==================================================================
	// Phase 5: Type Definition Resolution
	// ==================================================================

	/// <summary>
	/// Resolves all deferred <see cref="CodeType"/> forward references by
	/// matching them to their <see cref="CodeClass"/> or <see cref="CodeEnum"/>
	/// definitions in the CodeDOM tree.
	/// </summary>
	private void MapTypeDefinitions()
	{
		// Build a lookup of all classes and enums in the tree.
		var allClasses = new Dictionary<string, CodeClass>(StringComparer.OrdinalIgnoreCase);
		var allEnums = new Dictionary<string, CodeEnum>(StringComparer.OrdinalIgnoreCase);

		CollectTypeDefinitions(_rootNamespace, allClasses, allEnums);

		// Resolve each deferred reference.
		foreach (var deferred in _unresolvedTypes)
		{
			if (allClasses.TryGetValue(deferred.ReferenceName, out var cls))
			{
				deferred.TypeRef.TypeDefinition = cls;
			}
			else if (allEnums.TryGetValue(deferred.ReferenceName, out var en))
			{
				deferred.TypeRef.TypeDefinition = en;
			}
			// If unresolved, leave TypeDefinition as null (external type).
		}
	}

	/// <summary>
	/// Recursively collects all class and enum declarations from the namespace tree.
	/// </summary>
	private static void CollectTypeDefinitions(
		CodeNamespace ns,
		Dictionary<string, CodeClass> classes,
		Dictionary<string, CodeEnum> enums)
	{
		foreach (var cls in ns.Classes)
		{
			classes[cls.Name] = cls;

			// Also index inner classes.
			foreach (var inner in cls.GetAllInnerClasses())
			{
				classes[inner.Name] = inner;
			}
		}

		foreach (var en in ns.Enums)
		{
			enums[en.Name] = en;
		}

		foreach (var child in ns.Namespaces)
		{
			CollectTypeDefinitions(child, classes, enums);
		}
	}

	// ==================================================================
	// Type Resolution Helpers
	// ==================================================================

	/// <summary>
	/// Resolves an <see cref="OpenApiSchema"/> to a <see cref="CodeTypeBase"/>
	/// reference for use in properties, method return types, etc.
	/// </summary>
	private CodeTypeBase ResolveSchemaType(OpenApiSchema schema)
	{
		if (schema == null)
		{
			return new CodeType("object", isExternal: true) { IsNullable = true };
		}

		// $ref → reference to another schema (model class or enum).
		if (schema.Reference != null)
		{
			var refName = schema.Reference.Id;
			var codeType = new CodeType(refName) { IsNullable = true };
			_unresolvedTypes.Add(new DeferredTypeReference(codeType, refName));
			return codeType;
		}

		// oneOf → CodeUnionType
		if (schema.OneOf != null && schema.OneOf.Count > 0)
		{
			return CreateComposedType(schema.OneOf, isUnion: true);
		}

		// anyOf → CodeIntersectionType
		if (schema.AnyOf != null && schema.AnyOf.Count > 0)
		{
			return CreateComposedType(schema.AnyOf, isUnion: false);
		}

		// array → collection type
		if (string.Equals(schema.Type, "array", StringComparison.OrdinalIgnoreCase))
		{
			var itemType = ResolveSchemaType(schema.Items);
			if (itemType is CodeType ct)
			{
				ct.IsCollection = true;
				ct.CollectionKind = CollectionKind.Complex; // List<T>
				return ct;
			}

			// Fallback for complex item types.
			var wrapper = new CodeType(itemType.Name, isExternal: false)
			{
				IsCollection = true,
				CollectionKind = CollectionKind.Complex,
			};
			return wrapper;
		}

		// Primitive / format mapping.
		return MapPrimitiveType(schema);
	}

	/// <summary>
	/// Creates a <see cref="CodeUnionType"/> or <see cref="CodeIntersectionType"/>
	/// from a list of composed schemas.
	/// </summary>
	private CodeTypeBase CreateComposedType(IList<OpenApiSchema> schemas, bool isUnion)
	{
		var names = new List<string>();
		var types = new List<CodeType>();

		foreach (var s in schemas)
		{
			var resolved = ResolveSchemaType(s);
			if (resolved is CodeType ct)
			{
				types.Add(ct);
				names.Add(ct.Name);
			}
		}

		var composedName = string.Join("Or", names);

		if (isUnion)
		{
			var union = new CodeUnionType(composedName);
			foreach (var t in types)
			{
				union.AddType(t);
			}
			return union;
		}
		else
		{
			var intersection = new CodeIntersectionType(composedName);
			foreach (var t in types)
			{
				intersection.AddType(t);
			}
			return intersection;
		}
	}

	/// <summary>
	/// Maps an OpenAPI primitive schema (type + format) to a <see cref="CodeType"/>.
	/// </summary>
	private static CodeType MapPrimitiveType(OpenApiSchema schema)
	{
		var type = schema.Type ?? "object";
		var format = schema.Format;

		switch (type.ToLowerInvariant())
		{
			case "string":
				return MapStringType(format);

			case "integer":
				if (string.Equals(format, "int64", StringComparison.OrdinalIgnoreCase))
					return new CodeType("long", isExternal: true) { IsNullable = true };
				return new CodeType("int", isExternal: true) { IsNullable = true };

			case "number":
				if (string.Equals(format, "float", StringComparison.OrdinalIgnoreCase))
					return new CodeType("float", isExternal: true) { IsNullable = true };
				if (string.Equals(format, "decimal", StringComparison.OrdinalIgnoreCase))
					return new CodeType("decimal", isExternal: true) { IsNullable = true };
				return new CodeType("double", isExternal: true) { IsNullable = true };

			case "boolean":
				return new CodeType("bool", isExternal: true) { IsNullable = true };

			case "object":
				return new CodeType("object", isExternal: true) { IsNullable = true };

			default:
				return new CodeType("object", isExternal: true) { IsNullable = true };
		}
	}

	/// <summary>
	/// Maps a string schema with format to the appropriate C# type.
	/// </summary>
	private static CodeType MapStringType(string format)
	{
		if (string.IsNullOrEmpty(format))
		{
			return new CodeType("string", isExternal: true) { IsNullable = true };
		}

		switch (format.ToLowerInvariant())
		{
			case "date-time":
				return new CodeType("DateTimeOffset", isExternal: true) { IsNullable = true };
			case "date":
				return new CodeType("Date", isExternal: true) { IsNullable = true };
			case "time":
				return new CodeType("Time", isExternal: true) { IsNullable = true };
			case "duration":
				return new CodeType("TimeSpan", isExternal: true) { IsNullable = true };
			case "uuid":
				return new CodeType("Guid", isExternal: true) { IsNullable = true };
			case "binary":
				return new CodeType("Stream", isExternal: true) { IsNullable = true };
			case "byte":
				return new CodeType("byte", isExternal: true)
				{
					IsNullable = true,
					IsCollection = true,
					CollectionKind = CollectionKind.Array,
				};
			default:
				return new CodeType("string", isExternal: true) { IsNullable = true };
		}
	}

	// ==================================================================
	// OpenAPI Response Helpers
	// ==================================================================

	/// <summary>
	/// Determines the success response type for an operation.
	/// Returns the CodeTypeBase for the 2XX response schema, or void type.
	/// </summary>
	private CodeTypeBase GetSuccessResponseType(OpenApiOperation operation)
	{
		if (operation.Responses == null)
		{
			return null; // void
		}

		// Look for 200, 201, 2XX in order of preference.
		OpenApiResponse successResponse = null;
		foreach (var responseKvp in operation.Responses)
		{
			var statusCode = responseKvp.Key;
			if (statusCode.StartsWith("2", StringComparison.Ordinal)
				|| string.Equals(statusCode, "default", StringComparison.OrdinalIgnoreCase))
			{
				successResponse = responseKvp.Value;
				break;
			}
		}

		if (successResponse == null)
		{
			return null; // void
		}

		var schema = GetResponseSchema(successResponse);
		if (schema == null)
		{
			return null; // void (e.g., 204 No Content)
		}

		return ResolveSchemaType(schema);
	}

	/// <summary>
	/// Extracts the response schema from an <see cref="OpenApiResponse"/>,
	/// preferring JSON content.
	/// </summary>
	private static OpenApiSchema GetResponseSchema(OpenApiResponse response)
	{
		if (response?.Content == null || response.Content.Count == 0)
		{
			return null;
		}

		// Prefer application/json.
		if (response.Content.TryGetValue("application/json", out var jsonMedia))
		{
			return jsonMedia.Schema;
		}

		// Fall back to first available.
		foreach (var mediaKvp in response.Content)
		{
			if (mediaKvp.Value?.Schema != null)
			{
				return mediaKvp.Value.Schema;
			}
		}

		return null;
	}

	/// <summary>
	/// Gets the request body type from an <see cref="OpenApiRequestBody"/>.
	/// </summary>
	private CodeTypeBase GetRequestBodyType(OpenApiRequestBody requestBody)
	{
		if (requestBody?.Content == null)
		{
			return null;
		}

		// Prefer application/json.
		if (requestBody.Content.TryGetValue("application/json", out var jsonMedia)
			&& jsonMedia.Schema != null)
		{
			return ResolveSchemaType(jsonMedia.Schema);
		}

		// Fall back to first available.
		foreach (var mediaKvp in requestBody.Content)
		{
			if (mediaKvp.Value?.Schema != null)
			{
				return ResolveSchemaType(mediaKvp.Value.Schema);
			}
		}

		return null;
	}

	/// <summary>
	/// Gets the description from the schema referenced by the request body.
	/// </summary>
	private static string GetRequestBodySchemaDescription(OpenApiRequestBody requestBody)
	{
		if (requestBody?.Content == null)
		{
			return null;
		}

		// Prefer application/json.
		if (requestBody.Content.TryGetValue("application/json", out var jsonMedia)
			&& jsonMedia.Schema != null)
		{
			return jsonMedia.Schema.Description;
		}

		// Fall back to first available.
		foreach (var mediaKvp in requestBody.Content)
		{
			if (mediaKvp.Value?.Schema != null)
			{
				return mediaKvp.Value.Schema.Description;
			}
		}

		return null;
	}

	/// <summary>
	/// Gets the content type of the request body (e.g., "application/json").
	/// </summary>
	private static string GetRequestBodyContentType(OpenApiRequestBody requestBody)
	{
		if (requestBody?.Content == null)
		{
			return null;
		}

		if (requestBody.Content.ContainsKey("application/json"))
		{
			return "application/json";
		}

		foreach (var key in requestBody.Content.Keys)
		{
			return key;
		}

		return null;
	}

	/// <summary>
	/// Gets the accepted media types from operation responses.
	/// </summary>
	private static List<string> GetAcceptMediaTypes(OpenApiOperation operation)
	{
		var result = new List<string>();

		if (operation.Responses == null)
		{
			return result;
		}

		foreach (var responseKvp in operation.Responses)
		{
			if (responseKvp.Value?.Content == null)
			{
				continue;
			}

			foreach (var mediaType in responseKvp.Value.Content.Keys)
			{
				if (!result.Contains(mediaType))
				{
					result.Add(mediaType);
				}
			}
		}

		return result;
	}

	// ==================================================================
	// Naming & Formatting Helpers
	// ==================================================================

	/// <summary>
	/// Converts a string to PascalCase.
	/// </summary>
	private static string ToPascalCase(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}

		// Handle multi-word segments separated by dashes, underscores, or dots.
		var parts = input.Split(new[] { '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
		{
			return input;
		}

		var result = new System.Text.StringBuilder();
		foreach (var part in parts)
		{
			if (part.Length > 0)
			{
				result.Append(char.ToUpperInvariant(part[0]));
				if (part.Length > 1)
				{
					result.Append(part.Substring(1));
				}
			}
		}

		return result.ToString();
	}

	/// <summary>
	/// Converts the first character of the input to lowercase.
	/// E.g. "Vaccinations" → "vaccinations", "Pets" → "pets".
	/// </summary>
	private static string ToLowerFirstChar(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}

		if (char.IsLower(input[0]))
		{
			return input;
		}

		return char.ToLowerInvariant(input[0]) + input.Substring(1);
	}

	/// <summary>
	/// Sanitizes an OpenAPI enum value string into a valid C# identifier.
	/// Replaces dashes and dots with underscores, ensures the first character
	/// is uppercase.
	/// </summary>
	private static string SanitizeEnumMemberName(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "Unknown";
		}

		var result = new System.Text.StringBuilder(value.Length);
		for (int i = 0; i < value.Length; i++)
		{
			var c = value[i];
			if (c == '-' || c == '.' || c == ' ')
			{
				result.Append('_');
			}
			else if (i == 0 && char.IsDigit(c))
			{
				result.Append('_');
				result.Append(c);
			}
			else if (char.IsLetterOrDigit(c) || c == '_')
			{
				result.Append(c);
			}
			else
			{
				result.Append('_');
			}
		}

		var name = result.ToString();
		if (name.Length > 0)
		{
			name = char.ToUpperInvariant(name[0]) + name.Substring(1);
		}

		return name;
	}

	/// <summary>
	/// Maps an <see cref="OperationType"/> to the conventional C# method name prefix.
	/// </summary>
	private static string GetMethodNameForHttpMethod(OperationType method)
	{
		switch (method)
		{
			case OperationType.Get: return "Get";
			case OperationType.Post: return "Post";
			case OperationType.Put: return "Put";
			case OperationType.Patch: return "Patch";
			case OperationType.Delete: return "Delete";
			case OperationType.Head: return "Head";
			case OperationType.Options: return "Options";
			case OperationType.Trace: return "Trace";
			default: return method.ToString();
		}
	}

	/// <summary>
	/// Extracts the base URL from the OpenAPI document's servers list.
	/// </summary>
	private static string GetBaseUrl(OpenApiDocument document)
	{
		if (document.Servers != null && document.Servers.Count > 0)
		{
			var url = document.Servers[0].Url;
			if (!string.IsNullOrEmpty(url))
			{
				return url.TrimEnd('/');
			}
		}

		return string.Empty;
	}

	/// <summary>
	/// Parses the access modifier string from configuration.
	/// </summary>
	private static AccessModifier ParseAccessModifier(string value)
	{
		if (string.Equals(value, "Internal", StringComparison.OrdinalIgnoreCase))
		{
			return AccessModifier.Internal;
		}

		return AccessModifier.Public;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the schema represents a string enum.
	/// </summary>
	private static bool IsEnumSchema(OpenApiSchema schema)
	{
		return string.Equals(schema.Type, "string", StringComparison.OrdinalIgnoreCase)
			&& schema.Enum != null
			&& schema.Enum.Count > 0;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the schema is a composed type
	/// (top-level <c>oneOf</c> or <c>anyOf</c>) that should be modelled
	/// as a wrapper class implementing <c>IComposedTypeWrapper</c>.
	/// </summary>
	private static bool IsComposedTypeSchema(OpenApiSchema schema)
	{
		if (schema.OneOf != null && schema.OneOf.Count > 0)
		{
			return true;
		}

		if (schema.AnyOf != null && schema.AnyOf.Count > 0)
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the schema represents an object
	/// (or allOf-based model).
	/// </summary>
	private static bool IsObjectSchema(OpenApiSchema schema)
	{
		if (string.Equals(schema.Type, "object", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		// Schema with allOf is also a model (inheritance pattern).
		if (schema.AllOf != null && schema.AllOf.Count > 0)
		{
			return true;
		}

		// Schema with properties but no explicit type.
		if (schema.Properties != null && schema.Properties.Count > 0)
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the schema name identifies a model
	/// that was referenced as an error response (4XX/5XX). Uses the set
	/// populated by <see cref="CollectErrorSchemaNames"/>.
	/// </summary>
	private bool IsErrorSchema(string schemaName)
	{
		return _errorSchemaNames.Contains(schemaName);
	}

	/// <summary>
	/// Pre-scans all API operations to collect schema ref names that are
	/// used as error responses (4XX/5XX). Populates <see cref="_errorSchemaNames"/>.
	/// </summary>
	private void CollectErrorSchemaNames(OpenApiPaths paths)
	{
		foreach (var pathKvp in paths)
		{
			var pathItem = pathKvp.Value;
			if (pathItem?.Operations == null)
			{
				continue;
			}

			foreach (var opKvp in pathItem.Operations)
			{
				var operation = opKvp.Value;
				if (operation?.Responses == null)
				{
					continue;
				}

				foreach (var respKvp in operation.Responses)
				{
					if (IsErrorStatusCode(respKvp.Key))
					{
						var schema = GetResponseSchema(respKvp.Value);
						if (schema?.Reference != null)
						{
							_errorSchemaNames.Add(schema.Reference.Id);
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Returns <see langword="true"/> for error HTTP status codes (4XX, 5XX, or specific 4xx/5xx).
	/// </summary>
	private static bool IsErrorStatusCode(string statusCode)
	{
		if (string.Equals(statusCode, "4XX", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(statusCode, "5XX", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (int.TryParse(statusCode, out var code))
		{
			return code >= 400;
		}

		return false;
	}

	/// <summary>
	/// Extracts the schema name from a <c>$ref</c> string like
	/// <c>#/components/schemas/Pet</c> → <c>Pet</c>.
	/// </summary>
	private static string ExtractSchemaRefName(string refValue)
	{
		if (string.IsNullOrEmpty(refValue))
		{
			return refValue;
		}

		var lastSlash = refValue.LastIndexOf('/');
		return lastSlash >= 0 ? refValue.Substring(lastSlash + 1) : refValue;
	}

	/// <summary>
	/// Gets the name of the parent segment for naming item request builders.
	/// </summary>
	private static string GetParentSegmentName(UrlTreeNode parentNode)
	{
		var segment = parentNode.Segment;
		if (string.IsNullOrEmpty(segment))
		{
			return "Root";
		}

		return ToPascalCase(segment);
	}

	/// <summary>
	/// Cleans a path parameter name for use in item request builder class names.
	/// Strips common suffixes like "Id", "Key", "Number" and PascalCases the result.
	/// E.g. "petId" → "Pet", "ownerId" → "Owner", "name" → "Name".
	/// </summary>
	private static string CleanParameterName(string paramName)
	{
		if (string.IsNullOrEmpty(paramName))
		{
			return "Item";
		}

		// PascalCase the raw parameter name first.
		var pascal = ToPascalCase(paramName);

		// Strip common Id/Key suffixes that Kiota removes for cleaner naming.
		string[] suffixes = { "Id", "Key" };
		foreach (var suffix in suffixes)
		{
			if (pascal.Length > suffix.Length
				&& pascal.EndsWith(suffix, StringComparison.Ordinal))
			{
				var stripped = pascal.Substring(0, pascal.Length - suffix.Length);
				if (stripped.Length > 0)
				{
					return stripped;
				}
			}
		}

		return pascal;
	}

	/// <summary>
	/// Finds the description of a path parameter by name from a PathItem's operations.
	/// Returns null if no description is found.
	/// </summary>
	private static string FindPathParameterDescription(OpenApiPathItem pathItem, string paramName)
	{
		foreach (var opKvp in pathItem.Operations)
		{
			if (opKvp.Value.Parameters != null)
			{
				foreach (var param in opKvp.Value.Parameters)
				{
					if (param.In == ParameterLocation.Path
						&& string.Equals(param.Name, paramName, StringComparison.Ordinal)
						&& !string.IsNullOrEmpty(param.Description))
					{
						return param.Description;
					}
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Computes the lowercase namespace path for indexer descriptions.
	/// Given a namespace like PetStore.Client.Pets.Item, returns
	/// "PetStore.Client.pets.item" by lowercasing segments after the root namespace.
	/// </summary>
	private string GetLowercasePathDescription(CodeNamespace itemNamespace)
	{
		// Compute the fully qualified namespace name.
		var fqn = CSharpConventionService.GetFullyQualifiedName(itemNamespace);
		var rootNs = _config.ClientNamespaceName;

		if (fqn.StartsWith(rootNs + ".", StringComparison.Ordinal))
		{
			var pathPart = fqn.Substring(rootNs.Length + 1);
			var segments = pathPart.Split('.');
			for (int i = 0; i < segments.Length; i++)
			{
				segments[i] = segments[i].ToLowerInvariant();
			}

			return rootNs + "." + string.Join(".", segments);
		}

		return fqn;
	}

	/// <summary>
	/// Finds the first request builder class in a namespace.
	/// </summary>
	private static CodeClass FindRequestBuilderInNamespace(CodeNamespace ns)
	{
		foreach (var cls in ns.Classes)
		{
			if (cls.Kind == CodeClassKind.RequestBuilder)
			{
				return cls;
			}
		}

		return null;
	}

	// ==================================================================
	// Internal URL Tree Node
	// ==================================================================

	/// <summary>
	/// Internal tree node for building a hierarchical URL space from the
	/// flat OpenAPI paths dictionary.
	/// </summary>
	private sealed class UrlTreeNode
	{
		public UrlTreeNode(string segment)
		{
			Segment = segment;
			Children = new Dictionary<string, UrlTreeNode>(StringComparer.Ordinal);
		}

		/// <summary>The path segment name (e.g., "pets", "{petId}").</summary>
		public string Segment { get; }

		/// <summary>The full original path (e.g., "/pets/{petId}"). Set only on terminal nodes.</summary>
		public string FullPath { get; set; }

		/// <summary>The OpenAPI path item at this node, or null for intermediate nodes.</summary>
		public OpenApiPathItem PathItem { get; set; }

		/// <summary>Child nodes keyed by segment name.</summary>
		public Dictionary<string, UrlTreeNode> Children { get; }
	}

	// ==================================================================
	// Deferred Type Reference
	// ==================================================================

	/// <summary>
	/// Tracks a <see cref="CodeType"/> that needs its
	/// <see cref="CodeType.TypeDefinition"/> resolved in Phase 5.
	/// </summary>
	private readonly struct DeferredTypeReference
	{
		public DeferredTypeReference(CodeType typeRef, string referenceName)
		{
			TypeRef = typeRef;
			ReferenceName = referenceName;
		}

		/// <summary>The type reference to resolve.</summary>
		public CodeType TypeRef { get; }

		/// <summary>The name to look up in the CodeDOM tree.</summary>
		public string ReferenceName { get; }
	}
}
