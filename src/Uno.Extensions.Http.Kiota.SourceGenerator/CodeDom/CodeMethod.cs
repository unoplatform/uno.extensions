#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;

// ======================================================================
// CodeMethodKind enumeration
// ======================================================================

/// <summary>
/// Classifies the semantic role of a <see cref="CodeMethod"/> in the Kiota
/// code-generation model. The kind determines the shape of the emitted C#
/// method body (e.g., HTTP call dispatch, serialization logic, factory
/// switch, etc.).
/// </summary>
internal enum CodeMethodKind
{
	/// <summary>
	/// A class constructor that initializes the instance.
	/// <para>
	/// For <see cref="CodeClassKind.RequestBuilder"/> classes the constructor
	/// sets <c>PathParameters</c>, <c>UrlTemplate</c>, and
	/// <c>RequestAdapter</c> (and optionally registers default
	/// serializer/deserializer factories for the root client).
	/// </para>
	/// </summary>
	Constructor = 0,

	/// <summary>
	/// An HTTP operation executor method such as <c>GetAsync</c>,
	/// <c>PostAsync</c>, <c>PatchAsync</c>, <c>DeleteAsync</c>.
	/// <para>
	/// Always <c>async</c>. Calls <c>RequestAdapter.SendAsync</c> (or
	/// <c>SendPrimitiveAsync</c>) with error mappings and returns the
	/// deserialized response model (or <see langword="void"/>).
	/// </para>
	/// </summary>
	RequestExecutor = 1,

	/// <summary>
	/// A request-information builder method such as
	/// <c>ToGetRequestInformation</c>, <c>ToPostRequestInformation</c>.
	/// <para>
	/// Builds and returns a <c>RequestInformation</c> instance with the
	/// correct HTTP method, URL template, accept headers, and optional
	/// request body content.
	/// </para>
	/// </summary>
	RequestGenerator = 2,

	/// <summary>
	/// The <c>Serialize(ISerializationWriter)</c> method on model classes.
	/// <para>
	/// Writes each serializable property to the writer using the correct
	/// <c>WriteXxxValue</c> method determined by the property type.
	/// </para>
	/// </summary>
	Serializer = 3,

	/// <summary>
	/// The <c>GetFieldDeserializers()</c> method on model classes.
	/// <para>
	/// Returns a <c>Dictionary&lt;string, Action&lt;IParseNode&gt;&gt;</c>
	/// mapping each JSON field name to a setter lambda that reads the value
	/// with the correct <c>GetXxxValue</c> method.
	/// </para>
	/// </summary>
	Deserializer = 4,

	/// <summary>
	/// A static factory / discriminator method
	/// (<c>CreateFromDiscriminatorValue</c>).
	/// <para>
	/// Used for polymorphic deserialization. Reads the discriminator
	/// property from the parse node and returns the appropriate concrete
	/// type instance via a switch expression.
	/// </para>
	/// </summary>
	Factory = 5,

	/// <summary>
	/// A property getter method (used when properties are emitted as
	/// explicit getter/setter pairs rather than auto-properties).
	/// </summary>
	Getter = 6,

	/// <summary>
	/// A property setter method (used when properties are emitted as
	/// explicit getter/setter pairs rather than auto-properties).
	/// </summary>
	Setter = 7,

	/// <summary>
	/// An indexer accessor method for parameterized path segments
	/// (e.g., <c>ByPetId(string petId)</c>).
	/// <para>
	/// Creates a child item request builder with the path parameter
	/// added to the URL template parameters dictionary.
	/// </para>
	/// </summary>
	IndexerAccessor = 8,

	/// <summary>
	/// The <c>WithUrl</c> method that creates a new request builder instance
	/// from a raw URL string, bypassing URL template expansion.
	/// </summary>
	WithUrl = 9,
}

// ======================================================================
// CodeMethod
// ======================================================================

/// <summary>
/// Represents a method declaration in the CodeDOM tree.
/// <para>
/// A <see cref="CodeMethod"/> models any callable member — HTTP executor,
/// request-information builder, serializer, deserializer, constructor,
/// static factory, indexer accessor, or <c>WithUrl</c> helper —
/// distinguished by <see cref="Kind"/>. It owns an ordered list of
/// <see cref="CodeParameter"/> children and a <see cref="ReturnType"/>
/// reference.
/// </para>
/// <para>
/// The method is mutable during CodeDOM construction and refinement; treat
/// it as read-only during the emission phase. All child parameter additions
/// automatically wire up <see cref="CodeElement.Parent"/>.
/// </para>
/// </summary>
internal class CodeMethod : CodeElement
{
	/// <summary>
	/// Initializes a new <see cref="CodeMethod"/> with the specified name.
	/// </summary>
	/// <param name="name">
	/// The method name (e.g., <c>"GetAsync"</c>). Must not be <see langword="null"/>.
	/// </param>
	public CodeMethod(string name)
		: base(name)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="CodeMethod"/> with the specified name and
	/// kind.
	/// </summary>
	/// <param name="name">
	/// The method name. Must not be <see langword="null"/>.
	/// </param>
	/// <param name="kind">
	/// The semantic role of this method.
	/// </param>
	public CodeMethod(string name, CodeMethodKind kind)
		: base(name)
	{
		Kind = kind;
	}

	/// <summary>
	/// Initializes a new <see cref="CodeMethod"/> with the specified name,
	/// kind, and return type.
	/// </summary>
	/// <param name="name">
	/// The method name. Must not be <see langword="null"/>.
	/// </param>
	/// <param name="kind">
	/// The semantic role of this method.
	/// </param>
	/// <param name="returnType">
	/// The return type of this method. May be <see langword="null"/> for
	/// <c>void</c> methods.
	/// </param>
	public CodeMethod(string name, CodeMethodKind kind, CodeTypeBase returnType)
		: base(name)
	{
		Kind = kind;
		ReturnType = returnType;
	}

	// ------------------------------------------------------------------
	// Classification
	// ------------------------------------------------------------------

	/// <summary>
	/// The semantic role of this method (constructor, HTTP executor,
	/// request-information builder, serializer, etc.).
	/// </summary>
	public CodeMethodKind Kind { get; set; }

	/// <summary>
	/// Access modifier for the generated method declaration.
	/// Defaults to <see cref="AccessModifier.Public"/>.
	/// </summary>
	public AccessModifier Access { get; set; } = AccessModifier.Public;

	// ------------------------------------------------------------------
	// Signature modifiers
	// ------------------------------------------------------------------

	/// <summary>
	/// Whether this method is <c>async</c>.
	/// <para>
	/// Typically <see langword="true"/> for
	/// <see cref="CodeMethodKind.RequestExecutor"/> methods, and
	/// <see langword="false"/> for all other kinds.
	/// </para>
	/// </summary>
	public bool IsAsync { get; set; }

	/// <summary>
	/// Whether this method is <c>static</c>.
	/// <para>
	/// Typically <see langword="true"/> only for
	/// <see cref="CodeMethodKind.Factory"/> methods
	/// (<c>CreateFromDiscriminatorValue</c>).
	/// </para>
	/// </summary>
	public bool IsStatic { get; set; }

	/// <summary>
	/// Whether this method overrides a base class method.
	/// <para>
	/// When <see langword="true"/>, the emitter prepends the <c>new</c> or
	/// <c>override</c> keyword as appropriate. For example, a model that
	/// inherits from another model may override
	/// <c>GetFieldDeserializers</c> and <c>Serialize</c>.
	/// </para>
	/// </summary>
	public bool IsOverride { get; set; }

	// ------------------------------------------------------------------
	// Return type
	// ------------------------------------------------------------------

	/// <summary>
	/// The return type of this method, or <see langword="null"/> when the
	/// method returns <c>void</c>.
	/// <para>
	/// For <see cref="CodeMethodKind.RequestExecutor"/> methods this is the
	/// deserialized response model type (e.g., <c>Pet</c>). For
	/// <see cref="CodeMethodKind.RequestGenerator"/> methods this is
	/// <c>RequestInformation</c>. For <see cref="CodeMethodKind.Deserializer"/>
	/// methods this is
	/// <c>IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;</c>.
	/// </para>
	/// <para>
	/// May be <see langword="null"/> during early CodeDOM construction; must
	/// be resolved before the emission phase.
	/// </para>
	/// </summary>
	public CodeTypeBase ReturnType { get; set; }

	// ------------------------------------------------------------------
	// Parameters
	// ------------------------------------------------------------------

	/// <summary>
	/// The parameters of this method, in declaration order.
	/// <para>
	/// The order in this collection determines the order in the emitted C#
	/// method signature.
	/// </para>
	/// </summary>
	public IReadOnlyList<CodeParameter> Parameters => _parameters;

	private readonly List<CodeParameter> _parameters = new List<CodeParameter>();

	/// <summary>
	/// Adds a <see cref="CodeParameter"/> to this method and sets its
	/// <see cref="CodeElement.Parent"/>.
	/// </summary>
	/// <param name="parameter">
	/// The parameter to add. Must not be <see langword="null"/>.
	/// </param>
	/// <returns>The added <paramref name="parameter"/> for fluent chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="parameter"/> is <see langword="null"/>.
	/// </exception>
	public CodeParameter AddParameter(CodeParameter parameter) => AddChild(_parameters, parameter);

	/// <summary>
	/// Finds a parameter by <paramref name="name"/> (case-sensitive).
	/// </summary>
	/// <param name="name">The parameter name to search for.</param>
	/// <returns>
	/// The matching <see cref="CodeParameter"/>, or <see langword="null"/>
	/// if not found.
	/// </returns>
	public CodeParameter FindParameter(string name)
	{
		for (int i = 0; i < _parameters.Count; i++)
		{
			if (string.Equals(_parameters[i].Name, name, StringComparison.Ordinal))
			{
				return _parameters[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Returns all parameters matching the specified <paramref name="kind"/>.
	/// </summary>
	/// <param name="kind">The parameter kind to filter by.</param>
	/// <returns>An enumerable of matching parameters.</returns>
	public IEnumerable<CodeParameter> ParametersOfKind(CodeParameterKind kind)
	{
		for (int i = 0; i < _parameters.Count; i++)
		{
			if (_parameters[i].Kind == kind)
			{
				yield return _parameters[i];
			}
		}
	}

	// ------------------------------------------------------------------
	// HTTP-specific metadata (RequestExecutor / RequestGenerator)
	// ------------------------------------------------------------------

	/// <summary>
	/// The HTTP method for <see cref="CodeMethodKind.RequestExecutor"/> and
	/// <see cref="CodeMethodKind.RequestGenerator"/> methods (e.g.,
	/// <c>"GET"</c>, <c>"POST"</c>, <c>"PATCH"</c>, <c>"DELETE"</c>).
	/// <para>
	/// <see langword="null"/> for non-HTTP method kinds.
	/// </para>
	/// </summary>
	public string HttpMethod { get; set; }

	/// <summary>
	/// The <c>Accept</c> header media types that this executor/generator
	/// method declares (e.g., <c>"application/json"</c>).
	/// <para>
	/// Used to populate <c>requestInfo.Headers.TryAdd("Accept", ...)</c>
	/// in the emitted request-information builder.
	/// </para>
	/// </summary>
	public IReadOnlyList<string> AcceptedResponseTypes => _acceptedResponseTypes;

	private readonly List<string> _acceptedResponseTypes = new List<string>();

	/// <summary>
	/// Adds an accepted response media type.
	/// </summary>
	/// <param name="mediaType">
	/// The media type string (e.g., <c>"application/json"</c>).
	/// Must not be <see langword="null"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="mediaType"/> is <see langword="null"/>.
	/// </exception>
	public void AddAcceptedResponseType(string mediaType)
	{
		if (mediaType is null)
		{
			throw new ArgumentNullException(nameof(mediaType));
		}

		_acceptedResponseTypes.Add(mediaType);
	}

	/// <summary>
	/// The request body content type for <see cref="CodeMethodKind.RequestExecutor"/>
	/// and <see cref="CodeMethodKind.RequestGenerator"/> methods that send a body
	/// (e.g., <c>"application/json"</c>).
	/// <para>
	/// <see langword="null"/> when the method does not send a request body
	/// (e.g., <c>GET</c>, <c>DELETE</c>).
	/// </para>
	/// </summary>
	public string RequestBodyContentType { get; set; }

	// ------------------------------------------------------------------
	// Error mappings (RequestExecutor)
	// ------------------------------------------------------------------

	/// <summary>
	/// Maps HTTP error status code ranges to their corresponding error model
	/// types for <see cref="CodeMethodKind.RequestExecutor"/> methods.
	/// <para>
	/// Keys are status-code range strings (e.g., <c>"4XX"</c>, <c>"5XX"</c>,
	/// or specific codes like <c>"404"</c>). Values are <see cref="CodeType"/>
	/// references pointing to error model classes that extend
	/// <c>ApiException</c>.
	/// </para>
	/// <para>
	/// Used to emit the <c>errorMapping</c> dictionary in executor method
	/// bodies:
	/// <code>
	/// var errorMapping = new Dictionary&lt;string, ParsableFactory&lt;IParsable&gt;&gt;
	/// {
	///     { "4XX", global::Ns.Error4XX.CreateFromDiscriminatorValue },
	///     { "5XX", global::Ns.Error5XX.CreateFromDiscriminatorValue },
	/// };
	/// </code>
	/// </para>
	/// </summary>
	public IReadOnlyDictionary<string, CodeType> ErrorMappings => _errorMappings;

	private readonly Dictionary<string, CodeType> _errorMappings =
		new Dictionary<string, CodeType>(StringComparer.Ordinal);

	/// <summary>
	/// Registers an error status-code → type mapping for this executor method.
	/// </summary>
	/// <param name="statusCodeRange">
	/// The status-code range (e.g., <c>"4XX"</c>). Must not be
	/// <see langword="null"/>.
	/// </param>
	/// <param name="errorType">
	/// The error model type. Must not be <see langword="null"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="statusCodeRange"/> or <paramref name="errorType"/>
	/// is <see langword="null"/>.
	/// </exception>
	public void AddErrorMapping(string statusCodeRange, CodeType errorType)
	{
		if (statusCodeRange is null)
		{
			throw new ArgumentNullException(nameof(statusCodeRange));
		}

		if (errorType is null)
		{
			throw new ArgumentNullException(nameof(errorType));
		}

		_errorMappings[statusCodeRange] = errorType;
	}

	// ------------------------------------------------------------------
	// Cross-references
	// ------------------------------------------------------------------

	/// <summary>
	/// The base URL from the OpenAPI spec's first server entry.
	/// <para>
	/// Set only on root client <see cref="CodeMethodKind.Constructor"/> methods.
	/// Used by the <c>ConstructorEmitter</c> to emit the
	/// <c>requestAdapter.BaseUrl = "..."</c> fallback assignment inside
	/// the root client constructor body.
	/// </para>
	/// <para>
	/// <see langword="null"/> for all non-root-client constructors and all
	/// non-constructor methods.
	/// </para>
	/// </summary>
	public string BaseUrl { get; set; }

	/// <summary>
	/// For methods that override a base-class method (e.g., a derived
	/// model's <c>Serialize</c> overriding the base model's), this
	/// references the original base-class method.
	/// <para>
	/// <see langword="null"/> when this method does not override anything.
	/// Used by the emitter to determine whether to call <c>base.Serialize</c>
	/// or <c>base.GetFieldDeserializers()</c>.
	/// </para>
	/// </summary>
	public CodeMethod BaseMethod { get; set; }

	/// <summary>
	/// For <see cref="CodeMethodKind.Getter"/> and
	/// <see cref="CodeMethodKind.Setter"/> methods, the property this method
	/// accesses. <see langword="null"/> for all other kinds.
	/// </summary>
	public CodeProperty AccessedProperty { get; set; }

	// ------------------------------------------------------------------
	// Object overrides
	// ------------------------------------------------------------------

	/// <inheritdoc/>
	public override string ToString()
		=> $"{Kind} {Name}({string.Join(", ", _parameters.Select(p => p.Name))})"
		   + (ReturnType != null ? $" : {ReturnType.Name}" : " : void");
}
