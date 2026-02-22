using System;
using System.Text;
using Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;
using Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;
using Uno.Extensions.Http.Kiota.SourceGenerator.Refinement;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Emitter;

/// <summary>
/// Emits the <c>GetFieldDeserializers()</c> method body for model classes in
/// the CodeDOM tree. Produces output matching Kiota CLI patterns:
/// <list type="bullet">
///   <item>Return type:
///   <c>IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;</c>.</item>
///   <item>Base class delegation: the dictionary is initialised from
///   <c>base.GetFieldDeserializers()</c> when the class inherits from
///   another model.</item>
///   <item>Per-property entries: each custom property maps its serialized
///   name to a setter lambda that reads the value with the correct
///   <c>GetXxxValue</c> method from <c>IParseNode</c>.</item>
///   <item>Type dispatch:
///     <list type="bullet">
///       <item>Primitive types → <c>n.GetStringValue()</c>,
///       <c>n.GetIntValue()</c>, etc.</item>
///       <item>Object types →
///       <c>n.GetObjectValue&lt;T&gt;(T.CreateFromDiscriminatorValue)</c></item>
///       <item>Enum types → <c>n.GetEnumValue&lt;T&gt;()</c></item>
///       <item>Primitive collections →
///       <c>n.GetCollectionOfPrimitiveValues&lt;T&gt;()?.AsList()</c></item>
///       <item>Object collections →
///       <c>n.GetCollectionOfObjectValues&lt;T&gt;(T.CreateFromDiscriminatorValue)?.AsList()</c></item>
///       <item>Enum collections →
///       <c>n.GetCollectionOfEnumValues&lt;T&gt;()?.AsList()</c></item>
///     </list>
///   </item>
///   <item>Composed-type deserialization: delegates to the active member's
///   <c>GetFieldDeserializers()</c> for union types (<c>oneOf</c>), or
///   returns an empty dictionary for intersection types (<c>anyOf</c>).</item>
/// </list>
/// <para>
/// Called from <see cref="CSharpEmitter.EmitClassBody"/> for each
/// <see cref="CodeMethodKind.Deserializer"/> method in the canonical member
/// ordering.
/// </para>
/// </summary>
internal sealed class DeserializerEmitter
{
	private readonly KiotaGeneratorConfig _config;

	/// <summary>
	/// Initializes a new <see cref="DeserializerEmitter"/> with the given
	/// generator configuration.
	/// </summary>
	/// <param name="config">
	/// The generator configuration controlling additional data and backing
	/// store options.
	/// </param>
	public DeserializerEmitter(KiotaGeneratorConfig config)
	{
		_config = config;
	}

	/// <summary>
	/// Emits a complete <c>GetFieldDeserializers()</c> method declaration
	/// into the given <paramref name="writer"/>.
	/// </summary>
	/// <param name="writer">The code writer to emit into.</param>
	/// <param name="method">
	/// The deserializer method from the CodeDOM. Must have
	/// <see cref="CodeMethodKind.Deserializer"/> kind.
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
		// composed-type deserialization logic.
		if (IsComposedTypeWrapper(cls))
		{
			EmitComposedTypeDeserializer(writer, method, cls);
			return;
		}

		EmitStandardDeserializer(writer, method, cls);
	}

	// ==================================================================
	// Standard model deserializer
	// ==================================================================

	/// <summary>
	/// Emits the GetFieldDeserializers method for a standard (non-composed)
	/// model class.
	/// <para>
	/// Without inheritance:
	/// <code>
	/// public virtual IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;
	///     GetFieldDeserializers()
	/// {
	///     return new Dictionary&lt;string, Action&lt;IParseNode&gt;&gt;
	///     {
	///         { "name", n =&gt; { Name = n.GetStringValue(); } },
	///         { "count", n =&gt; { Count = n.GetIntValue(); } },
	///     };
	/// }
	/// </code>
	/// </para>
	/// <para>
	/// With inheritance:
	/// <code>
	/// public override IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;
	///     GetFieldDeserializers()
	/// {
	///     return new Dictionary&lt;string, Action&lt;IParseNode&gt;&gt;(
	///         base.GetFieldDeserializers())
	///     {
	///         { "color", n =&gt; { Color = n.GetStringValue(); } },
	///     };
	/// }
	/// </code>
	/// </para>
	/// </summary>
	private void EmitStandardDeserializer(CodeWriter writer, CodeMethod method, CodeClass cls)
	{
		// XML doc summary.
		CSharpEmitter.WriteXmlDocSummary(writer, "The deserialization information for the current model");

		// XML doc returns.
		writer.WriteLine(
			"/// <returns>"
			+ "A IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;"
			+ "</returns>");

		// Method signature.
		var hasBaseClass = cls.BaseClass != null
			&& !IsExternalBaseClass(cls.BaseClass);
		var modifier = hasBaseClass ? "override" : "virtual";

		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(method.Access)
			+ " "
			+ modifier
			+ " IDictionary<string, Action<IParseNode>> GetFieldDeserializers()");
		writer.OpenBlock();

		// Open the return statement with the dictionary initializer.
		if (hasBaseClass)
		{
			// Inherit from base deserializers.
			writer.WriteLine("return new Dictionary<string, Action<IParseNode>>(base.GetFieldDeserializers())");
		}
		else
		{
			writer.WriteLine("return new Dictionary<string, Action<IParseNode>>");
		}

		writer.OpenBlock();

		// Per-property entries for Custom properties (sorted alphabetically by name).
		var customProps = new System.Collections.Generic.List<CodeProperty>();
		for (int i = 0; i < cls.Properties.Count; i++)
		{
			if (cls.Properties[i].Kind == CodePropertyKind.Custom)
			{
				customProps.Add(cls.Properties[i]);
			}
		}

		customProps.Sort((a, b) => string.Compare(
			a.GetSerializedName(), b.GetSerializedName(), System.StringComparison.Ordinal));
		int entryCount = 0;
		for (int i = 0; i < customProps.Count; i++)
		{
			EmitPropertyDeserializerEntry(writer, customProps[i]);
			entryCount++;
		}

		// Close the dictionary initializer with };
		writer.DecreaseIndent();
		writer.WriteLine("};");

		writer.CloseBlock();
	}

	// ==================================================================
	// Composed-type deserializer (oneOf / anyOf wrappers)
	// ==================================================================

	/// <summary>
	/// Emits the GetFieldDeserializers method for a composed-type wrapper
	/// class.
	/// <para>
	/// For union types (<c>oneOf</c>), delegates to the first non-null
	/// member's <c>GetFieldDeserializers()</c>. For intersection types
	/// (<c>anyOf</c>), returns an empty dictionary (the runtime handles
	/// deserialization via the factory method).
	/// </para>
	/// </summary>
	private void EmitComposedTypeDeserializer(CodeWriter writer, CodeMethod method, CodeClass cls)
	{
		// XML doc summary.
		CSharpEmitter.WriteXmlDocSummary(writer, "The deserialization information for the current model");

		// XML doc returns.
		writer.WriteLine(
			"/// <returns>"
			+ "A IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;"
			+ "</returns>");

		// Method signature — composed types use virtual (same as regular models).
		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(method.Access)
			+ " virtual IDictionary<string, Action<IParseNode>> GetFieldDeserializers()");
		writer.OpenBlock();

		// Determine if this is a union (oneOf) or intersection (anyOf).
		bool isUnion = IsUnionTypeWrapper(cls);

		if (isUnion)
		{
			// Union: delegate to first non-null member using if/else if chain.
			bool firstBranch = true;
			for (int i = 0; i < cls.Properties.Count; i++)
			{
				var prop = cls.Properties[i];
				if (prop.Kind != CodePropertyKind.Custom)
				{
					continue;
				}

				// Only delegate to object-type members that implement IParsable.
				if (prop.Type is CodeType ct && ct.TypeDefinition is CodeClass)
				{
					if (firstBranch)
					{
						writer.WriteLine("if(" + prop.Name + " != null)");
						firstBranch = false;
					}
					else
					{
						writer.WriteLine("else if(" + prop.Name + " != null)");
					}

					writer.OpenBlock();
					writer.WriteLine("return " + prop.Name + ".GetFieldDeserializers();");
					writer.CloseBlock();
				}
			}
		}
		else
		{
			// Intersection (anyOf): use MergeDeserializersForIntersectionWrapper
			// when any member is non-null.
			var objectMembers = new System.Collections.Generic.List<CodeProperty>();
			for (int i = 0; i < cls.Properties.Count; i++)
			{
				var prop = cls.Properties[i];
				if (prop.Kind == CodePropertyKind.Custom
					&& prop.Type is CodeType ct
					&& ct.TypeDefinition is CodeClass)
				{
					objectMembers.Add(prop);
				}
			}

			if (objectMembers.Count > 0)
			{
				// Build condition: member1 != null || member2 != null || ...
				var conditions = new System.Collections.Generic.List<string>();
				var names = new System.Collections.Generic.List<string>();
				for (int i = 0; i < objectMembers.Count; i++)
				{
					conditions.Add(objectMembers[i].Name + " != null");
					names.Add(objectMembers[i].Name);
				}

				writer.WriteLine("if(" + string.Join(" || ", conditions) + ")");
				writer.OpenBlock();
				writer.WriteLine(
					"return ParseNodeHelper.MergeDeserializersForIntersectionWrapper("
					+ string.Join(", ", names) + ");");
				writer.CloseBlock();
			}
		}

		// Fall through: return empty dictionary.
		writer.WriteLine("return new Dictionary<string, Action<IParseNode>>();");

		writer.CloseBlock();
	}

	// ==================================================================
	// Per-property deserializer entry emission
	// ==================================================================

	/// <summary>
	/// Emits a single dictionary entry in the
	/// <c>GetFieldDeserializers()</c> return block:
	/// <c>{ "serializedName", n =&gt; { PropertyName = n.GetXxxValue(); } },</c>
	/// <para>
	/// Dispatches to the correct <c>IParseNode</c> method based on the
	/// property type:
	/// </para>
	/// <list type="bullet">
	///   <item>Primitive types → <c>n.GetStringValue()</c> etc.</item>
	///   <item>Object types →
	///   <c>n.GetObjectValue&lt;T&gt;(T.CreateFromDiscriminatorValue)</c></item>
	///   <item>Enum types → <c>n.GetEnumValue&lt;T&gt;()</c></item>
	///   <item>Primitive collections →
	///   <c>n.GetCollectionOfPrimitiveValues&lt;T&gt;()?.AsList()</c></item>
	///   <item>Object collections →
	///   <c>n.GetCollectionOfObjectValues&lt;T&gt;(T.CreateFromDiscriminatorValue)?.AsList()</c></item>
	///   <item>Enum collections →
	///   <c>n.GetCollectionOfEnumValues&lt;T&gt;()?.AsList()</c></item>
	/// </list>
	/// </summary>
	private static void EmitPropertyDeserializerEntry(CodeWriter writer, CodeProperty prop)
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
			EmitCollectionDeserializerEntry(writer, prop, quotedName, propName);
			return;
		}

		// Non-collection types.
		if (type is CodeType codeType)
		{
			// Check if it's a primitive.
			var parseMethod = CSharpConventionService.GetDeserializationMethodName(type);
			if (parseMethod != null)
			{
				writer.WriteLine(
					"{ " + quotedName + ", n => { "
					+ propName + " = n." + parseMethod + "(); } },");
				return;
			}

			// Check if it's an enum.
			if (IsEnumType(codeType))
			{
				var typeRef = CSharpConventionService.GetTypeReference(type);
				writer.WriteLine(
					"{ " + quotedName + ", n => { "
					+ propName + " = n.GetEnumValue<" + typeRef + ">(); } },");
				return;
			}

			// Object type.
			var objTypeRef = CSharpConventionService.GetTypeReference(type);
			writer.WriteLine(
				"{ " + quotedName + ", n => { "
				+ propName + " = n.GetObjectValue<" + objTypeRef + ">("
				+ objTypeRef + ".CreateFromDiscriminatorValue); } },");
			return;
		}

		// Fallback for union/intersection types or unresolved — treat as object.
		var fallbackRef = CSharpConventionService.GetTypeReference(type);
		writer.WriteLine(
			"{ " + quotedName + ", n => { "
			+ propName + " = n.GetObjectValue<" + fallbackRef + ">("
			+ fallbackRef + ".CreateFromDiscriminatorValue); } },");
	}

	/// <summary>
	/// Emits a deserializer entry for a collection property, dispatching
	/// between <c>GetCollectionOfPrimitiveValues</c>,
	/// <c>GetCollectionOfObjectValues</c>, and
	/// <c>GetCollectionOfEnumValues</c>.
	/// </summary>
	private static void EmitCollectionDeserializerEntry(
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
					"{ " + quotedName + ", n => { "
					+ propName + " = n.GetCollectionOfPrimitiveValues<" + codeType.Name + ">()"
					+ "?.AsList(); } },");
				return;
			}

			// Enum collection.
			if (IsEnumType(codeType))
			{
				writer.WriteLine(
					"{ " + quotedName + ", n => { "
					+ propName + " = n.GetCollectionOfEnumValues<" + elementTypeRef + ">()"
					+ "?.AsList(); } },");
				return;
			}

			// Object collection.
			writer.WriteLine(
				"{ " + quotedName + ", n => { "
				+ propName + " = n.GetCollectionOfObjectValues<" + elementTypeRef + ">("
				+ elementTypeRef + ".CreateFromDiscriminatorValue)"
				+ "?.AsList(); } },");
			return;
		}

		// Fallback — treat as object collection.
		writer.WriteLine(
			"{ " + quotedName + ", n => { "
			+ propName + " = n.GetCollectionOfObjectValues<" + elementTypeRef + ">("
			+ elementTypeRef + ".CreateFromDiscriminatorValue)"
			+ "?.AsList(); } },");
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
	/// union type (oneOf). Determined by checking the class description for
	/// "union" markers set during CodeDOM construction.
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
