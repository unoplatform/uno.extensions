using System;
using System.Collections.Generic;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;

/// <summary>
/// Represents a union type reference generated from an OpenAPI <c>oneOf</c>
/// composition. Exactly one of the <see cref="Types"/> is valid at runtime.
/// <para>
/// The C# emitter generates a wrapper class implementing
/// <c>IComposedTypeWrapper</c> with one property per constituent type and
/// a factory method that tries each type in turn during deserialization.
/// </para>
/// <para>
/// Example OpenAPI:
/// <code>
/// schema:
///   oneOf:
///     - $ref: '#/components/schemas/Cat'
///     - $ref: '#/components/schemas/Dog'
/// </code>
/// Results in a <see cref="CodeUnionType"/> with two entries in
/// <see cref="Types"/>: one pointing at <c>Cat</c> and one at <c>Dog</c>.
/// </para>
/// </summary>
internal class CodeUnionType : CodeTypeBase
{
	/// <summary>
	/// Initializes a new <see cref="CodeUnionType"/> with the specified name.
	/// </summary>
	/// <param name="name">
	/// The composed type name (e.g., <c>"CatOrDog"</c>).
	/// Must not be <see langword="null"/>.
	/// </param>
	public CodeUnionType(string name)
		: base(name)
	{
	}

	// ------------------------------------------------------------------
	// Constituent types
	// ------------------------------------------------------------------

	/// <summary>
	/// The constituent types of this union. Each <see cref="CodeType"/>
	/// represents one of the <c>oneOf</c> alternatives.
	/// <para>
	/// The emitter generates a property in the wrapper class for each
	/// entry. If a constituent type is a primitive (e.g., <c>string</c>,
	/// <c>int</c>), the wrapper property wraps it appropriately.
	/// </para>
	/// </summary>
	public IReadOnlyList<CodeType> Types => _types;

	private readonly List<CodeType> _types = new List<CodeType>();

	/// <summary>
	/// Adds a constituent type to this union.
	/// </summary>
	/// <param name="type">
	/// The type to add. Must not be <see langword="null"/>.
	/// </param>
	/// <returns>The added <paramref name="type"/> for fluent chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="type"/> is <see langword="null"/>.
	/// </exception>
	public CodeType AddType(CodeType type)
	{
		if (type is null)
		{
			throw new ArgumentNullException(nameof(type));
		}

		_types.Add(type);
		return type;
	}

	// ------------------------------------------------------------------
	// Cloning
	// ------------------------------------------------------------------

	/// <inheritdoc/>
	public override CodeTypeBase Clone()
	{
		var clone = new CodeUnionType(Name);
		CopyBaseTo(clone);

		for (int i = 0; i < _types.Count; i++)
		{
			clone._types.Add((CodeType)_types[i].Clone());
		}

		return clone;
	}

	// ------------------------------------------------------------------
	// Object overrides
	// ------------------------------------------------------------------

	/// <inheritdoc/>
	public override string ToString()
		=> $"oneOf({string.Join(" | ", _types)})";
}
