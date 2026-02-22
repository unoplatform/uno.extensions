using System;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;

// ======================================================================
// CollectionKind enumeration
// ======================================================================

/// <summary>
/// Specifies how a type is collected (not collected, array, or generic list).
/// <para>
/// Used by <see cref="CodeTypeBase.CollectionKind"/> to distinguish between
/// <c>T[]</c> (array) and <c>List&lt;T&gt;</c> (complex) collection types.
/// </para>
/// </summary>
internal enum CollectionKind
{
	/// <summary>The type is not a collection.</summary>
	None = 0,

	/// <summary>The type is an array (<c>T[]</c>).</summary>
	Array = 1,

	/// <summary>The type is a generic list (<c>List&lt;T&gt;</c>).</summary>
	Complex = 2,
}

// ======================================================================
// CodeTypeBase
// ======================================================================

/// <summary>
/// Abstract base class for type references in the CodeDOM.
/// <para>
/// Every property (<see cref="CodeProperty.Type"/>), method return type
/// (<see cref="CodeMethod.ReturnType"/>), and parameter type
/// (<see cref="CodeParameter.Type"/>) in the CodeDOM tree is represented
/// by a concrete subtype of <see cref="CodeTypeBase"/>:
/// </para>
/// <list type="bullet">
///   <item><see cref="CodeType"/> — a simple (possibly external) type
///   reference with an optional resolved <see cref="CodeType.TypeDefinition"/>
///   back-pointer into the CodeDOM tree.</item>
///   <item><see cref="CodeUnionType"/> — a discriminated union of types
///   generated from OpenAPI <c>oneOf</c> composition.</item>
///   <item><see cref="CodeIntersectionType"/> — a discriminated intersection
///   generated from OpenAPI <c>anyOf</c> composition.</item>
/// </list>
/// <para>
/// <see cref="CodeTypeBase"/> is not a <see cref="CodeElement"/>. It does
/// not participate in the parent/child tree — it is a lightweight value
/// object attached to the element that declares it. Use <see cref="Clone"/>
/// to create a deep copy during refinement when a shared type reference
/// needs per-site mutation.
/// </para>
/// </summary>
internal abstract class CodeTypeBase
{
	/// <summary>
	/// Initializes a new <see cref="CodeTypeBase"/> with the specified name.
	/// </summary>
	/// <param name="name">
	/// The type name (e.g., <c>"string"</c>, <c>"Pet"</c>).
	/// Must not be <see langword="null"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="name"/> is <see langword="null"/>.
	/// </exception>
	protected CodeTypeBase(string name)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
	}

	// ------------------------------------------------------------------
	// Core properties
	// ------------------------------------------------------------------

	/// <summary>
	/// The simple type name (e.g., <c>"string"</c>, <c>"Pet"</c>,
	/// <c>"IRequestAdapter"</c>). May be mutated during refinement
	/// (e.g., to add <c>global::</c> prefix or adjust casing).
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Whether this type reference is nullable.
	/// <para>
	/// For value types this causes <c>?</c> to be emitted (e.g.,
	/// <c>int?</c>). For reference types in nullable-enabled contexts
	/// this controls the conditional compilation guards
	/// (<c>#if NETSTANDARD2_1_OR_GREATER</c>).
	/// </para>
	/// </summary>
	public bool IsNullable { get; set; }

	/// <summary>
	/// Whether the type is a collection. Shorthand check — when
	/// <see langword="true"/>, <see cref="CollectionKind"/> should be
	/// either <see cref="CodeDom.CollectionKind.Array"/> or
	/// <see cref="CodeDom.CollectionKind.Complex"/>.
	/// </summary>
	public bool IsCollection { get; set; }

	/// <summary>
	/// The collection kind when <see cref="IsCollection"/> is
	/// <see langword="true"/>. Defaults to
	/// <see cref="CodeDom.CollectionKind.None"/>.
	/// </summary>
	public CollectionKind CollectionKind { get; set; }

	// ------------------------------------------------------------------
	// Cloning
	// ------------------------------------------------------------------

	/// <summary>
	/// Creates a deep copy of this type reference.
	/// <para>
	/// Used during refinement when a shared type reference needs per-site
	/// mutation (e.g., setting nullability on one use but not another).
	/// </para>
	/// </summary>
	/// <returns>A new <see cref="CodeTypeBase"/> instance with the same values.</returns>
	public abstract CodeTypeBase Clone();

	/// <summary>
	/// Copies the common <see cref="CodeTypeBase"/> properties from this
	/// instance into <paramref name="target"/>.
	/// <para>
	/// Called by concrete <see cref="Clone"/> implementations to share the
	/// base-property copy logic.
	/// </para>
	/// </summary>
	/// <param name="target">The clone target to populate.</param>
	protected void CopyBaseTo(CodeTypeBase target)
	{
		if (target is null)
		{
			throw new ArgumentNullException(nameof(target));
		}

		target.Name = Name;
		target.IsNullable = IsNullable;
		target.IsCollection = IsCollection;
		target.CollectionKind = CollectionKind;
	}

	// ------------------------------------------------------------------
	// Object overrides
	// ------------------------------------------------------------------

	/// <inheritdoc/>
	public override string ToString()
	{
		var suffix = IsCollection
			? CollectionKind == CollectionKind.Array ? "[]" : "<>"
			: string.Empty;

		var nullable = IsNullable ? "?" : string.Empty;

		return $"{Name}{suffix}{nullable}";
	}
}
