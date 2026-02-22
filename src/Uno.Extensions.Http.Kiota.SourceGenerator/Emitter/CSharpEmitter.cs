using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;
using Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;
using Uno.Extensions.Http.Kiota.SourceGenerator.Refinement;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Emitter;

/// <summary>
/// Main C# code emitter that orchestrates the emission of source files from
/// a refined CodeDOM tree. Walks the CodeDOM namespace hierarchy and produces
/// <c>(hintName, source)</c> tuples suitable for Roslyn's
/// <c>SourceProductionContext.AddSource</c> API.
/// <para>
/// The emitter coordinates a set of sub-emitters, each responsible for a
/// specific concern within a generated file:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Sub-emitter</term>
///     <description>Responsibility / Task</description>
///   </listheader>
///   <item>
///     <term><c>ClassDeclarationEmitter</c></term>
///     <description>File header, usings, namespace, class signature (T037)</description>
///   </item>
///   <item>
///     <term><c>ConstructorEmitter</c></term>
///     <description>Constructor bodies with <c>base()</c> calls (T038)</description>
///   </item>
///   <item>
///     <term><c>PropertyEmitter</c></term>
///     <description>Property declarations with nullable guards (T039)</description>
///   </item>
///   <item>
///     <term><c>MethodEmitter</c></term>
///     <description>Executor, request-info, WithUrl, and indexer methods (T040)</description>
///   </item>
///   <item>
///     <term><c>SerializerEmitter</c></term>
///     <description><c>Serialize()</c> method body (T041)</description>
///   </item>
///   <item>
///     <term><c>DeserializerEmitter</c></term>
///     <description><c>GetFieldDeserializers()</c> body (T042)</description>
///   </item>
///   <item>
///     <term><c>FactoryMethodEmitter</c></term>
///     <description><c>CreateFromDiscriminatorValue</c> factory (T043)</description>
///   </item>
///   <item>
///     <term><c>EnumEmitter</c></term>
///     <description>Enum type declarations (T044)</description>
///   </item>
/// </list>
/// <para>
/// Until a sub-emitter is implemented, its delegation point in this class
/// emits nothing (producing a valid but empty structural skeleton). Each
/// sub-emitter task updates the corresponding delegation method to call the
/// new sub-emitter class.
/// </para>
/// </summary>
internal sealed class CSharpEmitter
{
	private readonly KiotaGeneratorConfig _config;
	private readonly ClassDeclarationEmitter _classDeclarationEmitter;
	private readonly PropertyEmitter _propertyEmitter;
	private readonly ConstructorEmitter _constructorEmitter;
	private readonly MethodEmitter _methodEmitter;
	private readonly SerializerEmitter _serializerEmitter;
	private readonly DeserializerEmitter _deserializerEmitter;
	private readonly FactoryMethodEmitter _factoryMethodEmitter;
	private readonly EnumEmitter _enumEmitter;

	/// <summary>
	/// Initializes a new <see cref="CSharpEmitter"/> with the given
	/// generator configuration.
	/// </summary>
	/// <param name="config">
	/// The generator configuration controlling client name, namespace,
	/// backing store, additional data, and backward compatibility options.
	/// </param>
	public CSharpEmitter(KiotaGeneratorConfig config)
	{
		_config = config;
		_classDeclarationEmitter = new ClassDeclarationEmitter(config);
		_propertyEmitter = new PropertyEmitter(config);
		_constructorEmitter = new ConstructorEmitter(config);
		_methodEmitter = new MethodEmitter(config);
		_serializerEmitter = new SerializerEmitter(config);
		_deserializerEmitter = new DeserializerEmitter(config);
		_factoryMethodEmitter = new FactoryMethodEmitter(config);
		_enumEmitter = new EnumEmitter(config);
	}

	// ==================================================================
	// Public API
	// ==================================================================

	/// <summary>
	/// Emits C# source files for every type in the refined CodeDOM tree.
	/// </summary>
	/// <param name="root">
	/// The root <see cref="CodeNamespace"/> produced by
	/// <see cref="CodeDom.KiotaCodeDomBuilder"/> and refined by
	/// <see cref="CSharpRefiner"/>. Must not be <see langword="null"/>.
	/// </param>
	/// <returns>
	/// A lazily-evaluated sequence of <c>(hintName, source)</c> pairs.
	/// <c>hintName</c> is a unique identifier for the generated file
	/// (e.g., <c>"MyApp.PetStore.Models.Pet.g.cs"</c>).
	/// <c>source</c> is the complete C# source text.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="root"/> is <see langword="null"/>.
	/// </exception>
	public IEnumerable<(string HintName, string Source)> Emit(CodeNamespace root)
	{
		if (root is null)
		{
			throw new ArgumentNullException(nameof(root));
		}

		return EmitNamespaceTypes(root);
	}

	// ==================================================================
	// Tree walking
	// ==================================================================

	/// <summary>
	/// Recursively walks the namespace tree and emits a source file for
	/// every top-level <see cref="CodeClass"/> and <see cref="CodeEnum"/>.
	/// Inner classes are emitted inside their parent class's file.
	/// </summary>
	private IEnumerable<(string HintName, string Source)> EmitNamespaceTypes(
		CodeNamespace ns)
	{
		var namespaceName = GetNamespaceName(ns);

		// Emit each top-level class in this namespace.
		for (int i = 0; i < ns.Classes.Count; i++)
		{
			var cls = ns.Classes[i];
			var hintName = CSharpConventionService.GetHintName(cls);
			var source = EmitClassFile(cls, namespaceName);
			yield return (hintName, source);
		}

		// Emit each top-level enum in this namespace.
		for (int i = 0; i < ns.Enums.Count; i++)
		{
			var en = ns.Enums[i];
			var hintName = CSharpConventionService.GetHintName(en);
			var source = EmitEnumFile(en, namespaceName);
			yield return (hintName, source);
		}

		// Recurse into child namespaces.
		for (int i = 0; i < ns.Namespaces.Count; i++)
		{
			foreach (var result in EmitNamespaceTypes(ns.Namespaces[i]))
			{
				yield return result;
			}
		}
	}

	// ==================================================================
	// Class file emission
	// ==================================================================

	/// <summary>
	/// Produces a complete C# source file for the given top-level class.
	/// </summary>
	/// <param name="cls">The class to emit.</param>
	/// <param name="namespaceName">
	/// The C# namespace name for the <c>namespace</c> declaration.
	/// </param>
	/// <returns>The complete C# source text.</returns>
	private string EmitClassFile(CodeClass cls, string namespaceName)
	{
		var writer = new CodeWriter();

		// ── File preamble (header, usings, namespace) ──
		var usings = _classDeclarationEmitter.GetUsingsForClass(cls, namespaceName);
		_classDeclarationEmitter.EmitFileStart(writer, usings, namespaceName);

		// ── Type declaration ──
		EmitClass(writer, cls);

		// ── Close namespace ──
		_classDeclarationEmitter.EmitFileEnd(writer);

		return writer.ToString();
	}

	/// <summary>
	/// Emits a single class declaration (attribute, signature, body).
	/// Called for both top-level classes and inner classes.
	/// </summary>
	private void EmitClass(CodeWriter writer, CodeClass cls)
	{
		// ── Class declaration (XML doc, attributes, signature, open brace) ──
		_classDeclarationEmitter.EmitClassOpen(writer, cls);

		// ── Class body — member orchestration ──
		EmitClassBody(writer, cls);

		// ── Close class ──
		_classDeclarationEmitter.EmitClassClose(writer);
	}

	/// <summary>
	/// Orchestrates the emission of all members within a class body, in the
	/// canonical order that matches Kiota CLI output.
	/// <para>
	/// Each member category delegates to a sub-emitter method. The
	/// orchestration order is:
	/// </para>
	/// <list type="number">
	///   <item>Navigation properties — request builder child accessors</item>
	///   <item>Indexers — parameterized path segment accessors</item>
	///   <item>Custom / model properties</item>
	///   <item>Additional data property</item>
	///   <item>Backing store property</item>
	///   <item>Query parameter properties</item>
	///   <item>Constructors</item>
	///   <item>Factory methods (<c>CreateFromDiscriminatorValue</c>)</item>
	///   <item>Deserializer (<c>GetFieldDeserializers</c>)</item>
	///   <item>Serializer (<c>Serialize</c>)</item>
	///   <item>HTTP executor methods (<c>GetAsync</c>, etc.)</item>
	///   <item>Request-info builders (<c>ToGetRequestInformation</c>, etc.)</item>
	///   <item><c>WithUrl</c> method</item>
	///   <item>Inner classes recursively</item>
	/// </list>
	/// </summary>
	private void EmitClassBody(CodeWriter writer, CodeClass cls)
	{
		// ── 1. Navigation properties (request builders) ──
		foreach (var prop in IteratePropertiesOfKind(cls, CodePropertyKind.Navigation))
		{
			EmitProperty(writer, prop, cls);
		}

		// ── 2. Indexers ──
		for (int i = 0; i < cls.Indexers.Count; i++)
		{
			EmitIndexer(writer, cls.Indexers[i], cls);
		}

		// ── 3. AdditionalData + Custom / model properties + Error message override (sorted together alphabetically) ──
		{
			var combinedProps = new System.Collections.Generic.List<CodeProperty>();
			for (int i = 0; i < cls.Properties.Count; i++)
			{
				if (cls.Properties[i].Kind == CodePropertyKind.AdditionalData
					|| cls.Properties[i].Kind == CodePropertyKind.Custom
					|| cls.Properties[i].Kind == CodePropertyKind.ErrorMessageOverride)
				{
					combinedProps.Add(cls.Properties[i]);
				}
			}

			combinedProps.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));
			for (int i = 0; i < combinedProps.Count; i++)
			{
				if (combinedProps[i].Kind == CodePropertyKind.ErrorMessageOverride)
				{
					EmitErrorMessageOverrideProperty(writer, combinedProps[i]);
				}
				else
				{
					EmitProperty(writer, combinedProps[i], cls);
				}
			}
		}

		// ── 5. Backing store property ──
		foreach (var prop in IteratePropertiesOfKind(cls, CodePropertyKind.BackingStore))
		{
			EmitProperty(writer, prop, cls);
		}

		// ── 6. Query parameter properties ──
		foreach (var prop in IteratePropertiesOfKind(cls, CodePropertyKind.QueryParameter))
		{
			EmitProperty(writer, prop, cls);
		}

		// ── 7. Constructors ──
		foreach (var ctor in IterateMethodsOfKind(cls, CodeMethodKind.Constructor))
		{
			EmitConstructor(writer, ctor, cls);
		}

		// ── 8. Factory methods ──
		foreach (var factory in IterateMethodsOfKind(cls, CodeMethodKind.Factory))
		{
			EmitFactoryMethod(writer, factory, cls);
		}

		// ── 9. Deserializer (GetFieldDeserializers) ──
		foreach (var deser in IterateMethodsOfKind(cls, CodeMethodKind.Deserializer))
		{
			EmitDeserializerMethod(writer, deser, cls);
		}

		// ── 10. Serializer (Serialize) ──
		foreach (var ser in IterateMethodsOfKind(cls, CodeMethodKind.Serializer))
		{
			EmitSerializerMethod(writer, ser, cls);
		}

		// ── 11. HTTP executor methods (GetAsync, PostAsync, ...) ──
		foreach (var exec in IterateMethodsOfKind(cls, CodeMethodKind.RequestExecutor))
		{
			EmitExecutorMethod(writer, exec, cls);
		}

		// ── 12. Request-information builders (ToGetRequestInformation, ...) ──
		foreach (var req in IterateMethodsOfKind(cls, CodeMethodKind.RequestGenerator))
		{
			EmitRequestGeneratorMethod(writer, req, cls);
		}

		// ── 13. WithUrl method ──
		foreach (var withUrl in IterateMethodsOfKind(cls, CodeMethodKind.WithUrl))
		{
			EmitWithUrlMethod(writer, withUrl, cls);
		}

		// ── 14. Inner classes (recursive) ──
		for (int i = 0; i < cls.InnerClasses.Count; i++)
		{
			EmitClass(writer, cls.InnerClasses[i]);
		}
	}

	// ==================================================================
	// Enum file emission
	// ==================================================================

	/// <summary>
	/// Produces a complete C# source file for the given enum.
	/// </summary>
	private string EmitEnumFile(CodeEnum en, string namespaceName)
	{
		var writer = new CodeWriter();

		// ── File header (auto-generated, no CS0618 pragma for enums) ──
		writer.WriteLine("// <auto-generated/>");

		// ── Using directives ──
		WriteUsings(writer, CSharpConventionService.EnumUsings);

		// ── Namespace ──
		writer.WriteLine("namespace " + namespaceName);
		writer.OpenBlock();

		// ── Enum declaration ──
		EmitEnum(writer, en);

		writer.CloseBlock();

		return writer.ToString();
	}

	/// <summary>
	/// Emits a complete enum type declaration with <c>[GeneratedCode]</c>,
	/// optional <c>[Flags]</c>, and <c>[EnumMember]</c> attributes.
	/// <para>Delegation point → <see cref="EnumEmitter"/> (T044).</para>
	/// </summary>
	private void EmitEnum(CodeWriter writer, CodeEnum en)
	{
		_enumEmitter.Emit(writer, en);
	}

	// ==================================================================
	// Sub-emitter delegation points
	//
	// Each method below is a delegation point for a specific sub-emitter.
	// Currently these are stubs that emit nothing (or minimal output).
	// When the corresponding sub-emitter task is implemented, it will
	// create a dedicated class and these methods will delegate to it.
	// ==================================================================

	/// <summary>
	/// Emits a single property declaration.
	/// <para>Delegation point → <c>PropertyEmitter</c> (T039).</para>
	/// </summary>
	private void EmitProperty(CodeWriter writer, CodeProperty prop, CodeClass cls)
	{
		_propertyEmitter.Emit(writer, prop, cls);
	}

	/// <summary>
	/// Emits the <c>Message</c> override property for error models.
	/// </summary>
	private static void EmitErrorMessageOverrideProperty(CodeWriter writer, CodeProperty prop)
	{
		WriteXmlDocSummarySingleLine(writer, prop.Description ?? "The primary error message.");
		writer.WriteLine("public override string Message { get => MessageEscaped ?? string.Empty; }");
	}

	/// <summary>
	/// Emits an indexer accessor (parameterized path segment).
	/// <para>Delegation point → <c>MethodEmitter</c> (T040).</para>
	/// </summary>
	private void EmitIndexer(CodeWriter writer, CodeIndexer indexer, CodeClass cls)
	{
		_methodEmitter.EmitIndexer(writer, indexer, cls);
	}

	/// <summary>
	/// Emits a constructor method.
	/// <para>Delegation point → <c>ConstructorEmitter</c> (T038).</para>
	/// </summary>
	private void EmitConstructor(CodeWriter writer, CodeMethod ctor, CodeClass cls)
	{
		_constructorEmitter.Emit(writer, ctor, cls);
	}

	/// <summary>
	/// Emits a <c>CreateFromDiscriminatorValue</c> static factory method.
	/// <para>Delegation point → <c>FactoryMethodEmitter</c> (T043).</para>
	/// </summary>
	private void EmitFactoryMethod(CodeWriter writer, CodeMethod factory, CodeClass cls)
	{
		_factoryMethodEmitter.Emit(writer, factory, cls);
	}

	/// <summary>
	/// Emits a <c>GetFieldDeserializers</c> method.
	/// <para>Delegation point → <c>DeserializerEmitter</c> (T042).</para>
	/// </summary>
	private void EmitDeserializerMethod(CodeWriter writer, CodeMethod deser, CodeClass cls)
	{
		_deserializerEmitter.Emit(writer, deser, cls);
	}

	/// <summary>
	/// Emits a <c>Serialize</c> method.
	/// <para>Delegation point → <c>SerializerEmitter</c> (T041).</para>
	/// </summary>
	private void EmitSerializerMethod(CodeWriter writer, CodeMethod ser, CodeClass cls)
	{
		_serializerEmitter.Emit(writer, ser, cls);
	}

	/// <summary>
	/// Emits an HTTP executor method (<c>GetAsync</c>, <c>PostAsync</c>, etc.).
	/// <para>Delegation point → <c>MethodEmitter</c> (T040).</para>
	/// </summary>
	private void EmitExecutorMethod(CodeWriter writer, CodeMethod exec, CodeClass cls)
	{
		_methodEmitter.EmitExecutorMethod(writer, exec, cls);
	}

	/// <summary>
	/// Emits a request-information builder method
	/// (<c>ToGetRequestInformation</c>, etc.).
	/// <para>Delegation point → <c>MethodEmitter</c> (T040).</para>
	/// </summary>
	private void EmitRequestGeneratorMethod(CodeWriter writer, CodeMethod req, CodeClass cls)
	{
		_methodEmitter.EmitRequestGeneratorMethod(writer, req, cls);
	}

	/// <summary>
	/// Emits a <c>WithUrl</c> method.
	/// <para>Delegation point → <c>MethodEmitter</c> (T040).</para>
	/// </summary>
	private void EmitWithUrlMethod(CodeWriter writer, CodeMethod withUrl, CodeClass cls)
	{
		_methodEmitter.EmitWithUrlMethod(writer, withUrl, cls);
	}

	// ==================================================================
	// File-level structure helpers
	// ==================================================================

	/// <summary>
	/// Writes the standard auto-generated file header with pragma
	/// suppression.
	/// </summary>
	/// <param name="writer">The code writer.</param>
	internal static void WriteFileHeader(CodeWriter writer)
	{
		writer.WriteLine("// <auto-generated/>");
		writer.WriteLineRaw("#pragma warning disable CS0618");
	}

	/// <summary>
	/// Writes <c>using</c> directives for the given namespace list.
	/// </summary>
	/// <param name="writer">The code writer.</param>
	/// <param name="usings">
	/// Ordered collection of namespace strings to emit as <c>using</c>
	/// directives.
	/// </param>
	internal static void WriteUsings(CodeWriter writer, IReadOnlyList<string> usings)
	{
		for (int i = 0; i < usings.Count; i++)
		{
			writer.WriteLine("using " + usings[i] + ";");
		}
	}

	/// <summary>
	/// Writes the <c>[global::System.CodeDom.Compiler.GeneratedCode]</c>
	/// attribute on the current line.
	/// </summary>
	/// <param name="writer">The code writer.</param>
	internal static void WriteGeneratedCodeAttribute(CodeWriter writer)
	{
		writer.WriteLine(
			"[global::System.CodeDom.Compiler.GeneratedCode(\""
			+ CSharpConventionService.GeneratorName
			+ "\", \""
			+ CSharpConventionService.GeneratorVersion
			+ "\")]");
	}

	/// <summary>
	/// Writes an XML documentation <c>&lt;summary&gt;</c> block if
	/// <paramref name="description"/> is not <see langword="null"/> or empty.
	/// Uses multi-line format for class/method descriptions.
	/// </summary>
	/// <param name="writer">The code writer.</param>
	/// <param name="description">
	/// The description text, or <see langword="null"/> to skip.
	/// </param>
	internal static void WriteXmlDocSummary(CodeWriter writer, string description)
	{
		if (string.IsNullOrEmpty(description))
		{
			return;
		}

		writer.WriteLine("/// <summary>");
		writer.WriteLine("/// " + CSharpConventionService.EscapeXmlDoc(description));
		writer.WriteLine("/// </summary>");
	}

	/// <summary>
	/// Writes an XML documentation <c>&lt;summary&gt;</c> tag as a single
	/// line: <c>/// &lt;summary&gt;TEXT&lt;/summary&gt;</c>.
	/// Used for property descriptions to match Kiota CLI output format.
	/// </summary>
	/// <param name="writer">The code writer.</param>
	/// <param name="description">
	/// The description text, or <see langword="null"/> to skip.
	/// </param>
	internal static void WriteXmlDocSummarySingleLine(CodeWriter writer, string description)
	{
		if (string.IsNullOrEmpty(description))
		{
			return;
		}

		writer.WriteLine("/// <summary>" + CSharpConventionService.EscapeXmlDoc(description) + "</summary>");
	}

	// ==================================================================
	// Namespace resolution
	// ==================================================================

	/// <summary>
	/// Returns the C# namespace name for the given CodeDOM namespace by
	/// computing its fully qualified name.
	/// </summary>
	private static string GetNamespaceName(CodeNamespace ns)
	{
		return CSharpConventionService.GetFullyQualifiedName(ns);
	}

	// ==================================================================
	// Iteration helpers
	// ==================================================================

	/// <summary>
	/// Iterates properties of the given kind on a class, sorted
	/// alphabetically by name. Avoids LINQ allocations for source-generator
	/// performance.
	/// </summary>
	private static IEnumerable<CodeProperty> IteratePropertiesOfKind(
		CodeClass cls,
		CodePropertyKind kind)
	{
		// Collect matching properties and sort alphabetically by name
		// to match Kiota CLI output ordering.
		var matching = new List<CodeProperty>();
		for (int i = 0; i < cls.Properties.Count; i++)
		{
			if (cls.Properties[i].Kind == kind)
			{
				matching.Add(cls.Properties[i]);
			}
		}

		matching.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
		return matching;
	}

	/// <summary>
	/// Iterates methods of the given kind on a class, sorted
	/// alphabetically by name for deterministic output.
	/// </summary>
	private static List<CodeMethod> IterateMethodsOfKind(
		CodeClass cls,
		CodeMethodKind kind)
	{
		var result = new List<CodeMethod>();
		for (int i = 0; i < cls.Methods.Count; i++)
		{
			if (cls.Methods[i].Kind == kind)
			{
				result.Add(cls.Methods[i]);
			}
		}

		result.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
		return result;
	}

	// ==================================================================
	// String helpers
	// ==================================================================

	/// <summary>
	/// Escapes a string for use inside a C# string literal (double-quotes).
	/// </summary>
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
