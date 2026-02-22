using System;
using System.Text;
using Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;
using Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;
using Uno.Extensions.Http.Kiota.SourceGenerator.Refinement;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Emitter;

/// <summary>
/// Emits C# constructor declarations from <see cref="CodeMethod"/> nodes with
/// <see cref="CodeMethodKind.Constructor"/> kind in the CodeDOM tree. Produces
/// output matching Kiota CLI patterns for three distinct constructor shapes:
/// <list type="bullet">
///   <item><b>Root client constructor</b> — takes <c>IRequestAdapter</c>,
///   calls <c>base(requestAdapter, urlTemplate, new Dictionary&lt;string, object&gt;())</c>,
///   registers default serializer/deserializer factories, sets the base URL
///   fallback, and adds the <c>"baseurl"</c> path parameter.</item>
///   <item><b>Request builder constructor (path parameters)</b> — takes
///   <c>Dictionary&lt;string, object&gt; pathParameters</c> and
///   <c>IRequestAdapter requestAdapter</c>, forwards to
///   <c>base(requestAdapter, urlTemplate, pathParameters)</c>.</item>
///   <item><b>Request builder constructor (raw URL)</b> — takes
///   <c>string rawUrl</c> and <c>IRequestAdapter requestAdapter</c>,
///   forwards to <c>base(requestAdapter, urlTemplate, rawUrl)</c>.</item>
///   <item><b>Model constructor</b> — parameterless, optionally initializes
///   <c>BackingStore</c> and/or <c>AdditionalData</c>.</item>
/// </list>
/// <para>
/// Called from <see cref="CSharpEmitter.EmitClassBody"/> for each constructor
/// in the canonical member ordering.
/// </para>
/// </summary>
internal sealed class ConstructorEmitter
{
	private readonly KiotaGeneratorConfig _config;

	/// <summary>
	/// Initializes a new <see cref="ConstructorEmitter"/> with the given
	/// generator configuration.
	/// </summary>
	/// <param name="config">
	/// The generator configuration controlling client name, backing store,
	/// and additional data options.
	/// </param>
	public ConstructorEmitter(KiotaGeneratorConfig config)
	{
		_config = config;
	}

	/// <summary>
	/// Emits a single constructor declaration into the given
	/// <paramref name="writer"/>.
	/// </summary>
	/// <param name="writer">The code writer to emit into.</param>
	/// <param name="ctor">The constructor method to emit.</param>
	/// <param name="cls">The owning class (used for context).</param>
	public void Emit(CodeWriter writer, CodeMethod ctor, CodeClass cls)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		if (ctor is null)
		{
			throw new ArgumentNullException(nameof(ctor));
		}

		if (cls is null)
		{
			throw new ArgumentNullException(nameof(cls));
		}

		if (cls.Kind == CodeClassKind.RequestBuilder)
		{
			if (IsRootClientConstructor(ctor))
			{
				EmitRootClientConstructor(writer, ctor, cls);
			}
			else if (HasParameterOfKind(ctor, CodeParameterKind.Path))
			{
				EmitPathParametersConstructor(writer, ctor, cls);
			}
			else if (HasParameterOfKind(ctor, CodeParameterKind.RawUrl))
			{
				EmitRawUrlConstructor(writer, ctor, cls);
			}
		}
		else if (cls.Kind == CodeClassKind.Model)
		{
			EmitModelConstructor(writer, ctor, cls);
		}
	}

	// ==================================================================
	// Root client constructor
	// ==================================================================

	/// <summary>
	/// Emits the root client class constructor that:
	/// <list type="number">
	///   <item>Calls <c>base(requestAdapter, urlTemplate, new Dictionary&lt;string, object&gt;())</c></item>
	///   <item>Registers default serializer factories</item>
	///   <item>Registers default deserializer factories</item>
	///   <item>Sets <c>requestAdapter.BaseUrl</c> if empty</item>
	///   <item>Adds <c>"baseurl"</c> to <c>PathParameters</c></item>
	/// </list>
	/// </summary>
	private void EmitRootClientConstructor(CodeWriter writer, CodeMethod ctor, CodeClass cls)
	{
		var urlTemplate = GetUrlTemplateValue(cls);

		// XML doc summary.
		CSharpEmitter.WriteXmlDocSummary(
			writer,
			"Instantiates a new <see cref=\""
			+ CSharpConventionService.GetGloballyQualifiedName(cls)
			+ "\"/> and sets the default values.");

		// XML doc param.
		writer.WriteLine(
			"/// <param name=\"requestAdapter\">"
			+ "The request adapter to use to execute the requests."
			+ "</param>");

		// Constructor signature with base() call.
		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(ctor.Access)
			+ " "
			+ cls.Name
			+ "(IRequestAdapter requestAdapter)"
			+ " : base(requestAdapter, \""
			+ EscapeStringLiteral(urlTemplate)
			+ "\", new Dictionary<string, object>())");
		writer.OpenBlock();

		// Register default serializer factories.
		writer.WriteLine(
			"ApiClientBuilder.RegisterDefaultSerializer<JsonSerializationWriterFactory>();");
		writer.WriteLine(
			"ApiClientBuilder.RegisterDefaultSerializer<TextSerializationWriterFactory>();");
		writer.WriteLine(
			"ApiClientBuilder.RegisterDefaultSerializer<FormSerializationWriterFactory>();");
		writer.WriteLine(
			"ApiClientBuilder.RegisterDefaultSerializer<MultipartSerializationWriterFactory>();");

		// Register default deserializer factories.
		writer.WriteLine(
			"ApiClientBuilder.RegisterDefaultDeserializer<JsonParseNodeFactory>();");
		writer.WriteLine(
			"ApiClientBuilder.RegisterDefaultDeserializer<TextParseNodeFactory>();");
		writer.WriteLine(
			"ApiClientBuilder.RegisterDefaultDeserializer<FormParseNodeFactory>();");

		// Base URL fallback.
		var baseUrl = ctor.BaseUrl;
		if (!string.IsNullOrEmpty(baseUrl))
		{
			writer.WriteLine("if (string.IsNullOrEmpty(RequestAdapter.BaseUrl))");
			writer.OpenBlock();
			writer.WriteLine(
				"RequestAdapter.BaseUrl = \""
				+ EscapeStringLiteral(baseUrl)
				+ "\";");
			writer.CloseBlock();
		}

		// Add base URL to PathParameters.
		writer.WriteLine(
			"PathParameters.TryAdd(\"baseurl\", RequestAdapter.BaseUrl);");

		writer.CloseBlock();
	}

	// ==================================================================
	// Request builder constructor (path parameters overload)
	// ==================================================================

	/// <summary>
	/// Emits a request builder constructor that receives path parameters
	/// and a request adapter, forwarding both to the
	/// <c>BaseRequestBuilder</c> via <c>base()</c>.
	/// <para>
	/// Pattern:
	/// <code>
	/// public {Name}(Dictionary&lt;string, object&gt; pathParameters,
	///     IRequestAdapter requestAdapter)
	///     : base(requestAdapter, "{urlTemplate}", pathParameters)
	/// {
	/// }
	/// </code>
	/// </para>
	/// </summary>
	private void EmitPathParametersConstructor(CodeWriter writer, CodeMethod ctor, CodeClass cls)
	{
		var urlTemplate = GetUrlTemplateValue(cls);

		// XML doc summary.
		CSharpEmitter.WriteXmlDocSummary(
			writer,
			"Instantiates a new <see cref=\""
			+ CSharpConventionService.GetGloballyQualifiedName(cls)
			+ "\"/> and sets the default values.");

		// XML doc params.
		writer.WriteLine(
			"/// <param name=\"pathParameters\">"
			+ "Path parameters for the request"
			+ "</param>");
		writer.WriteLine(
			"/// <param name=\"requestAdapter\">"
			+ "The request adapter to use to execute the requests."
			+ "</param>");

		// Constructor signature with base() call.
		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(ctor.Access)
			+ " "
			+ cls.Name
			+ "(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter)"
			+ " : base(requestAdapter, \""
			+ EscapeStringLiteral(urlTemplate)
			+ "\", pathParameters)");
		writer.OpenBlock();
		writer.CloseBlock();
	}

	// ==================================================================
	// Request builder constructor (raw URL overload)
	// ==================================================================

	/// <summary>
	/// Emits a request builder constructor that receives a raw URL string
	/// and a request adapter, forwarding both to
	/// <c>BaseRequestBuilder</c> via <c>base()</c>.
	/// <para>
	/// Pattern:
	/// <code>
	/// public {Name}(string rawUrl, IRequestAdapter requestAdapter)
	///     : base(requestAdapter, "{urlTemplate}", rawUrl)
	/// {
	/// }
	/// </code>
	/// </para>
	/// </summary>
	private void EmitRawUrlConstructor(CodeWriter writer, CodeMethod ctor, CodeClass cls)
	{
		var urlTemplate = GetUrlTemplateValue(cls);

		// XML doc summary.
		CSharpEmitter.WriteXmlDocSummary(
			writer,
			"Instantiates a new <see cref=\""
			+ CSharpConventionService.GetGloballyQualifiedName(cls)
			+ "\"/> and sets the default values.");

		// XML doc params.
		writer.WriteLine(
			"/// <param name=\"rawUrl\">"
			+ "The raw URL to use for the request builder."
			+ "</param>");
		writer.WriteLine(
			"/// <param name=\"requestAdapter\">"
			+ "The request adapter to use to execute the requests."
			+ "</param>");

		// Constructor signature with base() call.
		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(ctor.Access)
			+ " "
			+ cls.Name
			+ "(string rawUrl, IRequestAdapter requestAdapter)"
			+ " : base(requestAdapter, \""
			+ EscapeStringLiteral(urlTemplate)
			+ "\", rawUrl)");
		writer.OpenBlock();
		writer.CloseBlock();
	}

	// ==================================================================
	// Model constructor
	// ==================================================================

	/// <summary>
	/// Emits a model class constructor that optionally initializes:
	/// <list type="bullet">
	///   <item><c>BackingStore</c> from <c>BackingStoreFactorySingleton</c>
	///   when <see cref="KiotaGeneratorConfig.UsesBackingStore"/> is enabled</item>
	///   <item><c>AdditionalData</c> as a new <c>Dictionary&lt;string, object&gt;</c>
	///   when <see cref="KiotaGeneratorConfig.IncludeAdditionalData"/> is enabled</item>
	/// </list>
	/// </summary>
	private void EmitModelConstructor(CodeWriter writer, CodeMethod ctor, CodeClass cls)
	{
		bool hasBackingStore = _config.UsesBackingStore && HasPropertyOfKind(cls, CodePropertyKind.BackingStore);
		bool hasAdditionalData = _config.IncludeAdditionalData && HasPropertyOfKind(cls, CodePropertyKind.AdditionalData);

		// If there is nothing to initialize, emit a minimal parameterless
		// constructor to match Kiota CLI output (which always emits one).
		bool hasBody = hasBackingStore || hasAdditionalData;

		// XML doc summary.
		CSharpEmitter.WriteXmlDocSummary(
			writer,
			"Instantiates a new <see cref=\""
			+ CSharpConventionService.GetGloballyQualifiedName(cls)
			+ "\"/> and sets the default values.");

		// Constructor signature (parameterless).
		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(ctor.Access)
			+ " "
			+ cls.Name
			+ "()");
		writer.OpenBlock();

		if (hasBackingStore)
		{
			writer.WriteLine(
				"BackingStore = BackingStoreFactorySingleton.Instance.CreateBackingStore();");
		}

		if (hasAdditionalData)
		{
			writer.WriteLine(
				"AdditionalData = new Dictionary<string, object>();");
		}

		writer.CloseBlock();
	}

	// ==================================================================
	// Helpers
	// ==================================================================

	/// <summary>
	/// Determines whether a constructor is the root client constructor.
	/// Root client constructors are identified by having a <c>BaseUrl</c>
	/// value set (by <c>KiotaCodeDomBuilder</c>) and having only a
	/// <see cref="CodeParameterKind.RequestAdapter"/> parameter (no path
	/// parameters or raw URL).
	/// </summary>
	private static bool IsRootClientConstructor(CodeMethod ctor)
	{
		// The root client constructor has BaseUrl set and only takes
		// an IRequestAdapter parameter.
		if (ctor.BaseUrl != null)
		{
			return true;
		}

		// Fallback: a constructor that has only a RequestAdapter parameter
		// and no Path or RawUrl parameters is the root client constructor.
		bool hasAdapter = false;
		bool hasOther = false;
		for (int i = 0; i < ctor.Parameters.Count; i++)
		{
			if (ctor.Parameters[i].Kind == CodeParameterKind.RequestAdapter)
			{
				hasAdapter = true;
			}
			else
			{
				hasOther = true;
			}
		}

		return hasAdapter && !hasOther;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the constructor has a parameter of
	/// the specified <paramref name="kind"/>.
	/// </summary>
	private static bool HasParameterOfKind(CodeMethod method, CodeParameterKind kind)
	{
		for (int i = 0; i < method.Parameters.Count; i++)
		{
			if (method.Parameters[i].Kind == kind)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the class has a property of the
	/// specified <paramref name="kind"/>.
	/// </summary>
	private static bool HasPropertyOfKind(CodeClass cls, CodePropertyKind kind)
	{
		for (int i = 0; i < cls.Properties.Count; i++)
		{
			if (cls.Properties[i].Kind == kind)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Extracts the URL template string from the owning class's
	/// <see cref="CodePropertyKind.UrlTemplate"/> property. The URL template
	/// is stored in the property's <see cref="CodeProperty.SerializedName"/>.
	/// </summary>
	/// <param name="cls">The class containing the UrlTemplate property.</param>
	/// <returns>
	/// The URL template string, or an empty string if not found.
	/// </returns>
	private static string GetUrlTemplateValue(CodeClass cls)
	{
		for (int i = 0; i < cls.Properties.Count; i++)
		{
			if (cls.Properties[i].Kind == CodePropertyKind.UrlTemplate)
			{
				return cls.Properties[i].SerializedName ?? string.Empty;
			}
		}

		return string.Empty;
	}

	/// <summary>
	/// Escapes a string for use inside a C# string literal.
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
