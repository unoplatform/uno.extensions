#nullable disable

using System;
using System.Collections.Generic;
using Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;
using Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;
using Uno.Extensions.Http.Kiota.SourceGenerator.Refinement;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Emitter;

/// <summary>
/// Emits the <c>CreateFromDiscriminatorValue</c> static factory method for
/// model classes in the CodeDOM tree. Produces output matching Kiota CLI
/// patterns:
/// <list type="bullet">
///   <item><b>Simple model</b> (no discriminator): returns
///   <c>new global::Ns.ModelName()</c> directly.</item>
///   <item><b>Discriminated model</b> (inheritance with discriminator
///   mappings): reads the discriminator property from the parse node via
///   <c>parseNode.GetChildNode("prop")?.GetStringValue()</c>, then returns
///   the matching concrete type from a <c>switch</c> expression with a
///   default fallback to the base class.</item>
///   <item><b>Derived class</b> (child in an inheritance hierarchy): uses
///   <c>new</c> modifier and returns the derived type directly without a
///   discriminator switch (the switch lives on the base class).</item>
///   <item><b>Composed-type wrapper</b>: creates a new wrapper instance
///   and returns it; the actual type resolution is deferred to
///   deserialization.</item>
/// </list>
/// <para>
/// Called from <see cref="CSharpEmitter.EmitClassBody"/> for each
/// <see cref="CodeMethodKind.Factory"/> method in the canonical member
/// ordering.
/// </para>
/// </summary>
internal sealed class FactoryMethodEmitter
{
	private readonly KiotaGeneratorConfig _config;

	/// <summary>
	/// Initializes a new <see cref="FactoryMethodEmitter"/> with the given
	/// generator configuration.
	/// </summary>
	/// <param name="config">
	/// The generator configuration controlling namespace and type naming.
	/// </param>
	public FactoryMethodEmitter(KiotaGeneratorConfig config)
	{
		_config = config;
	}

	/// <summary>
	/// Emits a complete <c>CreateFromDiscriminatorValue</c> static factory
	/// method declaration into the given <paramref name="writer"/>.
	/// </summary>
	/// <param name="writer">The code writer to emit into.</param>
	/// <param name="method">
	/// The factory method from the CodeDOM. Must have
	/// <see cref="CodeMethodKind.Factory"/> kind.
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

		// Resolve the fully qualified type name for the owning class.
		var qualifiedName = CSharpConventionService.GetGloballyQualifiedName(cls);

		// Check if this is a composed-type wrapper.
		if (IsComposedTypeWrapper(cls))
		{
			EmitComposedTypeFactory(writer, method, cls, qualifiedName);
			return;
		}

		// Determine if the class has discriminator mappings.
		bool hasDiscriminator = !string.IsNullOrEmpty(cls.DiscriminatorPropertyName)
			&& cls.DiscriminatorMappings.Count > 0;

		if (hasDiscriminator)
		{
			EmitDiscriminatedFactory(writer, method, cls, qualifiedName);
		}
		else
		{
			EmitSimpleFactory(writer, method, cls, qualifiedName);
		}
	}

	// ==================================================================
	// Simple factory (no discriminator)
	// ==================================================================

	/// <summary>
	/// Emits a simple factory method that creates a new instance of the
	/// owning class without discriminator checking.
	/// <para>
	/// For a base class without discriminator:
	/// <code>
	/// public static global::Ns.ModelName
	///     CreateFromDiscriminatorValue(IParseNode parseNode)
	/// {
	///     _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
	///     return new global::Ns.ModelName();
	/// }
	/// </code>
	/// </para>
	/// <para>
	/// For a derived class (child of an inheritance hierarchy), the
	/// <c>new</c> modifier is emitted to shadow the base class factory:
	/// <code>
	/// public new static global::Ns.DerivedName
	///     CreateFromDiscriminatorValue(IParseNode parseNode)
	/// {
	///     _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
	///     return new global::Ns.DerivedName();
	/// }
	/// </code>
	/// </para>
	/// </summary>
	private static void EmitSimpleFactory(
		CodeWriter writer,
		CodeMethod method,
		CodeClass cls,
		string qualifiedName)
	{
		// XML doc summary.
		CSharpEmitter.WriteXmlDocSummary(
			writer,
			"Creates a new instance of the appropriate class based on discriminator value");

		// XML doc returns.
		writer.WriteLine(
			"/// <returns>A <see cref=\"" + qualifiedName + "\"/></returns>");

		// XML doc param.
		writer.WriteLine(
			"/// <param name=\"parseNode\">"
			+ "The parse node to use to read the discriminator value and create the object"
			+ "</param>");

		// Method signature.
		// Derived classes use "new" to shadow the base factory.
		var hasNonExternalBase = cls.BaseClass != null && !cls.BaseClass.IsExternal;
		var newModifier = hasNonExternalBase ? "new " : string.Empty;

		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(method.Access)
			+ " static "
			+ newModifier
			+ qualifiedName
			+ " CreateFromDiscriminatorValue(IParseNode parseNode)");
		writer.OpenBlock();

		// Null guard.
		writer.WriteLine(
			"_ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));");

		// Return new instance.
		writer.WriteLine("return new " + qualifiedName + "();");

		writer.CloseBlock();
	}

	// ==================================================================
	// Discriminated factory (with discriminator switch)
	// ==================================================================

	/// <summary>
	/// Emits a factory method with discriminator-based type selection:
	/// <code>
	/// public static global::Ns.Animal
	///     CreateFromDiscriminatorValue(IParseNode parseNode)
	/// {
	///     _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
	///     var mappingValue = parseNode.GetChildNode("@odata.type")
	///         ?.GetStringValue();
	///     return mappingValue switch
	///     {
	///         "#/components/schemas/Cat" =&gt;
	///             new global::Ns.Cat(),
	///         "#/components/schemas/Dog" =&gt;
	///             new global::Ns.Dog(),
	///         _ =&gt; new global::Ns.Animal(),
	///     };
	/// }
	/// </code>
	/// </summary>
	private static void EmitDiscriminatedFactory(
		CodeWriter writer,
		CodeMethod method,
		CodeClass cls,
		string qualifiedName)
	{
		// XML doc summary.
		CSharpEmitter.WriteXmlDocSummary(
			writer,
			"Creates a new instance of the appropriate class based on discriminator value");

		// XML doc returns.
		writer.WriteLine(
			"/// <returns>A <see cref=\"" + qualifiedName + "\"/></returns>");

		// XML doc param.
		writer.WriteLine(
			"/// <param name=\"parseNode\">"
			+ "The parse node to use to read the discriminator value and create the object"
			+ "</param>");

		// Method signature — same as simple but always on base class
		// (base classes with discriminator don't typically have the "new"
		// modifier, but derived classes might also define one separately).
		var hasNonExternalBase = cls.BaseClass != null && !cls.BaseClass.IsExternal;
		var newModifier = hasNonExternalBase ? "new " : string.Empty;

		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(method.Access)
			+ " static "
			+ newModifier
			+ qualifiedName
			+ " CreateFromDiscriminatorValue(IParseNode parseNode)");
		writer.OpenBlock();

		// Null guard.
		writer.WriteLine(
			"_ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));");

		// Read discriminator value.
		writer.WriteLine(
			"var mappingValue = parseNode.GetChildNode(\""
			+ EscapeStringLiteral(cls.DiscriminatorPropertyName)
			+ "\")?.GetStringValue();");

		// Switch expression on discriminator value.
		writer.WriteLine("return mappingValue switch");
		writer.OpenBlock();

		// Emit one arm per discriminator mapping (sorted alphabetically by key
		// to match Kiota CLI output).
		var sortedMappings = new List<KeyValuePair<string, CodeType>>(cls.DiscriminatorMappings);
		sortedMappings.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));

		foreach (var mapping in sortedMappings)
		{
			var discriminatorValue = mapping.Key;
			var targetType = mapping.Value;

			// Resolve the fully qualified target type name.
			string targetQualifiedName;
			if (targetType.TypeDefinition != null)
			{
				targetQualifiedName = CSharpConventionService.GetGloballyQualifiedName(
					targetType.TypeDefinition);
			}
			else
			{
				targetQualifiedName = CSharpConventionService.GetTypeReference(targetType);
			}

			writer.WriteLine(
				"\""
				+ EscapeStringLiteral(discriminatorValue)
				+ "\" => new "
				+ targetQualifiedName
				+ "(),");
		}

		// Default arm — create the base class instance.
		writer.WriteLine("_ => new " + qualifiedName + "(),");

		// Close the switch block with };
		writer.DecreaseIndent();
		writer.WriteLine("};");

		writer.CloseBlock();
	}

	// ==================================================================
	// Composed-type factory (oneOf / anyOf wrappers)
	// ==================================================================

	/// <summary>
	/// Emits the factory method for a composed-type wrapper class.
	/// Handles three patterns:
	/// <list type="bullet">
	///   <item>oneOf with discriminator: if/else if chain setting specific
	///   member based on discriminator value, plus primitive try-parse for
	///   non-object constituents.</item>
	///   <item>anyOf (intersection): create ALL members.</item>
	///   <item>oneOf primitives only: try each primitive parse.</item>
	/// </list>
	/// </summary>
	private void EmitComposedTypeFactory(
		CodeWriter writer,
		CodeMethod method,
		CodeClass cls,
		string qualifiedName)
	{
		// XML doc.
		CSharpEmitter.WriteXmlDocSummary(
			writer,
			"Creates a new instance of the appropriate class based on discriminator value");
		writer.WriteLine(
			"/// <returns>A <see cref=\"" + qualifiedName + "\"/></returns>");
		writer.WriteLine(
			"/// <param name=\"parseNode\">"
			+ "The parse node to use to read the discriminator value and create the object"
			+ "</param>");

		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(method.Access)
			+ " static "
			+ qualifiedName
			+ " CreateFromDiscriminatorValue(IParseNode parseNode)");
		writer.OpenBlock();

		// Null guard.
		writer.WriteLine(
			"_ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));");

		bool isUnion = IsUnionTypeWrapper(cls);
		bool hasDiscriminator = !string.IsNullOrEmpty(cls.DiscriminatorPropertyName)
			&& cls.DiscriminatorMappings.Count > 0;

		// Collect member properties by kind.
		var objectMembers = new List<CodeProperty>();
		var primitiveMembers = new List<CodeProperty>();
		for (int i = 0; i < cls.Properties.Count; i++)
		{
			var prop = cls.Properties[i];
			if (prop.Kind != CodePropertyKind.Custom)
			{
				continue;
			}

			if (prop.Type is CodeType ct && CSharpConventionService.IsPrimitiveType(ct.Name))
			{
				primitiveMembers.Add(prop);
			}
			else
			{
				objectMembers.Add(prop);
			}
		}

		if (isUnion && hasDiscriminator)
		{
			// Read discriminator value.
			writer.WriteLine(
				"var mappingValue = parseNode.GetChildNode(\""
				+ EscapeStringLiteral(cls.DiscriminatorPropertyName)
				+ "\")?.GetStringValue();");
		}
		else if (isUnion)
		{
			// oneOf without explicit discriminator:
			// still read discriminator from empty string property (Kiota convention).
			writer.WriteLine(
				"var mappingValue = parseNode.GetChildNode(\"\")"
				+ "?.GetStringValue();");
		}

		writer.WriteLine(
			"var result = new " + qualifiedName + "();");

		if (isUnion)
		{
			// Emit if/else if chain for object members.
			bool firstBranch = true;

			if (hasDiscriminator)
			{
				// Use discriminator mappings.
				var sortedMappings = new List<KeyValuePair<string, CodeType>>(cls.DiscriminatorMappings);
				sortedMappings.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));

				foreach (var mapping in sortedMappings)
				{
					var discriminatorValue = mapping.Key;
					var targetType = mapping.Value;

					// Find the property that matches this mapping target.
					string propName = null;
					string targetQualifiedName = null;
					for (int i = 0; i < objectMembers.Count; i++)
					{
						var p = objectMembers[i];
						if (p.Type is CodeType pct && pct.TypeDefinition != null
							&& string.Equals(pct.TypeDefinition.Name, targetType.Name, StringComparison.Ordinal))
						{
							propName = p.Name;
							targetQualifiedName = CSharpConventionService.GetGloballyQualifiedName(pct.TypeDefinition);
							break;
						}
					}

					if (propName == null)
					{
						continue;
					}

					if (firstBranch)
					{
						writer.WriteLine(
							"if(\"" + EscapeStringLiteral(discriminatorValue)
							+ "\".Equals(mappingValue, StringComparison.OrdinalIgnoreCase))");
						firstBranch = false;
					}
					else
					{
						writer.WriteLine(
							"else if(\"" + EscapeStringLiteral(discriminatorValue)
							+ "\".Equals(mappingValue, StringComparison.OrdinalIgnoreCase))");
					}

					writer.OpenBlock();
					writer.WriteLine(
						"result." + propName + " = new " + targetQualifiedName + "();");
					writer.CloseBlock();
				}
			}
			else if (objectMembers.Count > 0)
			{
				// No discriminator, use schema name as matching value.
				for (int i = 0; i < objectMembers.Count; i++)
				{
					var p = objectMembers[i];
					string targetQualifiedName;
					if (p.Type is CodeType pct && pct.TypeDefinition != null)
					{
						targetQualifiedName = CSharpConventionService.GetGloballyQualifiedName(pct.TypeDefinition);
					}
					else
					{
						targetQualifiedName = CSharpConventionService.GetTypeReference(p.Type);
					}

					if (firstBranch)
					{
						writer.WriteLine(
							"if(\"" + EscapeStringLiteral(p.Name)
							+ "\".Equals(mappingValue, StringComparison.OrdinalIgnoreCase))");
						firstBranch = false;
					}
					else
					{
						writer.WriteLine(
							"else if(\"" + EscapeStringLiteral(p.Name)
							+ "\".Equals(mappingValue, StringComparison.OrdinalIgnoreCase))");
					}

					writer.OpenBlock();
					writer.WriteLine(
						"result." + p.Name + " = new " + targetQualifiedName + "();");
					writer.CloseBlock();
				}
			}

			// Primitive members: try-parse using else if chain.
			for (int i = 0; i < primitiveMembers.Count; i++)
			{
				var p = primitiveMembers[i];
				var parseMethod = CSharpConventionService.GetDeserializationMethodName(p.Type);
				if (parseMethod == null)
				{
					continue;
				}

				var varName = ToCamelCase(p.Name) + "Value";

				if (firstBranch)
				{
					writer.WriteLine(
						"if(parseNode." + parseMethod + "() is "
						+ p.Type.Name + " " + varName + ")");
					firstBranch = false;
				}
				else
				{
					writer.WriteLine(
						"else if(parseNode." + parseMethod + "() is "
						+ p.Type.Name + " " + varName + ")");
				}

				writer.OpenBlock();
				writer.WriteLine("result." + p.Name + " = " + varName + ";");
				writer.CloseBlock();
			}
		}
		else
		{
			// Intersection (anyOf): create ALL object members.
			for (int i = 0; i < objectMembers.Count; i++)
			{
				var p = objectMembers[i];
				string targetQualifiedName;
				if (p.Type is CodeType pct && pct.TypeDefinition != null)
				{
					targetQualifiedName = CSharpConventionService.GetGloballyQualifiedName(pct.TypeDefinition);
				}
				else
				{
					targetQualifiedName = CSharpConventionService.GetTypeReference(p.Type);
				}

				writer.WriteLine(
					"result." + p.Name + " = new " + targetQualifiedName + "();");
			}
		}

		writer.WriteLine("return result;");

		writer.CloseBlock();
	}

	// ==================================================================
	// Helper methods
	// ==================================================================

	/// <summary>
	/// Returns <see langword="true"/> when the class is a composed-type
	/// wrapper (union or intersection), as indicated by implementing
	/// <c>IComposedTypeWrapper</c>.
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
	/// Returns <see langword="true"/> when the composed-type wrapper is
	/// a union type (oneOf).
	/// </summary>
	private static bool IsUnionTypeWrapper(CodeClass cls)
	{
		return cls.IsUnionType;
	}

	/// <summary>
	/// Converts a string to camelCase.
	/// </summary>
	private static string ToCamelCase(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}

		return char.ToLowerInvariant(input[0]) + input.Substring(1);
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
