#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;

// ======================================================================
// CodeClassKind enumeration
// ======================================================================

/// <summary>
/// Classifies the semantic role of a <see cref="CodeClass"/> in the Kiota
/// code-generation model.
/// </summary>
internal enum CodeClassKind
{
	/// <summary>
	/// A request builder class representing an API path segment.
	/// Extends <c>BaseRequestBuilder</c> and contains navigation properties,
	/// indexers, HTTP executor methods, and request-information builders.
	/// </summary>
	RequestBuilder = 0,

	/// <summary>
	/// A data model class representing an OpenAPI schema object.
	/// Implements <c>IParsable</c> and optionally <c>IAdditionalDataHolder</c>
	/// and <c>IBackedModel</c>. Contains serializable properties, a factory
	/// method, <c>Serialize</c>, and <c>GetFieldDeserializers</c>.
	/// </summary>
	Model = 1,

	/// <summary>
	/// A query-parameters POCO nested inside a request builder.
	/// Contains one property per supported query parameter, decorated with
	/// <c>[QueryParameter]</c> attributes when the serialized name differs
	/// from the C# name.
	/// </summary>
	QueryParameters = 2,

	/// <summary>
	/// A deprecated request-configuration class nested inside a request
	/// builder. Maintained for backward compatibility only; newer Kiota
	/// versions use the generic <c>RequestConfiguration&lt;TQueryParams&gt;</c>
	/// from the abstractions library instead.
	/// </summary>
	RequestConfiguration = 3,
}

// ======================================================================
// CodeClass
// ======================================================================

/// <summary>
/// Represents a class declaration in the CodeDOM tree.
/// <para>
/// A <see cref="CodeClass"/> can model a request builder, a data model, a
/// query-parameters POCO, or a request-configuration class, distinguished
/// by <see cref="Kind"/>. It owns ordered collections of
/// <see cref="CodeProperty"/>, <see cref="CodeMethod"/>, and nested
/// <see cref="CodeClass"/> children, plus optional base-class and
/// implemented-interface references.
/// </para>
/// <para>
/// The class is mutable during CodeDOM construction and refinement; treat
/// it as read-only during the emission phase. All child additions
/// automatically wire up <see cref="CodeElement.Parent"/>.
/// </para>
/// </summary>
internal class CodeClass : CodeElement
{
	/// <summary>
	/// Initializes a new <see cref="CodeClass"/> with the specified name and
	/// a default <see cref="Kind"/> of <see cref="CodeClassKind.Model"/>.
	/// </summary>
	/// <param name="name">
	/// The class name (e.g., <c>"PetStoreClient"</c>). Must not be <see langword="null"/>.
	/// </param>
	public CodeClass(string name)
		: base(name)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="CodeClass"/> with the specified name and kind.
	/// </summary>
	/// <param name="name">
	/// The class name. Must not be <see langword="null"/>.
	/// </param>
	/// <param name="kind">
	/// The semantic role of this class.
	/// </param>
	public CodeClass(string name, CodeClassKind kind)
		: base(name)
	{
		Kind = kind;
	}

	// ------------------------------------------------------------------
	// Classification
	// ------------------------------------------------------------------

	/// <summary>
	/// The semantic role of this class (request builder, model, query
	/// parameters, or request configuration).
	/// </summary>
	public CodeClassKind Kind { get; set; } = CodeClassKind.Model;

	/// <summary>
	/// Access modifier for the generated class declaration.
	/// Defaults to <see cref="AccessModifier.Public"/>.
	/// </summary>
	public AccessModifier Access { get; set; } = AccessModifier.Public;

	// ------------------------------------------------------------------
	// Inheritance & interfaces
	// ------------------------------------------------------------------

	/// <summary>
	/// The base class this class extends, or <see langword="null"/> when the
	/// class does not inherit from a user-defined type.
	/// <para>
	/// For <see cref="CodeClassKind.RequestBuilder"/> classes this is
	/// typically the <c>BaseRequestBuilder</c> type.
	/// For <see cref="CodeClassKind.Model"/> classes this is the type
	/// resolved from an <c>allOf</c> <c>$ref</c>.
	/// </para>
	/// </summary>
	public CodeType BaseClass { get; set; }

	/// <summary>
	/// Interfaces implemented by this class (e.g., <c>IParsable</c>,
	/// <c>IAdditionalDataHolder</c>, <c>IBackedModel</c>).
	/// </summary>
	public IReadOnlyList<CodeType> Interfaces => _interfaces;

	private readonly List<CodeType> _interfaces = new List<CodeType>();

	/// <summary>
	/// Adds an implemented interface to this class.
	/// </summary>
	/// <param name="interfaceType">
	/// The interface type reference. Must not be <see langword="null"/>.
	/// </param>
	/// <returns>The added <paramref name="interfaceType"/> for fluent chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="interfaceType"/> is <see langword="null"/>.
	/// </exception>
	public CodeType AddInterface(CodeType interfaceType)
	{
		if (interfaceType is null)
		{
			throw new ArgumentNullException(nameof(interfaceType));
		}

		_interfaces.Add(interfaceType);
		return interfaceType;
	}

	// ------------------------------------------------------------------
	// Child collections — Properties
	// ------------------------------------------------------------------

	/// <summary>
	/// The properties declared in this class, in declaration order.
	/// </summary>
	public IReadOnlyList<CodeProperty> Properties => _properties;

	private readonly List<CodeProperty> _properties = new List<CodeProperty>();

	/// <summary>
	/// Adds a <see cref="CodeProperty"/> to this class and sets its
	/// <see cref="CodeElement.Parent"/>.
	/// </summary>
	/// <param name="property">The property to add. Must not be <see langword="null"/>.</param>
	/// <returns>The added <paramref name="property"/> for fluent chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="property"/> is <see langword="null"/>.
	/// </exception>
	public CodeProperty AddProperty(CodeProperty property) => AddChild(_properties, property);

	/// <summary>
	/// Finds a property declared in this class by <paramref name="name"/>
	/// (case-sensitive comparison).
	/// </summary>
	/// <param name="name">The property name to search for.</param>
	/// <returns>
	/// The matching <see cref="CodeProperty"/>, or <see langword="null"/> if
	/// not found.
	/// </returns>
	public CodeProperty FindProperty(string name)
	{
		for (int i = 0; i < _properties.Count; i++)
		{
			if (string.Equals(_properties[i].Name, name, StringComparison.Ordinal))
			{
				return _properties[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Returns all properties matching the specified <paramref name="kind"/>.
	/// </summary>
	/// <param name="kind">The property kind to filter by.</param>
	/// <returns>An enumerable of matching properties.</returns>
	public IEnumerable<CodeProperty> PropertiesOfKind(CodePropertyKind kind)
	{
		for (int i = 0; i < _properties.Count; i++)
		{
			if (_properties[i].Kind == kind)
			{
				yield return _properties[i];
			}
		}
	}

	// ------------------------------------------------------------------
	// Child collections — Methods
	// ------------------------------------------------------------------

	/// <summary>
	/// The methods declared in this class, in declaration order.
	/// </summary>
	public IReadOnlyList<CodeMethod> Methods => _methods;

	private readonly List<CodeMethod> _methods = new List<CodeMethod>();

	/// <summary>
	/// Adds a <see cref="CodeMethod"/> to this class and sets its
	/// <see cref="CodeElement.Parent"/>.
	/// </summary>
	/// <param name="method">The method to add. Must not be <see langword="null"/>.</param>
	/// <returns>The added <paramref name="method"/> for fluent chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="method"/> is <see langword="null"/>.
	/// </exception>
	public CodeMethod AddMethod(CodeMethod method) => AddChild(_methods, method);

	/// <summary>
	/// Finds a method declared in this class by <paramref name="name"/>
	/// (case-sensitive comparison).
	/// </summary>
	/// <param name="name">The method name to search for.</param>
	/// <returns>
	/// The matching <see cref="CodeMethod"/>, or <see langword="null"/> if
	/// not found.
	/// </returns>
	public CodeMethod FindMethod(string name)
	{
		for (int i = 0; i < _methods.Count; i++)
		{
			if (string.Equals(_methods[i].Name, name, StringComparison.Ordinal))
			{
				return _methods[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Returns all methods matching the specified <paramref name="kind"/>.
	/// </summary>
	/// <param name="kind">The method kind to filter by.</param>
	/// <returns>An enumerable of matching methods.</returns>
	public IEnumerable<CodeMethod> MethodsOfKind(CodeMethodKind kind)
	{
		for (int i = 0; i < _methods.Count; i++)
		{
			if (_methods[i].Kind == kind)
			{
				yield return _methods[i];
			}
		}
	}

	/// <summary>
	/// Removes all methods matching the specified <paramref name="kind"/>.
	/// </summary>
	/// <param name="kind">The method kind to remove.</param>
	public void RemoveMethodsOfKind(CodeMethodKind kind)
	{
		for (int i = _methods.Count - 1; i >= 0; i--)
		{
			if (_methods[i].Kind == kind)
			{
				_methods.RemoveAt(i);
			}
		}
	}

	// ------------------------------------------------------------------
	// Child collections — Inner classes
	// ------------------------------------------------------------------

	/// <summary>
	/// Nested class declarations (e.g., <c>QueryParameters</c> and
	/// <c>RequestConfiguration</c> inside a request builder).
	/// </summary>
	public IReadOnlyList<CodeClass> InnerClasses => _innerClasses;

	private readonly List<CodeClass> _innerClasses = new List<CodeClass>();

	/// <summary>
	/// Adds a nested <see cref="CodeClass"/> to this class and sets its
	/// <see cref="CodeElement.Parent"/>.
	/// </summary>
	/// <param name="innerClass">The inner class to add. Must not be <see langword="null"/>.</param>
	/// <returns>The added <paramref name="innerClass"/> for fluent chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="innerClass"/> is <see langword="null"/>.
	/// </exception>
	public CodeClass AddInnerClass(CodeClass innerClass) => AddChild(_innerClasses, innerClass);

	/// <summary>
	/// Finds a nested class by <paramref name="name"/> (case-sensitive comparison).
	/// </summary>
	/// <param name="name">The inner class name to search for.</param>
	/// <returns>
	/// The matching inner <see cref="CodeClass"/>, or <see langword="null"/> if
	/// not found.
	/// </returns>
	public CodeClass FindInnerClass(string name)
	{
		for (int i = 0; i < _innerClasses.Count; i++)
		{
			if (string.Equals(_innerClasses[i].Name, name, StringComparison.Ordinal))
			{
				return _innerClasses[i];
			}
		}

		return null;
	}

	// ------------------------------------------------------------------
	// Child collections — Indexers
	// ------------------------------------------------------------------

	/// <summary>
	/// Indexers declared in this class, representing parameterized path
	/// segments (e.g., <c>/pets/{petId}</c>).
	/// <para>
	/// Typically present only on <see cref="CodeClassKind.RequestBuilder"/>
	/// classes that contain collection endpoints.
	/// </para>
	/// </summary>
	public IReadOnlyList<CodeIndexer> Indexers => _indexers;

	private readonly List<CodeIndexer> _indexers = new List<CodeIndexer>();

	/// <summary>
	/// Adds a <see cref="CodeIndexer"/> to this class and sets its
	/// <see cref="CodeElement.Parent"/>.
	/// </summary>
	/// <param name="indexer">The indexer to add. Must not be <see langword="null"/>.</param>
	/// <returns>The added <paramref name="indexer"/> for fluent chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="indexer"/> is <see langword="null"/>.
	/// </exception>
	public CodeIndexer AddIndexer(CodeIndexer indexer) => AddChild(_indexers, indexer);

	/// <summary>
	/// Finds an indexer declared in this class by <paramref name="name"/>
	/// (case-sensitive comparison).
	/// </summary>
	/// <param name="name">The indexer name to search for.</param>
	/// <returns>
	/// The matching <see cref="CodeIndexer"/>, or <see langword="null"/> if
	/// not found.
	/// </returns>
	public CodeIndexer FindIndexer(string name)
	{
		for (int i = 0; i < _indexers.Count; i++)
		{
			if (string.Equals(_indexers[i].Name, name, StringComparison.Ordinal))
			{
				return _indexers[i];
			}
		}

		return null;
	}

	// ------------------------------------------------------------------
	// Discriminator support
	// ------------------------------------------------------------------

	/// <summary>
	/// The name of the JSON property used as a discriminator for polymorphic
	/// deserialization (e.g., <c>"@odata.type"</c>). <see langword="null"/>
	/// when the class has no discriminator.
	/// <para>
	/// Set by <c>KiotaCodeDomBuilder</c> when the OpenAPI schema specifies a
	/// <c>discriminator</c> with a <c>propertyName</c>.
	/// </para>
	/// </summary>
	public string DiscriminatorPropertyName { get; set; }

	/// <summary>
	/// Maps discriminator values to the concrete <see cref="CodeType"/>
	/// references that should be instantiated for that value.
	/// <para>
	/// For example: <c>{ "#microsoft.graph.user" → CodeType(User) }</c>.
	/// Used by the <c>FactoryMethodEmitter</c> to generate the
	/// <c>CreateFromDiscriminatorValue</c> switch statement.
	/// </para>
	/// </summary>
	public IReadOnlyDictionary<string, CodeType> DiscriminatorMappings => _discriminatorMappings;

	private readonly Dictionary<string, CodeType> _discriminatorMappings =
		new Dictionary<string, CodeType>(StringComparer.Ordinal);

	/// <summary>
	/// Registers a discriminator value → type mapping for polymorphic
	/// deserialization.
	/// </summary>
	/// <param name="discriminatorValue">
	/// The discriminator string value (e.g., <c>"#microsoft.graph.user"</c>).
	/// Must not be <see langword="null"/>.
	/// </param>
	/// <param name="type">
	/// The target type to instantiate for that discriminator value.
	/// Must not be <see langword="null"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="discriminatorValue"/> or <paramref name="type"/> is
	/// <see langword="null"/>.
	/// </exception>
	public void AddDiscriminatorMapping(string discriminatorValue, CodeType type)
	{
		if (discriminatorValue is null)
		{
			throw new ArgumentNullException(nameof(discriminatorValue));
		}

		if (type is null)
		{
			throw new ArgumentNullException(nameof(type));
		}

		_discriminatorMappings[discriminatorValue] = type;
	}

	// ------------------------------------------------------------------
	// Query helpers
	// ------------------------------------------------------------------

	/// <summary>
	/// Recursively enumerates all <see cref="CodeClass"/> declarations nested
	/// inside this class (inner classes and their descendants), depth-first.
	/// Does <em>not</em> include <c>this</c>.
	/// </summary>
	/// <returns>An enumerable of every inner class in the subtree.</returns>
	public IEnumerable<CodeClass> GetAllInnerClasses()
	{
		foreach (var inner in _innerClasses)
		{
			yield return inner;

			foreach (var nested in inner.GetAllInnerClasses())
			{
				yield return nested;
			}
		}
	}

	/// <summary>
	/// Returns <see langword="true"/> when this class is a model that
	/// represents an error response (i.e., it should extend
	/// <c>ApiException</c> instead of a plain base class).
	/// </summary>
	public bool IsErrorDefinition { get; set; }

	/// <summary>
	/// Returns <see langword="true"/> when this class is a composed-type
	/// wrapper representing a union type (<c>oneOf</c>).  When
	/// <see langword="false"/> the wrapper is an intersection (<c>anyOf</c>).
	/// <para>
	/// Only meaningful when the class implements <c>IComposedTypeWrapper</c>.
	/// </para>
	/// </summary>
	public bool IsUnionType { get; set; }

	/// <inheritdoc/>
	public override string ToString()
		=> $"{Kind} {Name}";
}
