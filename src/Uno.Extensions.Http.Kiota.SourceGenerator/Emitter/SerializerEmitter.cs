using System;
using System.Text;
using Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;
using Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;
using Uno.Extensions.Http.Kiota.SourceGenerator.Refinement;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Emitter;

/// <summary>
/// Emits the <c>Serialize(ISerializationWriter)</c> method body for model
/// classes in the CodeDOM tree. Produces output matching Kiota CLI patterns:
/// <list type="bullet">
///   <item>Null guard: <c>_ = writer ?? throw new ArgumentNullException(nameof(writer));</c></item>
///   <item>Base class delegation: <c>base.Serialize(writer);</c> when the
///   class inherits from another model.</item>
///   <item>Per-property write calls dispatched through the type dispatch
///   table (<c>WriteStringValue</c>, <c>WriteIntValue</c>,
///   <c>WriteObjectValue&lt;T&gt;</c>, etc.).</item>
///   <item>Collection writes: <c>WriteCollectionOfPrimitiveValues&lt;T&gt;</c>
///   for primitive collections, <c>WriteCollectionOfObjectValues&lt;T&gt;</c>
///   for object collections, <c>WriteCollectionOfEnumValues&lt;T&gt;</c>
///   for enum collections.</item>
///   <item>Enum writes: <c>WriteEnumValue&lt;T&gt;</c>.</item>
///   <item>Additional data: <c>writer.WriteAdditionalData(AdditionalData);</c>
///   when <c>IncludeAdditionalData</c> is enabled.</item>
///   <item>Composed-type serialization: delegates to the active member for
///   union types (<c>oneOf</c>) or writes all non-null members for
///   intersection types (<c>anyOf</c>).</item>
/// </list>
/// <para>
/// Called from <see cref="CSharpEmitter.EmitClassBody"/> for each
/// <see cref="CodeMethodKind.Serializer"/> method in the canonical member
/// ordering.
/// </para>
/// </summary>
internal sealed class SerializerEmitter
{
	private readonly KiotaGeneratorConfig _config;

	/// <summary>
	/// Initializes a new <see cref="SerializerEmitter"/> with the given
	/// generator configuration.
	/// </summary>
	/// <param name="config">
	/// The generator configuration controlling additional data and backing
	/// store options.
	/// </param>
	public SerializerEmitter(KiotaGeneratorConfig config)
	{
		_config = config;
	}

	/// <summary>
	/// Emits a complete <c>Serialize(ISerializationWriter)</c> method
	/// declaration into the given <paramref name="writer"/>.
	/// </summary>
	/// <param name="writer">The code writer to emit into.</param>
	/// <param name="method">
	/// The serializer method from the CodeDOM. Must have
	/// <see cref="CodeMethodKind.Serializer"/> kind.
	/// </param>
	/// <param name="cls">The owning model class.</param>
	public void Emit(CodeWriter writer, CodeMethod method, CodeClass cls)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		if (method is null)
		{
			throw new ArgumentNullException(nameof(method));
		}

		if (cls is null)
		{
			throw new ArgumentNullException(nameof(cls));
		}

		// If this is a composed-type wrapper class, delegate to
		// composed-type serialization logic.
		if (IsComposedTypeWrapper(cls))
		{
			EmitComposedTypeSerializer(writer, method, cls);
			return;
		}

		EmitStandardSerializer(writer, method, cls);
	}

	// ==================================================================
	// Standard model serializer
	// ==================================================================

	/// <summary>
	/// Emits the Serialize method for a standard (non-composed) model class.
	/// <para>
	/// Pattern:
	/// <code>
	/// public virtual void Serialize(ISerializationWriter writer)
	/// {
	///     _ = writer ?? throw new ArgumentNullException(nameof(writer));
	///     base.Serialize(writer);  // if has base class
	///     writer.WriteStringValue("name", Name);
	///     writer.WriteIntValue("count", Count);
	///     writer.WriteAdditionalData(AdditionalData);  // if applicable
	/// }
	/// </code>
	/// </para>
	/// </summary>
	private void EmitStandardSerializer(CodeWriter writer, CodeMethod method, CodeClass cls)
	{
		// XML doc summary.
		CSharpEmitter.WriteXmlDocSummary(writer, "Serializes information the current object");

		// XML doc param.
		writer.WriteLine(
			"/// <param name=\"writer\">"
			+ "Serialization writer to use to serialize this model"
			+ "</param>");

		// Method signature.
		var hasBaseClass = cls.BaseClass != null
			&& !IsExternalBaseClass(cls.BaseClass);
		var modifier = hasBaseClass ? "override" : "virtual";

		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(method.Access)
			+ " "
			+ modifier
			+ " void Serialize(ISerializationWriter writer)");
		writer.OpenBlock();

		// Null guard.
		writer.WriteLine(
			"_ = writer ?? throw new ArgumentNullException(nameof(writer));");

		// Base class delegation.
		if (hasBaseClass)
		{
			writer.WriteLine("base.Serialize(writer);");
		}

		// Per-property write calls for Custom properties (sorted alphabetically by name).
		var customProps = new System.Collections.Generic.List<CodeProperty>();
		for (int i = 0; i < cls.Properties.Count; i++)
		{
			if (cls.Properties[i].Kind == CodePropertyKind.Custom)
			{
				customProps.Add(cls.Properties[i]);
			}
		}

		customProps.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));
		for (int i = 0; i < customProps.Count; i++)
		{
			EmitPropertyWriteCall(writer, customProps[i]);
		}

		// Additional data.
		if (HasPropertyOfKind(cls, CodePropertyKind.AdditionalData))
		{
			writer.WriteLine("writer.WriteAdditionalData(AdditionalData);");
		}

		writer.CloseBlock();
	}

	// ==================================================================
	// Composed-type serializer (oneOf / anyOf wrappers)
	// ==================================================================

	/// <summary>
	/// Emits the Serialize method for a composed-type wrapper class.
	/// <para>
	/// For union types (<c>oneOf</c>), only the first non-null member is
	/// serialized using an if/else if chain. For intersection types
	/// (<c>anyOf</c>), all non-null members are serialized with separate
	/// if blocks.
	/// </para>
	/// </summary>
	private void EmitComposedTypeSerializer(CodeWriter writer, CodeMethod method, CodeClass cls)
	{
		// XML doc summary.
		CSharpEmitter.WriteXmlDocSummary(writer, "Serializes information the current object");

		// XML doc param.
		writer.WriteLine(
			"/// <param name=\"writer\">"
			+ "Serialization writer to use to serialize this model"
			+ "</param>");

		// Method signature — composed types use virtual (same as regular models).
		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(method.Access)
			+ " virtual void Serialize(ISerializationWriter writer)");
		writer.OpenBlock();

		// Null guard.
		writer.WriteLine(
			"_ = writer ?? throw new ArgumentNullException(nameof(writer));");

		// Determine if this is a union (oneOf) or intersection (anyOf) by
		// checking whether any Custom property has a CodeUnionType.
		bool isUnion = IsUnionTypeWrapper(cls);

		if (isUnion)
		{
			// Union: if/else if chain — only first non-null match is written.
			// Object types first (alphabetically), then primitive types (alphabetically).
			bool firstBranch = true;

			// First pass: object types.
			for (int i = 0; i < cls.Properties.Count; i++)
			{
				var prop = cls.Properties[i];
				if (prop.Kind != CodePropertyKind.Custom)
				{
					continue;
				}

				if (prop.Type is CodeType ct && CSharpConventionService.IsPrimitiveType(ct.Name))
				{
					continue; // Skip primitives in first pass.
				}

				EmitComposedMemberWrite(writer, prop, ref firstBranch);
			}

			// Second pass: primitive types.
			for (int i = 0; i < cls.Properties.Count; i++)
			{
				var prop = cls.Properties[i];
				if (prop.Kind != CodePropertyKind.Custom)
				{
					continue;
				}

				if (prop.Type is CodeType ct && CSharpConventionService.IsPrimitiveType(ct.Name))
				{
					EmitComposedMemberWrite(writer, prop, ref firstBranch);
				}
			}
		}
		else
		{
			// Intersection (anyOf): emit a single WriteObjectValue call
			// with all members as secondary params.
			var objectMembers = new System.Collections.Generic.List<CodeProperty>();
			for (int i = 0; i < cls.Properties.Count; i++)
			{
				var prop = cls.Properties[i];
				if (prop.Kind == CodePropertyKind.Custom)
				{
					objectMembers.Add(prop);
				}
			}

			if (objectMembers.Count > 0)
			{
				// Use first member's type for generic param, pass all as args.
				var firstTypeRef = CSharpConventionService.GetTypeReference(objectMembers[0].Type);
				var allNames = new System.Collections.Generic.List<string>();
				for (int i = 0; i < objectMembers.Count; i++)
				{
					allNames.Add(objectMembers[i].Name);
				}

				writer.WriteLine(
					"writer.WriteObjectValue<" + firstTypeRef + ">(null, "
					+ string.Join(", ", allNames) + ");");
			}
		}

		writer.CloseBlock();
	}

	/// <summary>
	/// Emits a single composed-type member write within the Serialize method
	/// for union types. Uses if/else if chain.
	/// </summary>
	private void EmitComposedMemberWrite(
		CodeWriter writer,
		CodeProperty prop,
		ref bool firstBranch)
	{
		var typeRef = CSharpConventionService.GetTypeReference(prop.Type);
		var isPrimitive = prop.Type is CodeType ct && CSharpConventionService.IsPrimitiveType(ct.Name);

		// Compose the condition.
		var condition = prop.Name + " != null";

		// Union: if / else if chain — only first match is written.
		if (firstBranch)
		{
			writer.WriteLine("if(" + condition + ")");
			firstBranch = false;
		}
		else
		{
			writer.WriteLine("else if(" + condition + ")");
		}

		writer.OpenBlock();

		if (isPrimitive)
		{
			var writeMethodName = CSharpConventionService.GetSerializationMethodName(prop.Type);
			if (writeMethodName != null)
			{
				writer.WriteLine(
					"writer." + writeMethodName + "(null, " + prop.Name + ");");
			}
		}
		else if (prop.Type is CodeType codeType && IsEnumType(codeType))
		{
			writer.WriteLine(
				"writer.WriteEnumValue<" + typeRef + ">(null, " + prop.Name + ");");
		}
		else
		{
			// Object type — use WriteObjectValue<T>.
			writer.WriteLine(
				"writer.WriteObjectValue<" + typeRef + ">(null, " + prop.Name + ");");
		}

		writer.CloseBlock();
	}

	// ==================================================================
	// Per-property write call emission
	// ==================================================================

	/// <summary>
	/// Emits a single <c>writer.WriteXxxValue()</c> call for a property.
	/// Dispatches to the correct write method based on the property type:
	/// <list type="bullet">
	///   <item>Primitive types → <c>writer.WriteStringValue(...)</c> etc.</item>
	///   <item>Object types → <c>writer.WriteObjectValue&lt;T&gt;(...)</c></item>
	///   <item>Enum types → <c>writer.WriteEnumValue&lt;T&gt;(...)</c></item>
	///   <item>Primitive collections → <c>writer.WriteCollectionOfPrimitiveValues&lt;T&gt;(...)</c></item>
	///   <item>Object collections → <c>writer.WriteCollectionOfObjectValues&lt;T&gt;(...)</c></item>
	///   <item>Enum collections → <c>writer.WriteCollectionOfEnumValues&lt;T&gt;(...)</c></item>
	/// </list>
	/// </summary>
	private static void EmitPropertyWriteCall(CodeWriter writer, CodeProperty prop)
	{
		var serializedName = prop.GetSerializedName();
		var quotedName = "\"" + EscapeStringLiteral(serializedName) + "\"";
		var propName = prop.Name;
		var type = prop.Type;

		if (type is null)
		{
			// Safety — skip properties with no resolved type.
			return;
		}

		if (type.IsCollection)
		{
			EmitCollectionWriteCall(writer, prop, quotedName, propName);
			return;
		}

		// Non-collection types.
		if (type is CodeType codeType)
		{
			// Check if it's a primitive.
			var writeMethod = CSharpConventionService.GetSerializationMethodName(type);
			if (writeMethod != null)
			{
				writer.WriteLine(
					"writer." + writeMethod + "(" + quotedName + ", " + propName + ");");
				return;
			}

			// Check if it's an enum.
			if (IsEnumType(codeType))
			{
				var typeRef = CSharpConventionService.GetTypeReference(type);
				writer.WriteLine(
					"writer.WriteEnumValue<" + typeRef + ">("
					+ quotedName + ", " + propName + ");");
				return;
			}

			// Object type.
			var objTypeRef = CSharpConventionService.GetTypeReference(type);
			writer.WriteLine(
				"writer.WriteObjectValue<" + objTypeRef + ">("
				+ quotedName + ", " + propName + ");");
			return;
		}

		// Fallback for union/intersection types or unresolved — treat as object.
		var fallbackRef = CSharpConventionService.GetTypeReference(type);
		writer.WriteLine(
			"writer.WriteObjectValue<" + fallbackRef + ">("
			+ quotedName + ", " + propName + ");");
	}

	/// <summary>
	/// Emits a write call for a collection property, dispatching between
	/// <c>WriteCollectionOfPrimitiveValues</c>,
	/// <c>WriteCollectionOfObjectValues</c>, and
	/// <c>WriteCollectionOfEnumValues</c>.
	/// </summary>
	private static void EmitCollectionWriteCall(
		CodeWriter writer,
		CodeProperty prop,
		string quotedName,
		string propName)
	{
		var type = prop.Type;
		var elementTypeRef = CSharpConventionService.GetTypeReference(type);

		if (type is CodeType codeType)
		{
			// Primitive collection.
			if (CSharpConventionService.IsPrimitiveType(codeType.Name))
			{
				writer.WriteLine(
					"writer.WriteCollectionOfPrimitiveValues<" + codeType.Name + ">("
					+ quotedName + ", " + propName + ");");
				return;
			}

			// Enum collection.
			if (IsEnumType(codeType))
			{
				writer.WriteLine(
					"writer.WriteCollectionOfEnumValues<" + elementTypeRef + ">("
					+ quotedName + ", " + propName + ");");
				return;
			}

			// Object collection.
			writer.WriteLine(
				"writer.WriteCollectionOfObjectValues<" + elementTypeRef + ">("
				+ quotedName + ", " + propName + ");");
			return;
		}

		// Fallback — treat as object collection.
		writer.WriteLine(
			"writer.WriteCollectionOfObjectValues<" + elementTypeRef + ">("
			+ quotedName + ", " + propName + ");");
	}

	// ==================================================================
	// Helper methods
	// ==================================================================

	/// <summary>
	/// Returns <see langword="true"/> when the class is a composed-type
	/// wrapper (union or intersection), as indicated by implementing the
	/// <c>IComposedTypeWrapper</c> interface.
	/// </summary>
	private static bool IsComposedTypeWrapper(CodeClass cls)
	{
		for (int i = 0; i < cls.Interfaces.Count; i++)
		{
			if (cls.Interfaces[i].Name == "IComposedTypeWrapper")
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the composed-type wrapper is a
	/// union type (oneOf).
	/// <para>
	/// When <see langword="false"/>, the class is an intersection (anyOf).
	/// </para>
	/// </summary>
	private static bool IsUnionTypeWrapper(CodeClass cls)
	{
		return cls.IsUnionType;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the <see cref="CodeType"/>
	/// resolves to a <see cref="CodeEnum"/>.
	/// </summary>
	private static bool IsEnumType(CodeType codeType)
	{
		return codeType.TypeDefinition is CodeEnum;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the base class reference points
	/// to an external type (Kiota runtime, e.g., <c>BaseRequestBuilder</c>
	/// or <c>ApiException</c>) rather than a user-defined model.
	/// </summary>
	private static bool IsExternalBaseClass(CodeType baseClass)
	{
		if (baseClass is null)
		{
			return true;
		}

		return baseClass.IsExternal;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the class has at least one
	/// property of the specified kind.
	/// </summary>
	private static bool HasPropertyOfKind(CodeClass cls, CodePropertyKind kind)
	{
		for (int i = 0; i < cls.Properties.Count; i++)
		{
			if (cls.Properties[i].Kind == kind)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Escapes a string for use inside a C# string literal.
	/// </summary>
	private static string EscapeStringLiteral(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}

		return value
			.Replace("\\", "\\\\")
			.Replace("\"", "\\\"");
	}
}
