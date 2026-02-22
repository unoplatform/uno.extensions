using System;
using System.Collections.Generic;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;

/// <summary>
/// Represents an intersection type reference generated from an OpenAPI
/// <c>anyOf</c> composition. Any combination of the <see cref="Types"/>
/// may be valid at runtime.
/// <para>
/// The C# emitter generates a wrapper class implementing
/// <c>IComposedTypeWrapper</c> with one property per constituent type.
/// During deserialization the wrapper attempts to populate as many
/// constituent properties as are present in the payload.
/// </para>
/// <para>
/// Example OpenAPI:
/// <code>
/// schema:
///   anyOf:
///     - $ref: '#/components/schemas/Address'
///     - $ref: '#/components/schemas/PhoneNumber'
/// </code>
/// Results in a <see cref="CodeIntersectionType"/> with two entries in
/// <see cref="Types"/>: one pointing at <c>Address</c> and one at
/// <c>PhoneNumber</c>.
/// </para>
/// </summary>
internal class CodeIntersectionType : CodeTypeBase
{
	/// <summary>
	/// Initializes a new <see cref="CodeIntersectionType"/> with the
	/// specified name.
	/// </summary>
	/// <param name="name">
	/// The composed type name (e.g., <c>"AddressOrPhoneNumber"</c>).
	/// Must not be <see langword="null"/>.
	/// </param>
	public CodeIntersectionType(string name)
		: base(name)
	{
	}

	// ------------------------------------------------------------------
	// Constituent types
	// ------------------------------------------------------------------

	/// <summary>
	/// The constituent types of this intersection. Each <see cref="CodeType"/>
	/// represents one of the <c>anyOf</c> alternatives.
	/// <para>
	/// The emitter generates a property in the wrapper class for each
	/// entry. Unlike <see cref="CodeUnionType"/>, multiple constituents
	/// may be simultaneously populated.
	/// </para>
	/// </summary>
	public IReadOnlyList<CodeType> Types => _types;

	private readonly List<CodeType> _types = new List<CodeType>();

	/// <summary>
	/// Adds a constituent type to this intersection.
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
		var clone = new CodeIntersectionType(Name);
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
		=> $"anyOf({string.Join(" & ", _types)})";
}
