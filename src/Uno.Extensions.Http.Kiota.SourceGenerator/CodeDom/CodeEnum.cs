#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;

// ======================================================================
// CodeEnumOption
// ======================================================================

/// <summary>
/// Represents a single member (option) within a <see cref="CodeEnum"/>.
/// <para>
/// Each option maps an OpenAPI <c>enum</c> string value to a C#-safe
/// identifier. The <see cref="SerializedName"/> preserves the original
/// value for the <c>[EnumMember(Value = "...")]</c> attribute emitted by
/// the C# emitter.
/// </para>
/// </summary>
internal sealed class CodeEnumOption
{
	/// <summary>
	/// Initializes a new <see cref="CodeEnumOption"/> with the specified
	/// C# name and serialized value.
	/// </summary>
	/// <param name="name">
	/// The C# identifier name for this enum member (e.g., <c>"Value1"</c>,
	/// <c>"Some_dashed_value"</c>). Must not be <see langword="null"/>.
	/// </param>
	/// <param name="serializedName">
	/// The original string value from the OpenAPI spec (e.g.,
	/// <c>"value1"</c>, <c>"some-dashed-value"</c>). Must not be
	/// <see langword="null"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="name"/> or <paramref name="serializedName"/> is
	/// <see langword="null"/>.
	/// </exception>
	public CodeEnumOption(string name, string serializedName)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		SerializedName = serializedName ?? throw new ArgumentNullException(nameof(serializedName));
	}

	/// <summary>
	/// The C# identifier name for this enum member.
	/// <para>
	/// May be mutated during refinement (e.g., PascalCase conversion,
	/// reserved-word escaping, dash-to-underscore mapping).
	/// </para>
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The original serialized string value from the OpenAPI spec.
	/// <para>
	/// Used as the value in <c>[EnumMember(Value = "...")]</c> attributes
	/// to ensure proper round-trip serialization regardless of the C# name.
	/// </para>
	/// </summary>
	public string SerializedName { get; set; }

	/// <summary>
	/// Optional description for the enum member, sourced from the OpenAPI
	/// <c>description</c> or <c>x-enum-descriptions</c> extension.
	/// Used for XML documentation comment emission.
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// The explicit numeric value for this enum member when the parent enum
	/// is a <c>[Flags]</c> enum. For flags enums, values follow powers of 2
	/// (1, 2, 4, 8, …). When <see langword="null"/>, the member uses the
	/// default compiler-assigned ordinal.
	/// </summary>
	public int? Value { get; set; }

	/// <inheritdoc />
	public override string ToString() => Name;
}

// ======================================================================
// CodeEnum
// ======================================================================

/// <summary>
/// Represents an enum declaration in the CodeDOM tree.
/// <para>
/// A <see cref="CodeEnum"/> is generated from OpenAPI schemas that use
/// <c>type: string</c> with an <c>enum:</c> list. When the enum is used
/// in an array context or has the <c>x-ms-enum</c> flags extension,
/// <see cref="IsFlags"/> is set to <see langword="true"/> and each option
/// receives an explicit power-of-2 value.
/// </para>
/// <para>
/// C# emission produces:
/// <list type="bullet">
///   <item><c>[Flags]</c> attribute when <see cref="IsFlags"/> is set</item>
///   <item><c>[EnumMember(Value = "...")]</c> on each member</item>
///   <item><c>[GeneratedCode("Kiota", ...)]</c> on the enum type</item>
/// </list>
/// </para>
/// </summary>
/// <example>
/// <code>
/// var status = new CodeEnum("PetStatus");
/// status.AddOption(new CodeEnumOption("Available", "available"));
/// status.AddOption(new CodeEnumOption("Pending", "pending"));
/// status.AddOption(new CodeEnumOption("Sold", "sold"));
/// </code>
/// </example>
internal class CodeEnum : CodeElement
{
	/// <summary>
	/// Initializes a new <see cref="CodeEnum"/> with the specified name.
	/// </summary>
	/// <param name="name">
	/// The enum name (e.g., <c>"PetStatus"</c>). Must not be <see langword="null"/>.
	/// </param>
	public CodeEnum(string name)
		: base(name)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="CodeEnum"/> with the specified name and
	/// flags indicator.
	/// </summary>
	/// <param name="name">
	/// The enum name. Must not be <see langword="null"/>.
	/// </param>
	/// <param name="isFlags">
	/// <see langword="true"/> if this is a <c>[Flags]</c> enum with
	/// power-of-2 values; otherwise <see langword="false"/>.
	/// </param>
	public CodeEnum(string name, bool isFlags)
		: base(name)
	{
		IsFlags = isFlags;
	}

	// ------------------------------------------------------------------
	// Classification
	// ------------------------------------------------------------------

	/// <summary>
	/// Indicates whether this enum should be emitted as a <c>[Flags]</c>
	/// enum with explicit power-of-2 values.
	/// <para>
	/// Set to <see langword="true"/> when the OpenAPI schema uses the enum
	/// in an array context or when the <c>x-ms-enum</c> extension specifies
	/// <c>isFlags: true</c>.
	/// </para>
	/// </summary>
	public bool IsFlags { get; set; }

	/// <summary>
	/// Access modifier for the generated enum declaration.
	/// Defaults to <see cref="AccessModifier.Public"/>.
	/// </summary>
	public AccessModifier Access { get; set; } = AccessModifier.Public;

	// ------------------------------------------------------------------
	// Options (members)
	// ------------------------------------------------------------------

	/// <summary>
	/// The enum members (options) in declaration order.
	/// </summary>
	public IReadOnlyList<CodeEnumOption> Options => _options;

	private readonly List<CodeEnumOption> _options = new List<CodeEnumOption>();

	/// <summary>
	/// Adds a <see cref="CodeEnumOption"/> to this enum.
	/// </summary>
	/// <param name="option">
	/// The option to add. Must not be <see langword="null"/>.
	/// </param>
	/// <returns>The added <paramref name="option"/> for fluent chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="option"/> is <see langword="null"/>.
	/// </exception>
	public CodeEnumOption AddOption(CodeEnumOption option)
	{
		if (option is null)
		{
			throw new ArgumentNullException(nameof(option));
		}

		_options.Add(option);
		return option;
	}

	/// <summary>
	/// Adds a new <see cref="CodeEnumOption"/> with the specified name and
	/// serialized name.
	/// </summary>
	/// <param name="name">
	/// The C# identifier name for the enum member. Must not be <see langword="null"/>.
	/// </param>
	/// <param name="serializedName">
	/// The original string value from the OpenAPI spec. Must not be <see langword="null"/>.
	/// </param>
	/// <returns>The newly created and added <see cref="CodeEnumOption"/>.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="name"/> or <paramref name="serializedName"/> is
	/// <see langword="null"/>.
	/// </exception>
	public CodeEnumOption AddOption(string name, string serializedName)
	{
		return AddOption(new CodeEnumOption(name, serializedName));
	}

	/// <summary>
	/// Finds an option in this enum by <paramref name="name"/>
	/// (case-sensitive comparison).
	/// </summary>
	/// <param name="name">The option name to search for.</param>
	/// <returns>
	/// The matching <see cref="CodeEnumOption"/>, or <see langword="null"/>
	/// if not found.
	/// </returns>
	public CodeEnumOption FindOption(string name)
	{
		for (int i = 0; i < _options.Count; i++)
		{
			if (string.Equals(_options[i].Name, name, StringComparison.Ordinal))
			{
				return _options[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Finds an option in this enum by <paramref name="serializedName"/>
	/// (case-sensitive comparison against <see cref="CodeEnumOption.SerializedName"/>).
	/// </summary>
	/// <param name="serializedName">The serialized value to search for.</param>
	/// <returns>
	/// The matching <see cref="CodeEnumOption"/>, or <see langword="null"/>
	/// if not found.
	/// </returns>
	public CodeEnumOption FindOptionBySerializedName(string serializedName)
	{
		for (int i = 0; i < _options.Count; i++)
		{
			if (string.Equals(_options[i].SerializedName, serializedName, StringComparison.Ordinal))
			{
				return _options[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Assigns power-of-2 values (1, 2, 4, 8, …) to all options that do
	/// not yet have an explicit <see cref="CodeEnumOption.Value"/>.
	/// <para>
	/// Call this after all options have been added and <see cref="IsFlags"/>
	/// is set to <see langword="true"/>. This is typically invoked during
	/// the refinement phase.
	/// </para>
	/// </summary>
	public void AssignFlagValues()
	{
		int nextPower = 1;
		for (int i = 0; i < _options.Count; i++)
		{
			if (_options[i].Value is null)
			{
				_options[i].Value = nextPower;
			}

			// Always advance the power regardless of whether we assigned,
			// so that manually-set values don't shift subsequent options.
			nextPower <<= 1;
		}
	}
}
