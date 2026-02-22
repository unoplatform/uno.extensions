#nullable disable

using System;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;

/// <summary>
/// Represents a concrete type reference in the CodeDOM tree, pointing at
/// either an internal CodeDOM element (a <see cref="CodeClass"/> or
/// <see cref="CodeEnum"/>) or an external type from the Kiota runtime
/// libraries or the BCL.
/// <para>
/// During early CodeDOM construction <see cref="TypeDefinition"/> may be
/// <see langword="null"/> (a forward reference). The
/// <c>KiotaCodeDomBuilder.MapTypeDefinitions</c> pass resolves all
/// forward references before emission.
/// </para>
/// <para>
/// Examples of internal types: <c>Pet</c>, <c>Error</c>,
/// <c>PetsRequestBuilder</c>. Examples of external types:
/// <c>IRequestAdapter</c>, <c>BaseRequestBuilder</c>, <c>IParsable</c>,
/// <c>string</c>, <c>int</c>.
/// </para>
/// </summary>
internal class CodeType : CodeTypeBase
{
	/// <summary>
	/// Initializes a new <see cref="CodeType"/> with the specified name.
	/// </summary>
	/// <param name="name">
	/// The type name (e.g., <c>"Pet"</c>). Must not be <see langword="null"/>.
	/// </param>
	public CodeType(string name)
		: base(name)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="CodeType"/> for an external/built-in type.
	/// </summary>
	/// <param name="name">
	/// The type name (e.g., <c>"string"</c>, <c>"IRequestAdapter"</c>).
	/// Must not be <see langword="null"/>.
	/// </param>
	/// <param name="isExternal">
	/// <see langword="true"/> when this type is defined outside the generated
	/// code (e.g., from the BCL or Kiota runtime).
	/// </param>
	public CodeType(string name, bool isExternal)
		: base(name)
	{
		IsExternal = isExternal;
	}

	// ------------------------------------------------------------------
	// Resolved reference
	// ------------------------------------------------------------------

	/// <summary>
	/// The resolved CodeDOM element that this type refers to
	/// (e.g., a <see cref="CodeClass"/> or <see cref="CodeEnum"/>),
	/// or <see langword="null"/> for external / built-in types.
	/// <para>
	/// Populated by the <c>KiotaCodeDomBuilder.MapTypeDefinitions</c> pass
	/// after all CodeDOM elements have been created.
	/// </para>
	/// </summary>
	public CodeElement TypeDefinition { get; set; }

	// ------------------------------------------------------------------
	// External flag
	// ------------------------------------------------------------------

	/// <summary>
	/// Whether this type is defined externally (e.g., from the Kiota
	/// runtime libraries or the BCL) rather than generated.
	/// <para>
	/// When <see langword="true"/>, the emitter emits the type name with a
	/// <c>global::</c> prefix and does not attempt to resolve a
	/// <see cref="TypeDefinition"/>.
	/// </para>
	/// </summary>
	public bool IsExternal { get; set; }

	// ------------------------------------------------------------------
	// Cloning
	// ------------------------------------------------------------------

	/// <inheritdoc/>
	public override CodeTypeBase Clone()
	{
		var clone = new CodeType(Name);
		CopyBaseTo(clone);
		clone.TypeDefinition = TypeDefinition;
		clone.IsExternal = IsExternal;
		return clone;
	}

	// ------------------------------------------------------------------
	// Object overrides
	// ------------------------------------------------------------------

	/// <inheritdoc/>
	public override string ToString()
	{
		var external = IsExternal ? " (external)" : string.Empty;
		return $"{base.ToString()}{external}";
	}
}
