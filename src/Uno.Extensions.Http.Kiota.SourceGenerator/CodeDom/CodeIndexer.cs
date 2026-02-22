#nullable disable

using System;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;

/// <summary>
/// Represents an indexer in a request-builder class, modelling a parameterized
/// path segment such as <c>/pets/{petId}</c>.
/// <para>
/// An indexer is emitted as an accessor method on the owning request-builder
/// class that accepts a scalar key and returns a child
/// <see cref="CodeClassKind.RequestBuilder"/> (the "item" request builder).
/// For example:
/// <code>
/// public PetsItemRequestBuilder this[string petId]
///     => new PetsItemRequestBuilder(PathParameters, RequestAdapter, petId);
/// </code>
/// Or, when emitted as a named method:
/// <code>
/// public PetsItemRequestBuilder ByPetId(string petId) { ... }
/// </code>
/// </para>
/// <para>
/// The <see cref="ReturnType"/> points to the item request-builder class,
/// <see cref="IndexParameterName"/> is the camelCase parameter name (e.g.,
/// <c>"petId"</c>), and <see cref="PathSegment"/> holds the raw URL template
/// segment (e.g., <c>"{+petId}"</c> or <c>"{pet%2Did}"</c>).
/// </para>
/// </summary>
internal class CodeIndexer : CodeElement
{
	/// <summary>
	/// Initializes a new <see cref="CodeIndexer"/> with the specified name.
	/// </summary>
	/// <param name="name">
	/// The indexer name used for method-style emission (e.g., <c>"ByPetId"</c>).
	/// Must not be <see langword="null"/>.
	/// </param>
	public CodeIndexer(string name)
		: base(name)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="CodeIndexer"/> with full details.
	/// </summary>
	/// <param name="name">
	/// The indexer name (e.g., <c>"ByPetId"</c>). Must not be <see langword="null"/>.
	/// </param>
	/// <param name="returnType">
	/// The return type — the item request-builder class.
	/// </param>
	/// <param name="indexParameterName">
	/// The parameter name (e.g., <c>"petId"</c>).
	/// </param>
	/// <param name="pathSegment">
	/// The raw URL template segment (e.g., <c>"{+petId}"</c>).
	/// </param>
	public CodeIndexer(string name, CodeType returnType, string indexParameterName, string pathSegment)
		: base(name)
	{
		ReturnType = returnType;
		IndexParameterName = indexParameterName;
		PathSegment = pathSegment;
	}

	// ------------------------------------------------------------------
	// Core properties
	// ------------------------------------------------------------------

	/// <summary>
	/// The return type of this indexer — always a <see cref="CodeType"/>
	/// pointing to a <see cref="CodeClassKind.RequestBuilder"/> class
	/// (the "item" request builder for the parameterized path segment).
	/// <para>
	/// May be <see langword="null"/> during early CodeDOM construction;
	/// must be resolved before the emission phase.
	/// </para>
	/// </summary>
	public CodeType ReturnType { get; set; }

	/// <summary>
	/// The name of the index parameter in the emitted method signature
	/// (e.g., <c>"petId"</c>, <c>"messageId"</c>).
	/// <para>
	/// This is used as the formal parameter name in the C# indexer
	/// <c>this[string petId]</c> or named method <c>ByPetId(string petId)</c>.
	/// </para>
	/// </summary>
	public string IndexParameterName { get; set; }

	/// <summary>
	/// The type of the index parameter. Typically a <see cref="CodeType"/>
	/// representing <c>string</c>, but may be <c>int</c>, <c>Guid</c>, etc.
	/// depending on the OpenAPI parameter schema.
	/// <para>
	/// When <see langword="null"/>, the emitter defaults to <c>string</c>.
	/// </para>
	/// </summary>
	public CodeTypeBase IndexParameterType { get; set; }

	/// <summary>
	/// The raw URL template segment for this parameterized path component
	/// (e.g., <c>"{+petId}"</c>, <c>"{pet%2Did}"</c>).
	/// <para>
	/// This is appended to the parent request builder's <c>UrlTemplate</c>
	/// when constructing the item request builder.
	/// </para>
	/// </summary>
	public string PathSegment { get; set; }

	/// <summary>
	/// Optional description for the index parameter, sourced from the OpenAPI
	/// parameter <c>description</c> field. When <see langword="null"/>, the
	/// emitter uses a default description like "Unique identifier of the item".
	/// </summary>
	public string ParameterDescription { get; set; }

	// ------------------------------------------------------------------
	// Object overrides
	// ------------------------------------------------------------------

	/// <inheritdoc/>
	public override string ToString()
		=> $"Indexer {Name}[{IndexParameterName}] -> {ReturnType?.Name ?? "?"}";
}
