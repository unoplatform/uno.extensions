#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;
using Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Refinement;

/// <summary>
/// Applies C#-specific transformations to a language-agnostic CodeDOM tree,
/// preparing it for emission by the <c>CSharpEmitter</c>.
/// <para>
/// Refinement is a single post-processing pass that runs after the CodeDOM
/// has been fully constructed by <see cref="CodeDom.KiotaCodeDomBuilder"/>
/// and after all type definitions have been resolved via
/// <c>MapTypeDefinitions()</c>. It mutates the tree in place.
/// </para>
/// <para>
/// Transformations performed (in order):
/// <list type="number">
///   <item>Set access modifiers from configuration.</item>
///   <item>PascalCase class names, property names, method names.</item>
///   <item>Escape C# reserved words in identifiers.</item>
///   <item>Normalise collection types to <c>List&lt;T&gt;</c>
///   (<see cref="CollectionKind.Complex"/>).</item>
///   <item>Add <c>IAdditionalDataHolder</c> interface and property when
///   <c>IncludeAdditionalData</c> is enabled.</item>
///   <item>Add <c>IBackedModel</c> interface and property when
///   <c>UsesBackingStore</c> is enabled.</item>
///   <item>Mark overriding <c>Serialize</c> / <c>GetFieldDeserializers()</c>
///   methods on derived models.</item>
///   <item>Generate composed-type wrapper classes for
///   <c>CodeUnionType</c> / <c>CodeIntersectionType</c> references.</item>
///   <item>Generate deprecated backward-compatible
///   <c>RequestConfiguration</c> inner classes (unless
///   <c>ExcludeBackwardCompatible</c> is set).</item>
/// </list>
/// </para>
/// </summary>
internal sealed class CSharpRefiner
{
	private readonly KiotaGeneratorConfig _config;

	/// <summary>
	/// Initializes a new <see cref="CSharpRefiner"/> with the given
	/// generator configuration.
	/// </summary>
	/// <param name="config">
	/// The generator configuration controlling optional features such as
	/// <c>IncludeAdditionalData</c>, <c>UsesBackingStore</c>, and
	/// <c>ExcludeBackwardCompatible</c>.
	/// </param>
	public CSharpRefiner(KiotaGeneratorConfig config)
	{
		_config = config;
	}

	// ==================================================================
	// Public entry point
	// ==================================================================

	/// <summary>
	/// Refines the entire CodeDOM tree rooted at <paramref name="root"/>
	/// for C# emission.
	/// </summary>
	/// <param name="root">
	/// The root <see cref="CodeNamespace"/> produced by
	/// <see cref="CodeDom.KiotaCodeDomBuilder"/>. Must not be
	/// <see langword="null"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="root"/> is <see langword="null"/>.
	/// </exception>
	public void Refine(CodeNamespace root)
	{
		if (root is null)
		{
			throw new ArgumentNullException(nameof(root));
		}

		// Determine the access modifier from config.
		var access = ResolveAccessModifier(_config.TypeAccessModifier);

		// Phase 1: Refine all classes (naming, interfaces, properties,
		//          methods, inner classes, composed types, backward compat).
		RefineNamespace(root, access);

		// Phase 2: Refine all enums (naming, access).
		RefineEnumsInNamespace(root, access);
	}

	// ==================================================================
	// Namespace traversal
	// ==================================================================

	/// <summary>
	/// Recursively refines all classes and child namespaces under the given
	/// namespace.
	/// </summary>
	private void RefineNamespace(CodeNamespace ns, AccessModifier access)
	{
		for (int i = 0; i < ns.Classes.Count; i++)
		{
			RefineClass(ns.Classes[i], access);
		}

		for (int i = 0; i < ns.Namespaces.Count; i++)
		{
			RefineNamespace(ns.Namespaces[i], access);
		}
	}

	/// <summary>
	/// Recursively refines all enums under the given namespace.
	/// </summary>
	private void RefineEnumsInNamespace(CodeNamespace ns, AccessModifier access)
	{
		for (int i = 0; i < ns.Enums.Count; i++)
		{
			RefineEnum(ns.Enums[i], access);
		}

		for (int i = 0; i < ns.Namespaces.Count; i++)
		{
			RefineEnumsInNamespace(ns.Namespaces[i], access);
		}
	}

	// ==================================================================
	// Class refinement
	// ==================================================================

	/// <summary>
	/// Refines a single <see cref="CodeClass"/> and its inner classes.
	/// </summary>
	private void RefineClass(CodeClass cls, AccessModifier access)
	{
		// Set access modifier from config.
		cls.Access = access;

		// Ensure class name is PascalCase and safe.
		cls.Name = SanitizeTypeName(cls.Name);

		switch (cls.Kind)
		{
			case CodeClassKind.Model:
				RefineModelClass(cls);
				break;

			case CodeClassKind.RequestBuilder:
				RefineRequestBuilderClass(cls);
				break;

			case CodeClassKind.QueryParameters:
				RefineQueryParametersClass(cls);
				break;

			case CodeClassKind.RequestConfiguration:
				// Nothing extra needed — deprecated placeholder.
				break;
		}

		// Refine properties (naming, types) — all class kinds.
		RefineProperties(cls);

		// Refine methods (naming, override flags) — all class kinds.
		RefineMethods(cls);

		// Refine inner classes recursively.
		for (int i = 0; i < cls.InnerClasses.Count; i++)
		{
			RefineClass(cls.InnerClasses[i], access);
		}

		// Refine indexers (naming).
		RefineIndexers(cls);
	}

	// ==================================================================
	// Model class refinement
	// ==================================================================

	/// <summary>
	/// Applies model-specific refinements: interfaces, additional data,
	/// backing store, override flags on serialization methods, and
	/// composed-type wrappers.
	/// </summary>
	private void RefineModelClass(CodeClass cls)
	{
		// Error models extend ApiException (Kiota Abstractions base class).
		if (cls.IsErrorDefinition && cls.BaseClass == null)
		{
			cls.BaseClass = new CodeType("ApiException", isExternal: true);

			// Rename "Message" property to "MessageEscaped" to avoid
			// conflict with the inherited Exception.Message property.
			// Then add an override "Message" property that delegates to MessageEscaped.
			RenameErrorModelMessageProperty(cls);
		}

		// Derived model classes (those inheriting from another model via allOf)
		// do NOT get IAdditionalDataHolder or AdditionalData — the base class
		// already provides those. Only root model classes need them.
		// Composed-type wrapper classes also skip IAdditionalDataHolder.
		bool isDerivedModel = cls.BaseClass != null
			&& cls.BaseClass.TypeDefinition is CodeClass;
		bool isComposedTypeWrapper = HasInterface(cls, "IComposedTypeWrapper");

		// Add IAdditionalDataHolder interface + property when enabled.
		// Added BEFORE IParsable to match Kiota CLI interface ordering
		// (alphabetical: IAdditionalDataHolder < IParsable).
		if (_config.IncludeAdditionalData && !isDerivedModel && !isComposedTypeWrapper)
		{
			AddAdditionalDataSupport(cls);
		}

		// Ensure IParsable interface is present (after IAdditionalDataHolder).
		EnsureInterface(cls, "IParsable");

		// Add IBackedModel interface + property when enabled.
		if (_config.UsesBackingStore)
		{
			AddBackingStoreSupport(cls);
		}

		// Mark Serialize / GetFieldDeserializers as override when the class
		// has a user-defined base class (allOf inheritance).
		if (isDerivedModel)
		{
			MarkInheritedMethodOverrides(cls);

			// Derived model classes don't need constructors — the base class
			// handles initialization of AdditionalData / BackingStore.
			cls.RemoveMethodsOfKind(CodeMethodKind.Constructor);
		}

		// Generate composed-type wrapper behaviour if any property
		// references a CodeUnionType or CodeIntersectionType.
		RefineComposedTypeProperties(cls);
	}

	/// <summary>
	/// For error models extending <c>ApiException</c>, renames the
	/// "Message" property to "MessageEscaped" and adds a computed
	/// <c>override string Message</c> property that delegates to it.
	/// </summary>
	private static void RenameErrorModelMessageProperty(CodeClass cls)
	{
		CodeProperty messageProp = null;
		for (int i = 0; i < cls.Properties.Count; i++)
		{
			if (string.Equals(cls.Properties[i].Name, "Message", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(cls.Properties[i].Name, "message", StringComparison.OrdinalIgnoreCase))
			{
				messageProp = cls.Properties[i];
				break;
			}
		}

		if (messageProp != null)
		{
			// Rename the serializable property to MessageEscaped.
			messageProp.Name = "MessageEscaped";

			// Add a computed override Message property.
			var messageOverride = new CodeProperty(
				"Message",
				CodePropertyKind.ErrorMessageOverride,
				new CodeType("string", isExternal: true))
			{
				Description = "The primary error message.",
				IsOverride = true,
			};
			cls.AddProperty(messageOverride);
		}
	}

	// ==================================================================
	// Request builder class refinement
	// ==================================================================

	/// <summary>
	/// Applies request-builder-specific refinements: backward-compatible
	/// <c>RequestConfiguration</c> inner classes.
	/// </summary>
	private void RefineRequestBuilderClass(CodeClass cls)
	{
		if (!_config.ExcludeBackwardCompatible)
		{
			AddBackwardCompatibleRequestConfigurations(cls);
		}
	}

	// ==================================================================
	// Query parameters class refinement
	// ==================================================================

	/// <summary>
	/// Applies query-parameters-specific refinements.
	/// </summary>
	private static void RefineQueryParametersClass(CodeClass cls)
	{
		// Ensure query parameter properties have SerializedName set when
		// the original name differs from the PascalCase name.
		for (int i = 0; i < cls.Properties.Count; i++)
		{
			var prop = cls.Properties[i];
			if (prop.Kind == CodePropertyKind.QueryParameter)
			{
				// The serialized name should already be set from the builder.
				// If the PascalCase name would match the serialized name, clear
				// the explicit SerializedName to suppress the attribute.
				var pascal = CSharpConventionService.ToPascalCase(
					CSharpConventionService.ToValidIdentifier(prop.Name));
				if (string.Equals(pascal, prop.SerializedName, StringComparison.Ordinal))
				{
					prop.SerializedName = null;
				}
			}
		}
	}

	// ==================================================================
	// Property refinement
	// ==================================================================

	/// <summary>
	/// Refines all properties on a class: PascalCase naming, reserved-word
	/// escaping, collection normalisation, and serialized-name preservation.
	/// </summary>
	private static void RefineProperties(CodeClass cls)
	{
		for (int i = 0; i < cls.Properties.Count; i++)
		{
			var prop = cls.Properties[i];

			// Preserve the serialized name before renaming. Only for custom
			// properties where the wire name differs from the C# name.
			if (prop.Kind == CodePropertyKind.Custom && prop.SerializedName == null)
			{
				prop.SerializedName = prop.Name;
			}

			// PascalCase the property name (except for structural properties
			// that already have correct names from the builder).
			if (prop.Kind == CodePropertyKind.Custom ||
				prop.Kind == CodePropertyKind.Navigation ||
				prop.Kind == CodePropertyKind.QueryParameter)
			{
				var newName = CSharpConventionService.ToPascalCase(
					CSharpConventionService.ToValidIdentifier(prop.Name));
				prop.Name = CSharpConventionService.EscapeReservedWord(newName);
			}

			// Normalise collection types to List<T> (Complex) for model
			// properties. Array collections remain as arrays (byte[]).
			RefinePropertyType(prop);
		}
	}

	/// <summary>
	/// Refines the type of a single property, normalising collection kinds
	/// for object collections to <c>List&lt;T&gt;</c>.
	/// </summary>
	private static void RefinePropertyType(CodeProperty prop)
	{
		if (prop.Type is null)
		{
			return;
		}

		// Query parameter properties keep array syntax (T[]).
		if (prop.Kind == CodePropertyKind.QueryParameter)
		{
			return;
		}

		// Object-collection properties should use List<T>, not arrays.
		// Exception: byte[] (base64 binary data) stays as an array.
		if (prop.Type.IsCollection && prop.Type.CollectionKind == CollectionKind.Array)
		{
			if (!CSharpConventionService.IsPrimitiveType(prop.Type.Name) ||
				!string.Equals(prop.Type.Name, "byte", StringComparison.Ordinal))
			{
				// Non-byte collections → List<T>.
				prop.Type.CollectionKind = CollectionKind.Complex;
			}
		}
	}

	// ==================================================================
	// Method refinement
	// ==================================================================

	/// <summary>
	/// Refines all methods on a class: naming conventions, parameter naming,
	/// and override detection.
	/// </summary>
	private static void RefineMethods(CodeClass cls)
	{
		for (int i = 0; i < cls.Methods.Count; i++)
		{
			var method = cls.Methods[i];

			// Escape method name if needed (unusual but possible).
			method.Name = CSharpConventionService.EscapeReservedWord(method.Name);

			// Refine parameter names.
			RefineMethodParameters(method);
		}
	}

	/// <summary>
	/// Refines method parameter names: camelCase and reserved-word escaping.
	/// </summary>
	private static void RefineMethodParameters(CodeMethod method)
	{
		for (int i = 0; i < method.Parameters.Count; i++)
		{
			var param = method.Parameters[i];
			var camel = CSharpConventionService.ToCamelCase(
				CSharpConventionService.ToValidIdentifier(param.Name));
			param.Name = CSharpConventionService.EscapeReservedWord(camel);
		}
	}

	// ==================================================================
	// Indexer refinement
	// ==================================================================

	/// <summary>
	/// Refines indexers on a class: parameter naming.
	/// </summary>
	private static void RefineIndexers(CodeClass cls)
	{
		for (int i = 0; i < cls.Indexers.Count; i++)
		{
			var indexer = cls.Indexers[i];

			// Ensure the index parameter name is camelCase and safe.
			if (!string.IsNullOrEmpty(indexer.IndexParameterName))
			{
				var camel = CSharpConventionService.ToCamelCase(
					CSharpConventionService.ToValidIdentifier(indexer.IndexParameterName));
				indexer.IndexParameterName = CSharpConventionService.EscapeReservedWord(camel);
			}
		}
	}

	// ==================================================================
	// Enum refinement
	// ==================================================================

	/// <summary>
	/// Refines a single <see cref="CodeEnum"/>: naming, access modifier,
	/// and option naming.
	/// </summary>
	private static void RefineEnum(CodeEnum codeEnum, AccessModifier access)
	{
		codeEnum.Access = access;

		// Inline enums (extracted from properties) use the convention
		// {ParentClass}_{propertyName} with a literal underscore. Preserve
		// this naming; only PascalCase enum names without underscores.
		if (codeEnum.Name.IndexOf('_') >= 0)
		{
			// Keep the name as-is (it already follows the Kiota convention).
		}
		else
		{
			codeEnum.Name = SanitizeTypeName(codeEnum.Name);
		}

		// Refine each option's C# name.
		for (int i = 0; i < codeEnum.Options.Count; i++)
		{
			var option = codeEnum.Options[i];
			option.Name = CSharpConventionService.ToEnumMemberName(option.SerializedName);
		}

		// Assign power-of-2 values for flags enums.
		if (codeEnum.IsFlags)
		{
			for (int i = 0; i < codeEnum.Options.Count; i++)
			{
				if (codeEnum.Options[i].Value == null)
				{
					codeEnum.Options[i].Value = i == 0 ? 1 : 1 << i;
				}
			}
		}
	}

	// ==================================================================
	// Additional data support
	// ==================================================================

	/// <summary>
	/// Ensures the model class has <c>IAdditionalDataHolder</c> interface
	/// and an <c>AdditionalData</c> property.
	/// </summary>
	private static void AddAdditionalDataSupport(CodeClass cls)
	{
		EnsureInterface(cls, "IAdditionalDataHolder");

		// Add AdditionalData property if not already present.
		if (cls.FindProperty("AdditionalData") == null)
		{
			var adProp = new CodeProperty("AdditionalData", CodePropertyKind.AdditionalData)
			{
				Access = AccessModifier.Public,
				Type = new CodeType("IDictionary<string, object>", isExternal: true)
				{
					IsNullable = false,
				},
				DefaultValue = "new Dictionary<string, object>()",
				Description = "Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.",
			};
			cls.AddProperty(adProp);
		}
	}

	// ==================================================================
	// Backing store support
	// ==================================================================

	/// <summary>
	/// Ensures the model class has <c>IBackedModel</c> interface and a
	/// <c>BackingStore</c> property.
	/// </summary>
	private static void AddBackingStoreSupport(CodeClass cls)
	{
		EnsureInterface(cls, "IBackedModel");

		// Add BackingStore property if not already present.
		if (cls.FindProperty("BackingStore") == null)
		{
			var bsProp = new CodeProperty("BackingStore", CodePropertyKind.BackingStore)
			{
				Access = AccessModifier.Public,
				IsReadOnly = true,
				Type = new CodeType("IBackingStore", isExternal: true)
				{
					IsNullable = false,
				},
				Description = "Stores model information.",
			};
			cls.AddProperty(bsProp);
		}
	}

	// ==================================================================
	// Inheritance override detection
	// ==================================================================

	/// <summary>
	/// Marks <c>Serialize</c> and <c>GetFieldDeserializers</c> methods as
	/// overrides when the class inherits from another model class. Also
	/// marks the factory method with <c>new</c> static semantics.
	/// </summary>
	private static void MarkInheritedMethodOverrides(CodeClass cls)
	{
		for (int i = 0; i < cls.Methods.Count; i++)
		{
			var method = cls.Methods[i];

			switch (method.Kind)
			{
				case CodeMethodKind.Serializer:
				case CodeMethodKind.Deserializer:
					// These override the base class implementation.
					method.IsOverride = true;
					break;

				case CodeMethodKind.Factory:
					// Factory is `new static` to hide the base version.
					method.IsStatic = true;
					break;
			}
		}
	}

	// ==================================================================
	// Composed-type property refinement
	// ==================================================================

	/// <summary>
	/// Scans properties of the given model class for references to
	/// <see cref="CodeUnionType"/> or <see cref="CodeIntersectionType"/>
	/// and ensures that the owning class has <c>IComposedTypeWrapper</c>
	/// interface when the class itself IS the composed-type wrapper.
	/// </summary>
	private static void RefineComposedTypeProperties(CodeClass cls)
	{
		bool hasComposedProperty = false;

		for (int i = 0; i < cls.Properties.Count; i++)
		{
			var prop = cls.Properties[i];
			if (prop.Type is CodeUnionType || prop.Type is CodeIntersectionType)
			{
				hasComposedProperty = true;
			}
		}

		// If properties reference composed types and the class itself
		// has no parent namespace (i.e., it IS the wrapper), add the marker.
		// In practice, KiotaCodeDomBuilder creates wrapper classes directly;
		// we just ensure the interface is declared.
		if (hasComposedProperty)
		{
			EnsureInterface(cls, "IComposedTypeWrapper");
		}
	}

	// ==================================================================
	// Backward-compatible request configuration classes
	// ==================================================================

	/// <summary>
	/// For each HTTP executor method on a request builder that has a query
	/// parameters inner class, generates a deprecated
	/// <c>{Method}RequestConfiguration</c> subclass of the generic
	/// <c>RequestConfiguration&lt;TQueryParams&gt;</c>.
	/// </summary>
	private static void AddBackwardCompatibleRequestConfigurations(CodeClass cls)
	{
		// Collect executor methods that have associated query-params classes.
		var executors = new List<CodeMethod>();
		for (int i = 0; i < cls.Methods.Count; i++)
		{
			if (cls.Methods[i].Kind == CodeMethodKind.RequestExecutor)
			{
				executors.Add(cls.Methods[i]);
			}
		}

		foreach (var executor in executors)
		{
			// Find the corresponding query-parameters inner class for this
			// executor's HTTP method. Convention: {HttpMethod}QueryParameters.
			var httpMethod = executor.HttpMethod;
			if (string.IsNullOrEmpty(httpMethod))
			{
				continue;
			}

			var methodPascal = CSharpConventionService.ToPascalCase(httpMethod.ToLowerInvariant());
			var queryParamsClassName = methodPascal + "QueryParameters";
			var queryParamsClass = cls.FindInnerClass(queryParamsClassName);
			if (queryParamsClass == null)
			{
				continue;
			}

			// Check if the backward-compat class already exists.
			var bcClassName = methodPascal + "RequestConfiguration";
			if (cls.FindInnerClass(bcClassName) != null)
			{
				continue;
			}

			// Build the fully qualified name for the query params type.
			var queryParamsTypeRef = CSharpConventionService.GetGloballyQualifiedName(queryParamsClass);

			// Create the deprecated inner class.
			var bcClass = new CodeClass(bcClassName, CodeClassKind.RequestConfiguration)
			{
				Access = cls.Access,
				Description = "Deprecated. This class is deprecated. Please use the generic RequestConfiguration class generated by the generator.",
			};

			// Set base class to RequestConfiguration<TQueryParams> (external).
			bcClass.BaseClass = new CodeType(
				"RequestConfiguration<" + queryParamsTypeRef + ">",
				isExternal: true);

			// Mark as deprecated.
			// The emitter will check for CodeClassKind.RequestConfiguration
			// to emit the [Obsolete] attribute.

			cls.AddInnerClass(bcClass);
		}
	}

	// ==================================================================
	// Helpers
	// ==================================================================

	/// <summary>
	/// Ensures the class has the specified interface in its Interfaces list.
	/// </summary>
	private static void EnsureInterface(CodeClass cls, string interfaceName)
	{
		for (int i = 0; i < cls.Interfaces.Count; i++)
		{
			if (string.Equals(cls.Interfaces[i].Name, interfaceName, StringComparison.Ordinal))
			{
				return; // Already present.
			}
		}

		cls.AddInterface(new CodeType(interfaceName, isExternal: true));
	}

	/// <summary>
	/// Returns <see langword="true"/> when the class already has the given
	/// interface.
	/// </summary>
	private static bool HasInterface(CodeClass cls, string interfaceName)
	{
		for (int i = 0; i < cls.Interfaces.Count; i++)
		{
			if (string.Equals(cls.Interfaces[i].Name, interfaceName, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Converts a raw type name to a sanitised, PascalCase C# type name.
	/// </summary>
	private static string SanitizeTypeName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return name;
		}

		var valid = CSharpConventionService.ToValidIdentifier(name);
		var pascal = CSharpConventionService.ToPascalCase(valid);
		return CSharpConventionService.EscapeReservedWord(pascal);
	}

	/// <summary>
	/// Resolves the <see cref="AccessModifier"/> from the config string.
	/// </summary>
	private static AccessModifier ResolveAccessModifier(string modifierString)
	{
		if (string.Equals(modifierString, "Internal", StringComparison.OrdinalIgnoreCase))
		{
			return AccessModifier.Internal;
		}

		return AccessModifier.Public;
	}
}
