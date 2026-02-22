using System;
using System.Text;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Emitter;

/// <summary>
/// A lightweight, indentation-aware string builder for generating C# source
/// code within a <c>netstandard2.0</c> Roslyn source generator.
/// <para>
/// Provides block-scoped indentation management via
/// <see cref="OpenBlock"/>/<see cref="CloseBlock"/>, and raw/indented write
/// variants. Used by <see cref="CSharpEmitter"/> and all sub-emitters.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var w = new CodeWriter();
/// w.WriteLine("namespace Foo");
/// w.OpenBlock();
/// w.WriteLine("public class Bar");
/// w.OpenBlock();
/// w.CloseBlock();
/// w.CloseBlock();
/// // Produces correctly indented output.
/// </code>
/// </example>
internal sealed class CodeWriter
{
	private readonly StringBuilder _sb;
	private int _indentLevel;

	/// <summary>
	/// Initializes a new <see cref="CodeWriter"/> with a default buffer
	/// capacity of 4 KB.
	/// </summary>
	public CodeWriter()
	{
		_sb = new StringBuilder(4096);
	}

	/// <summary>
	/// Initializes a new <see cref="CodeWriter"/> with the specified initial
	/// buffer capacity.
	/// </summary>
	/// <param name="capacity">Initial capacity of the internal buffer.</param>
	public CodeWriter(int capacity)
	{
		_sb = new StringBuilder(capacity);
	}

	// ------------------------------------------------------------------
	// Indentation management
	// ------------------------------------------------------------------

	/// <summary>Gets the current indentation depth.</summary>
	public int IndentLevel => _indentLevel;

	/// <summary>
	/// Increases the indentation level by one (four spaces per level).
	/// </summary>
	public void IncreaseIndent() => _indentLevel++;

	/// <summary>
	/// Decreases the indentation level by one. Does nothing when already at
	/// level 0.
	/// </summary>
	public void DecreaseIndent()
	{
		if (_indentLevel > 0)
		{
			_indentLevel--;
		}
	}

	// ------------------------------------------------------------------
	// Block helpers
	// ------------------------------------------------------------------

	/// <summary>
	/// Writes an opening brace <c>{</c> on its own line and increases
	/// indentation.
	/// </summary>
	public void OpenBlock()
	{
		WriteLine("{");
		IncreaseIndent();
	}

	/// <summary>
	/// Decreases indentation and writes a closing brace <c>}</c> on its own
	/// line.
	/// </summary>
	public void CloseBlock()
	{
		DecreaseIndent();
		WriteLine("}");
	}

	// ------------------------------------------------------------------
	// Indented write operations
	// ------------------------------------------------------------------

	/// <summary>
	/// Writes a blank line (no indentation).
	/// </summary>
	public void WriteLine()
	{
		_sb.AppendLine();
	}

	/// <summary>
	/// Writes <paramref name="text"/> preceded by the current indentation
	/// and followed by a newline. A <see langword="null"/> or empty
	/// <paramref name="text"/> produces a blank line without indentation.
	/// </summary>
	/// <param name="text">The text to write.</param>
	public void WriteLine(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			_sb.AppendLine();
			return;
		}

		WriteIndent();
		_sb.AppendLine(text);
	}

	/// <summary>
	/// Writes <paramref name="text"/> preceded by the current indentation
	/// but with <strong>no</strong> trailing newline. Useful for building a
	/// line from multiple segments.
	/// </summary>
	/// <param name="text">The text to write.</param>
	public void Write(string text)
	{
		WriteIndent();
		_sb.Append(text);
	}

	// ------------------------------------------------------------------
	// Raw (non-indented) write operations
	// ------------------------------------------------------------------

	/// <summary>
	/// Appends <paramref name="text"/> followed by a newline
	/// <strong>without</strong> indentation. Useful for preprocessor
	/// directives (<c>#if</c>, <c>#endif</c>, <c>#pragma</c>).
	/// </summary>
	/// <param name="text">The text to write.</param>
	public void WriteLineRaw(string text)
	{
		_sb.AppendLine(text);
	}

	/// <summary>
	/// Appends <paramref name="text"/> <strong>without</strong> indentation
	/// or a trailing newline. Useful for continuing a line started with
	/// <see cref="Write"/>.
	/// </summary>
	/// <param name="text">The text to append.</param>
	public void WriteRaw(string text)
	{
		_sb.Append(text);
	}

	// ------------------------------------------------------------------
	// Output
	// ------------------------------------------------------------------

	/// <summary>
	/// Gets the number of characters currently in the buffer.
	/// </summary>
	public int Length => _sb.Length;

	/// <summary>
	/// Returns the accumulated source text.
	/// </summary>
	public override string ToString() => _sb.ToString();

	// ------------------------------------------------------------------
	// Internals
	// ------------------------------------------------------------------

	/// <summary>
	/// Writes the current indentation (four spaces per level) to the buffer.
	/// </summary>
	private void WriteIndent()
	{
		for (int i = 0; i < _indentLevel; i++)
		{
			_sb.Append("    ");
		}
	}
}
