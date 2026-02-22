#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.OpenApi.Models;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Parsing;

/// <summary>
/// Immutable result of parsing an OpenAPI document. Contains the parsed
/// <see cref="OpenApiDocument"/> (when successful) and any diagnostics
/// produced during parsing.
/// <para>
/// Implements <see cref="IEquatable{T}"/> so the Roslyn incremental pipeline
/// can detect unchanged results and skip downstream transforms.
/// </para>
/// </summary>
internal readonly struct OpenApiParseResult : IEquatable<OpenApiParseResult>
{
	/// <summary>
	/// Initializes a successful <see cref="OpenApiParseResult"/> with the
	/// parsed document and any non-fatal diagnostics.
	/// </summary>
	public OpenApiParseResult(OpenApiDocument document, ImmutableArray<Diagnostic> diagnostics)
	{
		Document = document;
		Diagnostics = diagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : diagnostics;
	}

	/// <summary>
	/// The parsed OpenAPI document, or <see langword="null"/> if parsing failed.
	/// </summary>
	public OpenApiDocument Document { get; }

	/// <summary>
	/// Diagnostics produced during parsing. May contain warnings even when
	/// <see cref="IsSuccess"/> is <see langword="true"/>.
	/// </summary>
	public ImmutableArray<Diagnostic> Diagnostics { get; }

	/// <summary>
	/// Returns <see langword="true"/> when the document was parsed successfully.
	/// There may still be warning-level diagnostics.
	/// </summary>
	public bool IsSuccess => Document != null;

	/// <summary>
	/// Returns <see langword="true"/> when at least one diagnostic has
	/// <see cref="DiagnosticSeverity.Error"/> severity.
	/// </summary>
	public bool HasErrors
	{
		get
		{
			if (Diagnostics.IsDefaultOrEmpty)
			{
				return false;
			}

			for (var i = 0; i < Diagnostics.Length; i++)
			{
				if (Diagnostics[i].Severity == DiagnosticSeverity.Error)
				{
					return true;
				}
			}

			return false;
		}
	}

	/// <summary>
	/// Creates a failed <see cref="OpenApiParseResult"/> with a single
	/// error diagnostic.
	/// </summary>
	public static OpenApiParseResult Failure(Diagnostic errorDiagnostic)
	{
		return new OpenApiParseResult(
			document: null,
			diagnostics: ImmutableArray.Create(errorDiagnostic));
	}

	// ------------------------------------------------------------------
	// IEquatable<T> — reference equality on the document because
	// OpenApiDocument is a mutable reference type that doesn't
	// implement structural equality. The incremental pipeline will
	// already compare the source text, so the document reference
	// changing is a reasonable proxy for content change.
	// ------------------------------------------------------------------

	/// <inheritdoc/>
	public bool Equals(OpenApiParseResult other)
	{
		return ReferenceEquals(Document, other.Document)
			&& Diagnostics.SequenceEqual(other.Diagnostics);
	}

	/// <inheritdoc/>
	public override bool Equals(object obj)
	{
		return obj is OpenApiParseResult other && Equals(other);
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		unchecked
		{
			var hash = Document?.GetHashCode() ?? 0;
			hash = (hash * 397) ^ Diagnostics.Length;
			return hash;
		}
	}

	/// <summary>Equality operator.</summary>
	public static bool operator ==(OpenApiParseResult left, OpenApiParseResult right) => left.Equals(right);

	/// <summary>Inequality operator.</summary>
	public static bool operator !=(OpenApiParseResult left, OpenApiParseResult right) => !left.Equals(right);
}
