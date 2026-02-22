using System;
using System.Text;
using Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;
using Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;
using Uno.Extensions.Http.Kiota.SourceGenerator.Refinement;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Emitter;

/// <summary>
/// Emits C# enum declarations from <see cref="CodeEnum"/> nodes in the CodeDOM
/// tree, producing output matching Kiota CLI patterns:
/// <list type="bullet">
///   <item><c>[global::System.CodeDom.Compiler.GeneratedCode("Kiota", "...")]</c>
///   attribute on the enum type.</item>
///   <item><c>[Flags]</c> attribute when <see cref="CodeEnum.IsFlags"/> is
///   <see langword="true"/>, with explicit power-of-2 values on each member.</item>
///   <item><c>[EnumMember(Value = "...")]</c> attribute on each member
///   preserving the original OpenAPI string value for round-trip serialization.</item>
///   <item>XML documentation comments on the enum type and individual members
///   when descriptions are available from the OpenAPI spec.</item>
/// </list>
/// <para>
/// Expected output for a simple enum:
/// <code>
/// [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
/// public enum PetStatus
/// {
///     [EnumMember(Value = "available")]
///     Available,
///     [EnumMember(Value = "pending")]
///     Pending,
///     [EnumMember(Value = "sold")]
///     Sold,
/// }
/// </code>
/// </para>
/// <para>
/// Expected output for a flags enum:
/// <code>
/// [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
/// [Flags]
/// public enum Permissions
/// {
///     [EnumMember(Value = "read")]
///     Read = 1,
///     [EnumMember(Value = "write")]
///     Write = 2,
///     [EnumMember(Value = "execute")]
///     Execute = 4,
/// }
/// </code>
/// </para>
/// <para>
/// Called from <see cref="CSharpEmitter"/> when walking the namespace tree
/// and encountering <see cref="CodeEnum"/> nodes.
/// </para>
/// </summary>
internal sealed class EnumEmitter
{
	private readonly KiotaGeneratorConfig _config;

	/// <summary>
	/// Initializes a new <see cref="EnumEmitter"/> with the given generator
	/// configuration.
	/// </summary>
	/// <param name="config">
	/// The generator configuration. Currently unused for enum emission but
	/// accepted for consistency with other sub-emitter constructors and
	/// future extensibility.
	/// </param>
	public EnumEmitter(KiotaGeneratorConfig config)
	{
		_config = config;
	}

	/// <summary>
	/// Emits a complete enum type declaration into the given
	/// <paramref name="writer"/>. The caller is responsible for writing the
	/// file header, <c>using</c> directives, and <c>namespace</c> block;
	/// this method emits the enum type itself including:
	/// <list type="number">
	///   <item>XML documentation summary (if available)</item>
	///   <item><c>[GeneratedCode]</c> attribute</item>
	///   <item><c>[Flags]</c> attribute (for flags enums)</item>
	///   <item>Enum declaration with access modifier</item>
	///   <item>All enum members with <c>[EnumMember]</c> attributes</item>
	/// </list>
	/// </summary>
	/// <param name="writer">The code writer to emit into.</param>
	/// <param name="en">The enum to emit. Must not be <see langword="null"/>.</param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="writer"/> or <paramref name="en"/> is <see langword="null"/>.
	/// </exception>
	public void Emit(CodeWriter writer, CodeEnum en)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		if (en is null)
		{
			throw new ArgumentNullException(nameof(en));
		}

		// ── XML doc summary (single-line for enums) ──
		CSharpEmitter.WriteXmlDocSummarySingleLine(writer, en.Description);

		// ── [GeneratedCode] attribute ──
		CSharpEmitter.WriteGeneratedCodeAttribute(writer);

		// ── [Flags] attribute for flags enums ──
		if (en.IsFlags)
		{
			writer.WriteLine("[Flags]");
		}

		// ── CS1591 pragma around enum declaration when no doc summary ──
		bool needsDeclPragma = string.IsNullOrEmpty(en.Description);
		if (needsDeclPragma)
		{
			writer.WriteLine("#pragma warning disable CS1591");
		}

		// ── Enum declaration ──
		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(en.Access)
			+ " enum "
			+ en.Name);

		if (needsDeclPragma)
		{
			writer.WriteLine("#pragma warning restore CS1591");
		}

		writer.OpenBlock();

		// ── Enum members ──
		for (int i = 0; i < en.Options.Count; i++)
		{
			EmitOption(writer, en.Options[i]);
		}

		writer.CloseBlock();
	}

	// ==================================================================
	// Option (member) emission
	// ==================================================================

	/// <summary>
	/// Emits a single enum member with its <c>[EnumMember]</c> attribute
	/// and optional XML documentation comment.
	/// </summary>
	/// <param name="writer">The code writer.</param>
	/// <param name="option">The enum option to emit.</param>
	private static void EmitOption(CodeWriter writer, CodeEnumOption option)
	{
		// XML doc summary for the individual member (if available).
		if (!string.IsNullOrEmpty(option.Description))
		{
			CSharpEmitter.WriteXmlDocSummary(writer, option.Description);
		}

		// [EnumMember(Value = "...")] attribute.
		writer.WriteLine(
			"[EnumMember(Value = \""
			+ EscapeStringLiteral(option.SerializedName)
			+ "\")]");

		// Suppress CS1591 around the member name (Kiota CLI pattern).
		writer.WriteLine("#pragma warning disable CS1591");

		// Member declaration with optional explicit value (for flags enums).
		var sb = new StringBuilder();
		sb.Append(option.Name);

		if (option.Value.HasValue)
		{
			sb.Append(" = ");
			sb.Append(option.Value.Value);
		}

		sb.Append(",");
		writer.WriteLine(sb.ToString());

		writer.WriteLine("#pragma warning restore CS1591");
	}

	// ==================================================================
	// String helpers
	// ==================================================================

	/// <summary>
	/// Escapes a string for use inside a C# string literal (double-quotes).
	/// Handles backslash and double-quote characters.
	/// </summary>
	/// <param name="value">The string to escape.</param>
	/// <returns>The escaped string, or the original if no escaping is needed.</returns>
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
