#nullable disable

using System;
using System.Collections.Generic;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;

/// <summary>
/// Abstract base class for all CodeDOM nodes in the Kiota source generator pipeline.
/// <para>
/// Each node has a <see cref="Name"/> and an optional <see cref="Parent"/> reference
/// forming a tree that models the generated code structure. Concrete subtypes
/// (CodeNamespace, CodeClass, CodeMethod, CodeProperty, CodeEnum, CodeType,
/// CodeIndexer, CodeParameter) add domain-specific child collections and properties.
/// </para>
/// <para>
/// The CodeDOM tree is built by <c>KiotaCodeDomBuilder</c> from a parsed
/// <c>OpenApiDocument</c>, refined by <c>CSharpRefiner</c>, and then walked by
/// <c>CSharpEmitter</c> to produce source text. The tree is mutable during
/// construction and refinement, then treated as read-only during emission.
/// </para>
/// </summary>
internal abstract class CodeElement
{
	/// <summary>
	/// Initializes a new <see cref="CodeElement"/> with the specified name.
	/// </summary>
	/// <param name="name">
	/// The identifier name of this code element. Must not be <see langword="null"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="name"/> is <see langword="null"/>.
	/// </exception>
	protected CodeElement(string name)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
	}

	// ------------------------------------------------------------------
	// Core properties
	// ------------------------------------------------------------------

	/// <summary>
	/// The identifier name of this code element (e.g., class name, method name,
	/// namespace segment). May be mutated during refinement (e.g., PascalCase
	/// adjustment, reserved-word escaping).
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The parent element in the CodeDOM tree, or <see langword="null"/> for the
	/// root namespace. Set automatically when the element is added to a parent
	/// container via the container's <c>Add*</c> helper methods.
	/// </summary>
	public CodeElement Parent { get; set; }

	/// <summary>
	/// Optional description used for XML documentation comment emission.
	/// Typically sourced from the OpenAPI <c>description</c> field of the
	/// corresponding schema, operation, or parameter.
	/// </summary>
	public string Description { get; set; }

	// ------------------------------------------------------------------
	// Tree navigation helpers
	// ------------------------------------------------------------------

	/// <summary>
	/// Walks up the <see cref="Parent"/> chain to find the nearest ancestor of
	/// type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The CodeElement subtype to search for.</typeparam>
	/// <returns>
	/// The nearest ancestor of type <typeparamref name="T"/>, or
	/// <see langword="null"/> if no matching ancestor exists.
	/// </returns>
	public T FindParentOfType<T>() where T : CodeElement
	{
		var current = Parent;
		while (current != null)
		{
			if (current is T match)
			{
				return match;
			}

			current = current.Parent;
		}

		return null;
	}

	/// <summary>
	/// Builds the fully qualified name by walking up the <see cref="Parent"/>
	/// chain and joining all non-empty <see cref="Name"/> segments.
	/// </summary>
	/// <param name="separator">
	/// The separator to place between segments (default <c>"."</c>).
	/// </param>
	/// <returns>
	/// A dot-separated (or custom-separated) fully qualified name such as
	/// <c>MyApp.PetStore.Models.Pet</c>.
	/// </returns>
	public string GetFullName(string separator = ".")
	{
		var segments = new List<string>();
		var current = this;
		while (current != null)
		{
			if (!string.IsNullOrEmpty(current.Name))
			{
				segments.Add(current.Name);
			}

			current = current.Parent;
		}

		// Segments were collected leaf → root; reverse for root → leaf order.
		segments.Reverse();
		return string.Join(separator, segments);
	}

	// ------------------------------------------------------------------
	// Child-management helper for subtypes
	// ------------------------------------------------------------------

	/// <summary>
	/// Adds <paramref name="child"/> to the given <paramref name="collection"/>
	/// and sets its <see cref="Parent"/> to this element.
	/// <para>
	/// Subtypes should call this from their typed <c>Add*</c> methods to ensure
	/// the parent reference is always set consistently.
	/// </para>
	/// </summary>
	/// <typeparam name="T">The concrete CodeElement subtype.</typeparam>
	/// <param name="collection">The child collection owned by this element.</param>
	/// <param name="child">The child element to add.</param>
	/// <returns>
	/// The added <paramref name="child"/>, for fluent chaining.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="child"/> is <see langword="null"/>.
	/// </exception>
	protected T AddChild<T>(IList<T> collection, T child) where T : CodeElement
	{
		if (child is null)
		{
			throw new ArgumentNullException(nameof(child));
		}

		child.Parent = this;
		collection.Add(child);
		return child;
	}

	// ------------------------------------------------------------------
	// Object overrides
	// ------------------------------------------------------------------

	/// <inheritdoc />
	public override string ToString() => Name;
}

// ======================================================================
// Shared enumerations used across multiple CodeDOM types
// ======================================================================

/// <summary>
/// Access modifier for generated types and members.
/// Maps to the <c>TypeAccessModifier</c> configuration option.
/// </summary>
internal enum AccessModifier
{
	/// <summary>The member is accessible from any assembly (<c>public</c>).</summary>
	Public = 0,

	/// <summary>The member is accessible only within the defining assembly (<c>internal</c>).</summary>
	Internal = 1,
}
