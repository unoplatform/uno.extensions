#nullable disable

using System;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;

// ======================================================================
// CodeParameterKind enumeration
// ======================================================================

/// <summary>
/// Classifies the semantic role of a <see cref="CodeParameter"/> within the
/// method that owns it. The kind determines how the parameter is mapped in
/// the emitted C# source (e.g., a path segment token, a request body, or a
/// cancellation token).
/// </summary>
internal enum CodeParameterKind
{
	/// <summary>
	/// A path parameter that is substituted into the URL template
	/// (e.g., <c>{petId}</c>).
	/// </summary>
	Path = 0,

	/// <summary>
	/// A query parameter passed via the query-string configuration lambda
	/// (e.g., <c>q => q.Filter = "..."</c>). Used in executor and
	/// request-generator method signatures.
	/// </summary>
	QueryParameter = 1,

	/// <summary>
	/// The request body parameter (e.g., the <c>Pet</c> object sent in a
	/// <c>PostAsync</c> call).
	/// </summary>
	Body = 2,

	/// <summary>
	/// The <c>RequestConfiguration</c> parameter that bundles query
	/// parameters and request headers.
	/// </summary>
	RequestConfiguration = 3,

	/// <summary>
	/// A raw URL string used by the <c>WithUrl</c> method or the
	/// alternate constructor that accepts a pre-built URL.
	/// </summary>
	RawUrl = 4,

	/// <summary>
	/// The <c>IRequestAdapter</c> parameter injected into request-builder
	/// constructors.
	/// </summary>
	RequestAdapter = 5,

	/// <summary>
	/// A <see cref="System.Threading.CancellationToken"/> parameter
	/// appended to async executor methods.
	/// </summary>
	Cancellation = 6,
}

// ======================================================================
// CodeParameter
// ======================================================================

/// <summary>
/// Represents a method parameter in the CodeDOM tree.
/// <para>
/// Each <see cref="CodeParameter"/> has a <see cref="Kind"/> that classifies
/// its semantic role (path token, request body, cancellation token, etc.),
/// a <see cref="Type"/> describing the parameter's CLR type, and an
/// <see cref="Optional"/> flag that controls whether the parameter has a
/// default value in the emitted C# signature.
/// </para>
/// <para>
/// Parameters are owned by a <see cref="CodeMethod"/> and maintained in its
/// <c>Parameters</c> collection. The order in the collection determines the
/// order in the emitted method signature.
/// </para>
/// </summary>
internal class CodeParameter : CodeElement
{
	/// <summary>
	/// Initializes a new <see cref="CodeParameter"/> with the specified name.
	/// </summary>
	/// <param name="name">
	/// The parameter name (e.g., <c>"body"</c>, <c>"cancellationToken"</c>).
	/// Must not be <see langword="null"/>.
	/// </param>
	public CodeParameter(string name)
		: base(name)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="CodeParameter"/> with the specified name,
	/// kind, and type.
	/// </summary>
	/// <param name="name">
	/// The parameter name. Must not be <see langword="null"/>.
	/// </param>
	/// <param name="kind">
	/// The semantic role of this parameter.
	/// </param>
	/// <param name="type">
	/// The type of this parameter. May be <see langword="null"/> when the
	/// parameter's type has not yet been resolved during CodeDOM construction.
	/// </param>
	public CodeParameter(string name, CodeParameterKind kind, CodeTypeBase type)
		: base(name)
	{
		Kind = kind;
		Type = type;
	}

	// ------------------------------------------------------------------
	// Classification
	// ------------------------------------------------------------------

	/// <summary>
	/// The semantic role of this parameter (path, body, cancellation, etc.).
	/// </summary>
	public CodeParameterKind Kind { get; set; }

	// ------------------------------------------------------------------
	// Type information
	// ------------------------------------------------------------------

	/// <summary>
	/// The type of this parameter (e.g., a <see cref="CodeType"/> pointing
	/// to <c>"Pet"</c>, or a built-in type like <c>"string"</c>).
	/// <para>
	/// May be <see langword="null"/> during early CodeDOM construction; must
	/// be resolved before the emission phase.
	/// </para>
	/// </summary>
	public CodeTypeBase Type { get; set; }

	// ------------------------------------------------------------------
	// Default value
	// ------------------------------------------------------------------

	/// <summary>
	/// Whether this parameter is optional (has a default value in the emitted
	/// C# method signature).
	/// <para>
	/// For example, <see cref="CodeParameterKind.Cancellation"/> parameters
	/// are typically optional with <c>default</c> as their default value.
	/// <see cref="CodeParameterKind.RequestConfiguration"/> parameters are
	/// also optional, defaulting to <see langword="null"/>.
	/// </para>
	/// </summary>
	public bool Optional { get; set; }

	/// <summary>
	/// The default value expression to emit when <see cref="Optional"/> is
	/// <see langword="true"/> (e.g., <c>"default"</c>, <c>"null"</c>).
	/// <para>
	/// When <see langword="null"/> and <see cref="Optional"/> is
	/// <see langword="true"/>, the emitter will choose an appropriate default
	/// based on the parameter's <see cref="Type"/>.
	/// </para>
	/// </summary>
	public string DefaultValue { get; set; }

	// ------------------------------------------------------------------
	// Object overrides
	// ------------------------------------------------------------------

	/// <inheritdoc/>
	public override string ToString()
		=> $"{Kind} {Name}: {Type?.Name ?? "?"}";
}
