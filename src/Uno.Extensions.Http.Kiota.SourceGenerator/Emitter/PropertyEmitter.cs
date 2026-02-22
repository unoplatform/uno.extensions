using System;
using System.Text;
using Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;
using Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;
using Uno.Extensions.Http.Kiota.SourceGenerator.Refinement;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Emitter;

/// <summary>
/// Emits C# property declarations from <see cref="CodeProperty"/> nodes in the
/// CodeDOM tree, producing output that matches Kiota CLI patterns including:
/// <list type="bullet">
///   <item><c>#if NETSTANDARD2_1_OR_GREATER</c> nullable reference-type guards</item>
///   <item>Backing-store delegate properties when <c>UsesBackingStore</c> is enabled</item>
///   <item><c>[QueryParameter("...")]</c> attributes for query parameter properties</item>
///   <item>Navigation property getters that construct child request builders</item>
///   <item><c>global::</c>-prefixed type references for internal types</item>
///   <item>Read-only properties with default value initializers</item>
/// </list>
/// <para>
/// Called from <see cref="CSharpEmitter.EmitClassBody"/> for each property
/// in the canonical member ordering.
/// </para>
/// </summary>
internal sealed class PropertyEmitter
{
	private readonly KiotaGeneratorConfig _config;

	/// <summary>
	/// Initializes a new <see cref="PropertyEmitter"/> with the given
	/// generator configuration.
	/// </summary>
	/// <param name="config">
	/// The generator configuration controlling backing store and additional
	/// data options.
	/// </param>
	public PropertyEmitter(KiotaGeneratorConfig config)
	{
		_config = config;
	}

	/// <summary>
	/// Emits a single property declaration into the given <paramref name="writer"/>.
	/// </summary>
	/// <param name="writer">The code writer to emit into.</param>
	/// <param name="prop">The property to emit.</param>
	/// <param name="cls">The owning class (used for context such as class kind).</param>
	public void Emit(CodeWriter writer, CodeProperty prop, CodeClass cls)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		if (prop is null)
		{
			throw new ArgumentNullException(nameof(prop));
		}

		switch (prop.Kind)
		{
			case CodePropertyKind.Navigation:
				EmitNavigationProperty(writer, prop);
				break;

			case CodePropertyKind.Custom:
				EmitCustomProperty(writer, prop);
				break;

			case CodePropertyKind.AdditionalData:
				EmitAdditionalDataProperty(writer, prop);
				break;

			case CodePropertyKind.BackingStore:
				EmitBackingStoreProperty(writer, prop);
				break;

			case CodePropertyKind.QueryParameter:
				EmitQueryParameterProperty(writer, prop);
				break;

			case CodePropertyKind.UrlTemplate:
				EmitSimpleProperty(writer, prop);
				break;

			case CodePropertyKind.PathParameters:
				EmitSimpleProperty(writer, prop);
				break;

			case CodePropertyKind.RequestAdapter:
				EmitSimpleProperty(writer, prop);
				break;

			default:
				EmitSimpleProperty(writer, prop);
				break;
		}
	}

	// ==================================================================
	// Navigation properties
	// ==================================================================

	/// <summary>
	/// Emits a navigation property that constructs a child request builder.
	/// <para>
	/// Pattern:
	/// <code>
	/// /// &lt;summary&gt;{description}&lt;/summary&gt;
	/// public global::Ns.ChildRequestBuilder Child
	/// {
	///     get =&gt; new global::Ns.ChildRequestBuilder(PathParameters, RequestAdapter);
	/// }
	/// </code>
	/// </para>
	/// </summary>
	private void EmitNavigationProperty(CodeWriter writer, CodeProperty prop)
	{
		// XML doc summary (single-line for properties).
		CSharpEmitter.WriteXmlDocSummarySingleLine(writer, prop.Description);

		// Deprecation attribute.
		WriteDeprecationAttribute(writer, prop);

		var typeString = CSharpConventionService.GetTypeString(prop.Type);

		// Property declaration with getter-only body.
		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(prop.Access)
			+ " "
			+ typeString
			+ " "
			+ prop.Name);
		writer.OpenBlock();

		// The navigation getter constructs the child request builder,
		// forwarding PathParameters and RequestAdapter.
		writer.WriteLine(
			"get => new "
			+ typeString
			+ "(PathParameters, RequestAdapter);");

		writer.CloseBlock();
	}

	// ==================================================================
	// Custom (model) properties
	// ==================================================================

	/// <summary>
	/// Emits a custom (model) property, which may use:
	/// <list type="bullet">
	///   <item>Nullable reference-type conditional compilation guards</item>
	///   <item>Backing-store delegate get/set when <c>UsesBackingStore</c> is enabled</item>
	///   <item>Simple auto-property when neither applies</item>
	/// </list>
	/// </summary>
	private void EmitCustomProperty(CodeWriter writer, CodeProperty prop)
	{
		// XML doc summary (single-line for properties).
		CSharpEmitter.WriteXmlDocSummarySingleLine(writer, prop.Description);

		// Deprecation attribute.
		WriteDeprecationAttribute(writer, prop);

		if (_config.UsesBackingStore)
		{
			EmitBackingStoreAccessorProperty(writer, prop);
		}
		else if (CSharpConventionService.RequiresNullableGuard(prop.Type))
		{
			EmitNullableGuardedAutoProperty(writer, prop);
		}
		else
		{
			EmitAutoProperty(writer, prop);
		}
	}

	// ==================================================================
	// AdditionalData property
	// ==================================================================

	/// <summary>
	/// Emits the <c>AdditionalData</c> property. When backing store is
	/// enabled, delegates to the store; otherwise emits a simple auto-property.
	/// </summary>
	private void EmitAdditionalDataProperty(CodeWriter writer, CodeProperty prop)
	{
		// XML doc summary (single-line for properties).
		CSharpEmitter.WriteXmlDocSummarySingleLine(writer, prop.Description);

		if (_config.UsesBackingStore)
		{
			EmitBackingStoreAccessorProperty(writer, prop);
		}
		else if (CSharpConventionService.RequiresNullableGuard(prop.Type))
		{
			EmitNullableGuardedAutoProperty(writer, prop);
		}
		else
		{
			EmitAutoProperty(writer, prop);
		}
	}

	// ==================================================================
	// BackingStore property
	// ==================================================================

	/// <summary>
	/// Emits the <c>BackingStore</c> property itself:
	/// <code>
	/// public IBackingStore BackingStore { get; private set; }
	/// </code>
	/// </summary>
	private void EmitBackingStoreProperty(CodeWriter writer, CodeProperty prop)
	{
		// XML doc summary (single-line for properties).
		CSharpEmitter.WriteXmlDocSummarySingleLine(writer, prop.Description);

		var typeString = CSharpConventionService.GetTypeString(prop.Type, includeNullable: false);

		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(prop.Access)
			+ " "
			+ typeString
			+ " "
			+ prop.Name
			+ " { get; private set; }");
	}

	// ==================================================================
	// QueryParameter properties
	// ==================================================================

	/// <summary>
	/// Emits a query parameter property with an optional
	/// <c>[QueryParameter("...")]</c> attribute when the serialized name
	/// differs from the C# property name.
	/// </summary>
	private void EmitQueryParameterProperty(CodeWriter writer, CodeProperty prop)
	{
		// XML doc summary (single-line for properties).
		CSharpEmitter.WriteXmlDocSummarySingleLine(writer, prop.Description);

		// Deprecation attribute.
		WriteDeprecationAttribute(writer, prop);

		// [QueryParameter("original-name")] when the serialized name differs.
		var serializedName = prop.GetSerializedName();
		var hasQpAttr = !string.Equals(serializedName, prop.Name, StringComparison.Ordinal);
		var qpAttrLine = hasQpAttr
			? "[QueryParameter(\"" + EscapeStringLiteral(serializedName) + "\")]"
			: null;

		if (CSharpConventionService.RequiresNullableGuard(prop.Type))
		{
			// The [QueryParameter] attribute must appear inside each #if/#else branch.
			var typeString = CSharpConventionService.GetTypeString(prop.Type, includeNullable: false);
			var accessMod = CSharpConventionService.GetAccessModifier(prop.Access);
			var accessor = prop.IsReadOnly ? " { get; }" : " { get; set; }";

			writer.WriteLineRaw("#if " + CSharpConventionService.NullableEnableCondition);
			writer.WriteLineRaw("#nullable enable");
			if (qpAttrLine != null)
			{
				writer.WriteLine(qpAttrLine);
			}

			writer.WriteLine(accessMod + " " + typeString + "? " + prop.Name + accessor);
			writer.WriteLineRaw("#nullable restore");
			writer.WriteLineRaw("#else");
			if (qpAttrLine != null)
			{
				writer.WriteLine(qpAttrLine);
			}

			writer.WriteLine(accessMod + " " + typeString + " " + prop.Name + accessor);
			writer.WriteLineRaw("#endif");
		}
		else
		{
			if (qpAttrLine != null)
			{
				writer.WriteLine(qpAttrLine);
			}

			EmitAutoProperty(writer, prop);
		}
	}

	// ==================================================================
	// Simple / structural properties (UrlTemplate, PathParameters, etc.)
	// ==================================================================

	/// <summary>
	/// Emits a simple auto-property, optionally with a default value
	/// initializer and/or read-only semantics.
	/// </summary>
	private void EmitSimpleProperty(CodeWriter writer, CodeProperty prop)
	{
		// XML doc summary (single-line for properties).
		CSharpEmitter.WriteXmlDocSummarySingleLine(writer, prop.Description);

		// Deprecation attribute.
		WriteDeprecationAttribute(writer, prop);

		if (CSharpConventionService.RequiresNullableGuard(prop.Type))
		{
			EmitNullableGuardedAutoProperty(writer, prop);
		}
		else
		{
			EmitAutoProperty(writer, prop);
		}
	}

	// ==================================================================
	// Property emission patterns
	// ==================================================================

	/// <summary>
	/// Emits a standard auto-property (no nullable guards, no backing store).
	/// <para>
	/// Pattern examples:
	/// <code>
	/// public int? Count { get; set; }
	/// private string UrlTemplate { get; } = "{+baseurl}/pets";
	/// </code>
	/// </para>
	/// </summary>
	private void EmitAutoProperty(CodeWriter writer, CodeProperty prop)
	{
		var sb = new StringBuilder();

		// Access modifier.
		sb.Append(CSharpConventionService.GetAccessModifier(prop.Access));
		sb.Append(" ");

		// Static modifier.
		if (prop.IsStatic)
		{
			sb.Append("static ");
		}

		// Type.
		sb.Append(CSharpConventionService.GetTypeString(prop.Type));
		sb.Append(" ");

		// Name.
		sb.Append(prop.Name);

		// Accessor.
		if (prop.IsReadOnly)
		{
			sb.Append(" { get; }");
		}
		else
		{
			sb.Append(" { get; set; }");
		}

		// Default value.
		if (!string.IsNullOrEmpty(prop.DefaultValue))
		{
			sb.Append(" = ");
			sb.Append(FormatDefaultValue(prop));
			sb.Append(";");
		}

		writer.WriteLine(sb.ToString());
	}

	/// <summary>
	/// Emits a property with nullable reference-type conditional compilation
	/// guards for cross-targeting compatibility.
	/// <para>
	/// Pattern:
	/// <code>
	/// #if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
	/// #nullable enable
	///         public string? PropertyName { get; set; }
	/// #nullable restore
	/// #else
	///         public string PropertyName { get; set; }
	/// #endif
	/// </code>
	/// </para>
	/// </summary>
	private void EmitNullableGuardedAutoProperty(CodeWriter writer, CodeProperty prop)
	{
		var typeString = CSharpConventionService.GetTypeString(prop.Type, includeNullable: false);
		var accessMod = CSharpConventionService.GetAccessModifier(prop.Access);
		var staticMod = prop.IsStatic ? "static " : string.Empty;
		var accessor = prop.IsReadOnly ? " { get; }" : " { get; set; }";
		var defaultSuffix = string.Empty;

		if (!string.IsNullOrEmpty(prop.DefaultValue))
		{
			defaultSuffix = " = " + FormatDefaultValue(prop) + ";";
		}

		// #if guard — nullable-enabled version.
		writer.WriteLineRaw("#if " + CSharpConventionService.NullableEnableCondition);
		writer.WriteLineRaw("#nullable enable");

		writer.WriteLine(
			accessMod + " " + staticMod + typeString + "? "
			+ prop.Name + accessor + defaultSuffix);

		writer.WriteLineRaw("#nullable restore");
		writer.WriteLineRaw("#else");

		// Non-nullable version.
		writer.WriteLine(
			accessMod + " " + staticMod + typeString + " "
			+ prop.Name + accessor + defaultSuffix);

		writer.WriteLineRaw("#endif");
	}

	/// <summary>
	/// Emits a property that delegates get/set to the backing store.
	/// <para>
	/// Pattern:
	/// <code>
	/// public string PropertyName
	/// {
	///     get { return BackingStore?.Get&lt;string&gt;("propertyName"); }
	///     set { BackingStore?.Set("propertyName", value); }
	/// }
	/// </code>
	/// </para>
	/// </summary>
	private void EmitBackingStoreAccessorProperty(CodeWriter writer, CodeProperty prop)
	{
		var typeString = CSharpConventionService.GetTypeString(prop.Type);
		var accessMod = CSharpConventionService.GetAccessModifier(prop.Access);
		var staticMod = prop.IsStatic ? "static " : string.Empty;
		var backingStoreKey = prop.GetSerializedName();
		var backingStoreTypeArg = CSharpConventionService.GetTypeString(prop.Type, includeNullable: false);

		bool needsNullableGuard = CSharpConventionService.RequiresNullableGuard(prop.Type);

		if (needsNullableGuard)
		{
			// Emit the nullable-guarded version with backing store.
			writer.WriteLineRaw("#if " + CSharpConventionService.NullableEnableCondition);
			writer.WriteLineRaw("#nullable enable");

			writer.WriteLine(accessMod + " " + staticMod + typeString + "? " + prop.Name);
			writer.OpenBlock();

			writer.WriteLine(
				"get { return BackingStore?.Get<"
				+ backingStoreTypeArg
				+ ">(\"" + EscapeStringLiteral(backingStoreKey) + "\"); }");

			writer.WriteLine(
				"set { BackingStore?.Set(\""
				+ EscapeStringLiteral(backingStoreKey)
				+ "\", value); }");

			writer.CloseBlock();

			writer.WriteLineRaw("#nullable restore");
			writer.WriteLineRaw("#else");

			writer.WriteLine(accessMod + " " + staticMod + typeString + " " + prop.Name);
			writer.OpenBlock();

			writer.WriteLine(
				"get { return BackingStore?.Get<"
				+ backingStoreTypeArg
				+ ">(\"" + EscapeStringLiteral(backingStoreKey) + "\"); }");

			writer.WriteLine(
				"set { BackingStore?.Set(\""
				+ EscapeStringLiteral(backingStoreKey)
				+ "\", value); }");

			writer.CloseBlock();

			writer.WriteLineRaw("#endif");
		}
		else
		{
			// No nullable guard needed (value types or non-nullable).
			writer.WriteLine(accessMod + " " + staticMod + typeString + " " + prop.Name);
			writer.OpenBlock();

			writer.WriteLine(
				"get { return BackingStore?.Get<"
				+ backingStoreTypeArg
				+ ">(\"" + EscapeStringLiteral(backingStoreKey) + "\"); }");

			writer.WriteLine(
				"set { BackingStore?.Set(\""
				+ EscapeStringLiteral(backingStoreKey)
				+ "\", value); }");

			writer.CloseBlock();
		}
	}

	// ==================================================================
	// Helpers
	// ==================================================================

	/// <summary>
	/// Writes an <c>[Obsolete]</c> attribute if the property is deprecated.
	/// </summary>
	private static void WriteDeprecationAttribute(CodeWriter writer, CodeProperty prop)
	{
		if (!prop.IsDeprecated)
		{
			return;
		}

		if (!string.IsNullOrEmpty(prop.DeprecationMessage))
		{
			writer.WriteLine(
				"[Obsolete(\""
				+ EscapeStringLiteral(prop.DeprecationMessage)
				+ "\")]");
		}
		else
		{
			writer.WriteLine("[Obsolete]");
		}
	}

	/// <summary>
	/// Formats the default value for a property initializer.
	/// <para>
	/// String default values are wrapped in double-quotes. Other values
	/// (numeric literals, constructor calls like
	/// <c>new Dictionary&lt;string, object&gt;()</c>) are emitted verbatim.
	/// </para>
	/// </summary>
	private static string FormatDefaultValue(CodeProperty prop)
	{
		var value = prop.DefaultValue;

		// If the default value is for a UrlTemplate (string literal),
		// wrap in quotes.
		if (prop.Kind == CodePropertyKind.UrlTemplate)
		{
			return "\"" + EscapeStringLiteral(value) + "\"";
		}

		// All other default values are emitted verbatim (they are already
		// valid C# expressions set by the CodeDOM builder/refiner).
		return value;
	}

	/// <summary>
	/// Escapes a string for use inside a C# string literal (double-quotes).
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
