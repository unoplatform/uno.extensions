#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;
using Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;
using Uno.Extensions.Http.Kiota.SourceGenerator.Refinement;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Emitter;

/// <summary>
/// Emits C# class declaration structures from <see cref="CodeClass"/> nodes in
/// the CodeDOM tree. Handles all file-level and type-level framing that
/// surrounds the class body members:
/// <list type="bullet">
///   <item><c>// &lt;auto-generated/&gt;</c> header and
///   <c>#pragma warning disable CS0618</c></item>
///   <item><c>using</c> directives selected by class kind (root client,
///   request builder, or model)</item>
///   <item><c>namespace { ... }</c> wrapper</item>
///   <item>XML documentation <c>&lt;summary&gt;</c> on the class</item>
///   <item><c>[Obsolete]</c> attribute for deprecated request-configuration
///   classes</item>
///   <item><c>[global::System.CodeDom.Compiler.GeneratedCode("Kiota", "...")]</c>
///   attribute</item>
///   <item><c>partial class</c> declaration with base class and interface
///   list</item>
/// </list>
/// <para>
/// Expected output structure:
/// <code>
/// // &lt;auto-generated/&gt;
/// #pragma warning disable CS0618
/// using Microsoft.Kiota.Abstractions.Extensions;
/// // ... additional using directives ...
/// namespace MyApp.Client
/// {
///     /// &lt;summary&gt;Description&lt;/summary&gt;
///     [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
///     public partial class PetStoreClient : BaseRequestBuilder
///     {
///         // ... body emitted by other sub-emitters ...
///     }
/// }
/// </code>
/// </para>
/// <para>
/// Called from <see cref="CSharpEmitter"/> when emitting class files and
/// inner class declarations. Body content (properties, methods, constructors,
/// etc.) is handled by the respective sub-emitters and orchestrated by
/// <see cref="CSharpEmitter"/>.
/// </para>
/// </summary>
internal sealed class ClassDeclarationEmitter
{
	private readonly KiotaGeneratorConfig _config;

	/// <summary>
	/// Initializes a new <see cref="ClassDeclarationEmitter"/> with the given
	/// generator configuration.
	/// </summary>
	/// <param name="config">
	/// The generator configuration controlling client class name and namespace
	/// options used for selecting the appropriate <c>using</c> directives.
	/// </param>
	public ClassDeclarationEmitter(KiotaGeneratorConfig config)
	{
		_config = config;
	}

	// ==================================================================
	// File-level emission
	// ==================================================================

	/// <summary>
	/// Emits the file-level preamble for a class source file: the
	/// <c>// &lt;auto-generated/&gt;</c> header, <c>#pragma warning</c>
	/// suppression, <c>using</c> directives, and the opening
	/// <c>namespace</c> block.
	/// </summary>
	/// <param name="writer">The code writer to emit into.</param>
	/// <param name="usings">
	/// Ordered collection of namespace strings to emit as <c>using</c>
	/// directives.
	/// </param>
	/// <param name="namespaceName">
	/// The C# namespace name for the <c>namespace</c> declaration.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="writer"/> is <see langword="null"/>.
	/// </exception>
	public void EmitFileStart(
		CodeWriter writer,
		IReadOnlyList<string> usings,
		string namespaceName)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		// ── File header ──
		CSharpEmitter.WriteFileHeader(writer);

		// ── Using directives ──
		CSharpEmitter.WriteUsings(writer, usings);

		// ── Namespace ──
		writer.WriteLine("namespace " + namespaceName);
		writer.OpenBlock();
	}

	/// <summary>
	/// Emits the closing brace for the <c>namespace</c> block that was
	/// opened by <see cref="EmitFileStart"/>.
	/// </summary>
	/// <param name="writer">The code writer to emit into.</param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="writer"/> is <see langword="null"/>.
	/// </exception>
	public void EmitFileEnd(CodeWriter writer)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		writer.CloseBlock();

		// Close the #pragma warning disable CS0618 that was opened in
		// EmitFileStart via WriteFileHeader.
		writer.WriteLineRaw("#pragma warning restore CS0618");
	}

	// ==================================================================
	// Class declaration emission
	// ==================================================================

	/// <summary>
	/// Emits the opening of a class declaration: XML documentation summary,
	/// <c>[Obsolete]</c> attribute (for request-configuration classes),
	/// <c>[GeneratedCode]</c> attribute, the <c>partial class</c> signature
	/// with base class and interface list, and the opening brace.
	/// <para>
	/// Called for both top-level classes and inner (nested) classes. The
	/// caller is responsible for emitting the class body (properties, methods,
	/// constructors, etc.) and calling <see cref="EmitClassClose"/> to close
	/// the declaration.
	/// </para>
	/// </summary>
	/// <param name="writer">The code writer to emit into.</param>
	/// <param name="cls">
	/// The class to emit. Must not be <see langword="null"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="writer"/> or <paramref name="cls"/> is
	/// <see langword="null"/>.
	/// </exception>
	public void EmitClassOpen(CodeWriter writer, CodeClass cls)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		if (cls is null)
		{
			throw new ArgumentNullException(nameof(cls));
		}

		// ── XML doc summary ──
		CSharpEmitter.WriteXmlDocSummary(writer, cls.Description);

		// ── [Obsolete] attribute for deprecated request-configuration classes ──
		if (cls.Kind == CodeClassKind.RequestConfiguration)
		{
			writer.WriteLine(
				"[Obsolete(\"This class is deprecated. "
				+ "Please use the generic RequestConfiguration class "
				+ "generated by the generator.\")]");
		}

		// ── [GeneratedCode] attribute ──
		CSharpEmitter.WriteGeneratedCodeAttribute(writer);

		// ── #pragma CS1591 guards for model classes without doc summary ──
		// Kiota CLI wraps class declarations that lack XML doc comments
		// in #pragma directives to suppress missing-doc warnings.
		bool needsPragmaGuard = string.IsNullOrEmpty(cls.Description);
		if (needsPragmaGuard)
		{
			writer.WriteLine("#pragma warning disable CS1591");
		}

		// ── Class signature ──
		WriteClassSignature(writer, cls);

		// ── Close #pragma CS1591 guard ──
		if (needsPragmaGuard)
		{
			writer.WriteLine("#pragma warning restore CS1591");
		}

		// ── Opening brace ──
		writer.OpenBlock();
	}

	/// <summary>
	/// Emits the closing brace of a class declaration that was opened by
	/// <see cref="EmitClassOpen"/>.
	/// </summary>
	/// <param name="writer">The code writer to emit into.</param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="writer"/> is <see langword="null"/>.
	/// </exception>
	public void EmitClassClose(CodeWriter writer)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		writer.CloseBlock();
	}

	// ==================================================================
	// Using-directive selection
	// ==================================================================

	/// <summary>
	/// Returns the appropriate <c>using</c> directive list based on the
	/// class kind and role. Root client classes get serialization factory
	/// usings; other request builders get standard request builder usings;
	/// model, query-parameter, and request-configuration classes get model
	/// usings.
	/// </summary>
	/// <param name="cls">The class to determine usings for.</param>
	/// <param name="namespaceName">Optional namespace name used for filtering self-references.</param>
	/// <returns>
	/// An ordered read-only list of namespace strings for <c>using</c>
	/// directives.
	/// </returns>
	public IReadOnlyList<string> GetUsingsForClass(CodeClass cls, string namespaceName = null)
	{
		if (cls is null)
		{
			throw new ArgumentNullException(nameof(cls));
		}

		IReadOnlyList<string> baseUsings;

		if (cls.Kind == CodeClassKind.RequestBuilder)
		{
			if (IsRootClient(cls))
			{
				baseUsings = CSharpConventionService.ClientRootUsings;
			}
			else
			{
				baseUsings = CSharpConventionService.RequestBuilderUsings;
			}
		}
		else
		{
			// Model, QueryParameters, and RequestConfiguration classes.
			// Composed-type wrappers with only primitive members need
			// minimal usings (just Serialization namespace).
			if (IsComposedTypeWrapperWithPrimitivesOnly(cls))
			{
				baseUsings = CSharpConventionService.ComposedTypePrimitiveUsings;
			}
			else
			{
				// Check whether the model needs the extra Abstractions using.
				baseUsings = CSharpConventionService.NeedsAbstractionsUsing(cls)
					? CSharpConventionService.ModelWithAbstractionsUsings
					: CSharpConventionService.ModelBaseUsings;
			}
		}

		// Compute local namespace usings from referenced internal types.
		if (string.IsNullOrEmpty(namespaceName))
		{
			return baseUsings;
		}

		var localUsings = CSharpConventionService.CollectLocalUsings(cls, namespaceName);
		if (localUsings.Count == 0)
		{
			return baseUsings;
		}

		// Merge base usings (in their predefined Kiota order) with local
		// usings (sorted alphabetically) so that each local using is
		// interleaved at its correct alphabetical position.
		var merged = new List<string>(baseUsings.Count + localUsings.Count);
		int li = 0;

		for (int bi = 0; bi < baseUsings.Count; bi++)
		{
			// Insert any local usings that alphabetically precede this
			// base using.
			while (li < localUsings.Count
				&& string.Compare(localUsings[li], baseUsings[bi], StringComparison.Ordinal) < 0)
			{
				merged.Add(localUsings[li++]);
			}

			merged.Add(baseUsings[bi]);
		}

		// Append any remaining local usings that come after all base usings.
		while (li < localUsings.Count)
		{
			merged.Add(localUsings[li++]);
		}

		return merged;
	}

	// ==================================================================
	// Class signature helper
	// ==================================================================

	/// <summary>
	/// Writes the class signature line:
	/// <c>{access} partial class {Name} : {BaseClass}, {IInterface}, ...</c>
	/// </summary>
	/// <param name="writer">The code writer.</param>
	/// <param name="cls">The class whose signature to emit.</param>
	private static void WriteClassSignature(CodeWriter writer, CodeClass cls)
	{
		var sb = new StringBuilder();

		// Access modifier.
		sb.Append(CSharpConventionService.GetAccessModifier(cls.Access));
		sb.Append(" partial class ");
		sb.Append(cls.Name);

		// Base class and interfaces.
		var inheritanceParts = new List<string>();

		if (cls.BaseClass != null)
		{
			inheritanceParts.Add(CSharpConventionService.GetTypeReference(cls.BaseClass));
		}

		for (int i = 0; i < cls.Interfaces.Count; i++)
		{
			inheritanceParts.Add(CSharpConventionService.GetTypeReference(cls.Interfaces[i]));
		}

		if (inheritanceParts.Count > 0)
		{
			sb.Append(" : ");

			for (int i = 0; i < inheritanceParts.Count; i++)
			{
				if (i > 0)
				{
					sb.Append(", ");
				}

				sb.Append(inheritanceParts[i]);
			}
		}

		writer.WriteLine(sb.ToString());
	}

	// ==================================================================
	// Private helpers
	// ==================================================================

	/// <summary>
	/// Returns <see langword="true"/> when the given class is the root
	/// client class (matches <see cref="KiotaGeneratorConfig.ClientClassName"/>).
	/// </summary>
	private bool IsRootClient(CodeClass cls)
	{
		return string.Equals(cls.Name, _config.ClientClassName, StringComparison.Ordinal);
	}

	/// <summary>
	/// Returns <see langword="true"/> when the class is a composed-type
	/// wrapper where ALL custom properties are primitive types (no object
	/// references). Such wrappers need minimal usings.
	/// </summary>
	private static bool IsComposedTypeWrapperWithPrimitivesOnly(CodeClass cls)
	{
		bool hasComposedTypeWrapper = false;
		for (int i = 0; i < cls.Interfaces.Count; i++)
		{
			if (cls.Interfaces[i].Name == "IComposedTypeWrapper")
			{
				hasComposedTypeWrapper = true;
				break;
			}
		}

		if (!hasComposedTypeWrapper)
		{
			return false;
		}

		// Check if all custom properties are primitives.
		for (int i = 0; i < cls.Properties.Count; i++)
		{
			var prop = cls.Properties[i];
			if (prop.Kind != CodePropertyKind.Custom)
			{
				continue;
			}

			if (prop.Type is CodeType ct && CSharpConventionService.IsPrimitiveType(ct.Name))
			{
				continue;
			}

			// Non-primitive property found.
			return false;
		}

		return true;
	}
}
