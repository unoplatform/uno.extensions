#nullable disable

using System;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;

// ======================================================================
// CodePropertyKind enumeration
// ======================================================================

/// <summary>
/// Classifies the semantic role of a <see cref="CodeProperty"/> in the Kiota
/// code-generation model. The kind determines the shape of the emitted C#
/// property (e.g., auto-property vs. backing-store delegate, default value
/// expression, required attributes, etc.).
/// </summary>
internal enum CodePropertyKind
{
	/// <summary>
	/// A user-defined property mapped from an OpenAPI schema field.
	/// <para>
	/// Emitted as a standard auto-property (or a backing-store property when
	/// <c>UsesBackingStore</c> is enabled). The <see cref="CodeProperty.SerializedName"/>
	/// holds the original JSON field name for serialization dispatch.
	/// </para>
	/// </summary>
	Custom = 0,

	/// <summary>
	/// The <c>UrlTemplate</c> string constant on a request builder.
	/// <para>
	/// Emitted as a read-only <c>string</c> property with an inline
	/// initializer containing the URL template literal. Example:
	/// <code>private string UrlTemplate { get; } = "{+baseurl}/pets{?limit,offset}";</code>
	/// </para>
	/// </summary>
	UrlTemplate = 1,

	/// <summary>
	/// The <c>PathParameters</c> dictionary on a request builder.
	/// <para>
	/// Emitted as a <c>Dictionary&lt;string, object&gt;</c> property
	/// initialized by the constructor from the parent request builder's
	/// path parameters.
	/// </para>
	/// </summary>
	PathParameters = 2,

	/// <summary>
	/// The <c>RequestAdapter</c> reference on a request builder.
	/// <para>
	/// Emitted as a property holding the <c>IRequestAdapter</c> reference
	/// injected via the constructor.
	/// </para>
	/// </summary>
	RequestAdapter = 3,

	/// <summary>
	/// Navigation property pointing to a child request builder.
	/// <para>
	/// Emitted as a read-only property whose getter constructs the child
	/// request builder, forwarding <c>PathParameters</c> and
	/// <c>RequestAdapter</c>. Example:
	/// <code>public PetsRequestBuilder Pets => new PetsRequestBuilder(PathParameters, RequestAdapter);</code>
	/// </para>
	/// </summary>
	Navigation = 4,

	/// <summary>
	/// The <c>BackingStore</c> property when backing-store mode is enabled.
	/// <para>
	/// Emitted as:
	/// <code>public IBackingStore BackingStore { get; set; }</code>
	/// All <see cref="Custom"/> properties delegate their get/set to this store.
	/// </para>
	/// </summary>
	BackingStore = 5,

	/// <summary>
	/// The <c>AdditionalData</c> dictionary for round-trip fidelity.
	/// <para>
	/// Emitted as a <c>IDictionary&lt;string, object&gt;</c> property. When
	/// backing-store mode is enabled, delegates to the backing store;
	/// otherwise uses a plain auto-property with an inline initializer.
	/// </para>
	/// </summary>
	AdditionalData = 6,

	/// <summary>
	/// A query parameter property inside a <c>QueryParameters</c> class.
	/// <para>
	/// May be decorated with a <c>[QueryParameter("original-name")]</c>
	/// attribute when the serialized query-string name differs from the
	/// PascalCase C# property name.
	/// </para>
	/// </summary>
	QueryParameter = 7,

	/// <summary>
	/// The <c>Message</c> override property on error models extending
	/// <c>ApiException</c>. Delegates to <c>MessageEscaped</c>.
	/// <para>
	/// Emitted as:
	/// <code>public override string Message { get =&gt; MessageEscaped ?? string.Empty; }</code>
	/// </para>
	/// </summary>
	ErrorMessageOverride = 8,
}

// ======================================================================
// CodeProperty
// ======================================================================

/// <summary>
/// Represents a property declaration in the CodeDOM tree.
/// <para>
/// A <see cref="CodeProperty"/> models any property on a generated class —
/// user-defined schema fields, URL template constants, path parameter
/// dictionaries, request adapter references, navigation getters, backing
/// store handles, additional-data holders, and query parameter POCOs —
/// distinguished by <see cref="Kind"/>. It carries a <see cref="Type"/>
/// reference and an optional <see cref="SerializedName"/> for wire-format
/// mapping.
/// </para>
/// <para>
/// The property is mutable during CodeDOM construction and refinement; treat
/// it as read-only during the emission phase. Properties are owned by a
/// <see cref="CodeClass"/> and maintained in its <c>Properties</c>
/// collection.
/// </para>
/// </summary>
internal class CodeProperty : CodeElement
{
	/// <summary>
	/// Initializes a new <see cref="CodeProperty"/> with the specified name.
	/// </summary>
	/// <param name="name">
	/// The property name (e.g., <c>"Name"</c>). Must not be <see langword="null"/>.
	/// </param>
	public CodeProperty(string name)
		: base(name)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="CodeProperty"/> with the specified name
	/// and kind.
	/// </summary>
	/// <param name="name">
	/// The property name. Must not be <see langword="null"/>.
	/// </param>
	/// <param name="kind">
	/// The semantic role of this property.
	/// </param>
	public CodeProperty(string name, CodePropertyKind kind)
		: base(name)
	{
		Kind = kind;
	}

	/// <summary>
	/// Initializes a new <see cref="CodeProperty"/> with the specified name,
	/// kind, and type.
	/// </summary>
	/// <param name="name">
	/// The property name. Must not be <see langword="null"/>.
	/// </param>
	/// <param name="kind">
	/// The semantic role of this property.
	/// </param>
	/// <param name="type">
	/// The type of this property. May be <see langword="null"/> when the
	/// property's type has not yet been resolved during CodeDOM construction.
	/// </param>
	public CodeProperty(string name, CodePropertyKind kind, CodeTypeBase type)
		: base(name)
	{
		Kind = kind;
		Type = type;
	}

	// ------------------------------------------------------------------
	// Classification
	// ------------------------------------------------------------------

	/// <summary>
	/// The semantic role of this property (custom schema field, URL template,
	/// path parameters, request adapter, navigation, backing store,
	/// additional data, or query parameter).
	/// </summary>
	public CodePropertyKind Kind { get; set; }

	/// <summary>
	/// Access modifier for the generated property declaration.
	/// Defaults to <see cref="AccessModifier.Public"/>.
	/// </summary>
	public AccessModifier Access { get; set; } = AccessModifier.Public;

	// ------------------------------------------------------------------
	// Type information
	// ------------------------------------------------------------------

	/// <summary>
	/// The type of this property (e.g., a <see cref="CodeType"/> pointing
	/// to <c>"Pet"</c>, or a built-in type like <c>"string"</c>).
	/// <para>
	/// May be <see langword="null"/> during early CodeDOM construction; must
	/// be resolved before the emission phase.
	/// </para>
	/// </summary>
	public CodeTypeBase Type { get; set; }

	// ------------------------------------------------------------------
	// Serialization metadata
	// ------------------------------------------------------------------

	/// <summary>
	/// The wire-format (JSON) name of this property, used by the
	/// <c>Serialize</c> and <c>GetFieldDeserializers</c> methods.
	/// <para>
	/// For <see cref="CodePropertyKind.Custom"/> properties this is the
	/// original OpenAPI schema field name (e.g., <c>"first_name"</c>) which
	/// may differ from the PascalCase C# <see cref="CodeElement.Name"/>
	/// (e.g., <c>"FirstName"</c>). For
	/// <see cref="CodePropertyKind.QueryParameter"/> properties this is the
	/// original query-string parameter name when it contains characters
	/// not valid in C# identifiers (e.g., <c>"$filter"</c>).
	/// </para>
	/// <para>
	/// When <see langword="null"/>, the emitter uses
	/// <see cref="CodeElement.Name"/> as the serialized name (indicating the
	/// names are identical).
	/// </para>
	/// </summary>
	public string SerializedName { get; set; }

	// ------------------------------------------------------------------
	// Modifiers
	// ------------------------------------------------------------------

	/// <summary>
	/// Whether this property is read-only (emitted with only a getter).
	/// <para>
	/// Typically <see langword="true"/> for
	/// <see cref="CodePropertyKind.UrlTemplate"/> (compile-time constant),
	/// <see cref="CodePropertyKind.Navigation"/> (computed getter), and
	/// structural request-builder properties. Model
	/// <see cref="CodePropertyKind.Custom"/> properties are normally
	/// read-write.
	/// </para>
	/// </summary>
	public bool IsReadOnly { get; set; }

	/// <summary>
	/// Whether this property is <c>static</c>.
	/// <para>
	/// Currently unused in standard Kiota patterns but included for
	/// completeness and forward compatibility with potential constant
	/// properties.
	/// </para>
	/// </summary>
	public bool IsStatic { get; set; }

	// ------------------------------------------------------------------
	// Default value
	// ------------------------------------------------------------------

	/// <summary>
	/// The default value expression to emit in the property initializer.
	/// <para>
	/// For example:
	/// <list type="bullet">
	///   <item><see cref="CodePropertyKind.UrlTemplate"/>:
	///   <c>"{+baseurl}/pets{?limit,offset}"</c></item>
	///   <item><see cref="CodePropertyKind.AdditionalData"/>:
	///   <c>"new Dictionary&lt;string, object&gt;()"</c></item>
	///   <item><see cref="CodePropertyKind.PathParameters"/>:
	///   <c>"new Dictionary&lt;string, object&gt;()"</c></item>
	/// </list>
	/// </para>
	/// <para>
	/// When <see langword="null"/>, the emitter does not emit an initializer
	/// (the property starts with the type's default value).
	/// </para>
	/// </summary>
	public string DefaultValue { get; set; }

	// ------------------------------------------------------------------
	// Deprecation
	// ------------------------------------------------------------------

	/// <summary>
	/// Whether this property is deprecated. When <see langword="true"/>,
	/// the emitter adds an <c>[Obsolete]</c> attribute to the generated
	/// property declaration.
	/// </summary>
	public bool IsDeprecated { get; set; }

	/// <summary>
	/// Whether this property overrides a base-class property. When
	/// <see langword="true"/>, the emitter adds the <c>override</c>
	/// modifier to the generated property declaration.
	/// </summary>
	public bool IsOverride { get; set; }

	/// <summary>
	/// The deprecation message included in the <c>[Obsolete]</c> attribute
	/// when <see cref="IsDeprecated"/> is <see langword="true"/>.
	/// <para>
	/// When <see langword="null"/> and <see cref="IsDeprecated"/> is
	/// <see langword="true"/>, the emitter uses a generic deprecation message.
	/// </para>
	/// </summary>
	public string DeprecationMessage { get; set; }

	// ------------------------------------------------------------------
	// Cross references
	// ------------------------------------------------------------------

	/// <summary>
	/// The getter method for this property, when properties are emitted as
	/// explicit getter/setter method pairs instead of auto-properties.
	/// <see langword="null"/> when using auto-property syntax.
	/// </summary>
	public CodeMethod Getter { get; set; }

	/// <summary>
	/// The setter method for this property, when properties are emitted as
	/// explicit getter/setter method pairs instead of auto-properties.
	/// <see langword="null"/> when using auto-property syntax or when the
	/// property is read-only.
	/// </summary>
	public CodeMethod Setter { get; set; }

	// ------------------------------------------------------------------
	// Convenience helpers
	// ------------------------------------------------------------------

	/// <summary>
	/// Returns the effective serialized name for this property: the explicit
	/// <see cref="SerializedName"/> if set, otherwise <see cref="CodeElement.Name"/>.
	/// </summary>
	/// <returns>
	/// The name to use on the wire (JSON key, query-string parameter name).
	/// Never <see langword="null"/>.
	/// </returns>
	public string GetSerializedName()
		=> SerializedName ?? Name;

	// ------------------------------------------------------------------
	// Object overrides
	// ------------------------------------------------------------------

	/// <inheritdoc/>
	public override string ToString()
		=> $"{Kind} {Name}: {Type?.Name ?? "?"}";
}
