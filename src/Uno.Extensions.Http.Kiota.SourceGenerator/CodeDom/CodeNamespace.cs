#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;

/// <summary>
/// Represents a namespace in the CodeDOM tree. Acts as the root container for
/// <see cref="CodeClass"/> declarations, <see cref="CodeEnum"/> declarations,
/// and child <see cref="CodeNamespace"/> nodes.
/// <para>
/// The namespace hierarchy mirrors the target C# namespace structure.
/// Typical layout: root namespace → client namespace (request builders) →
/// models namespace (model/enum definitions). The root namespace is the only
/// element whose <see cref="CodeElement.Parent"/> is <see langword="null"/>.
/// </para>
/// <para>
/// Use <see cref="AddNamespace(string)"/> to create an immediate child namespace
/// that is automatically wired into the tree.
/// Use <see cref="GetOrAddNamespace(string)"/> to resolve (or create) a deeply
/// nested namespace from a dot-separated qualified name.
/// </para>
/// </summary>
internal sealed class CodeNamespace : CodeElement
{
	/// <summary>
	/// Initializes a new <see cref="CodeNamespace"/> with the specified name.
	/// </summary>
	/// <param name="name">
	/// The namespace segment name (e.g., <c>"Models"</c> for a child, or
	/// <c>"MyApp.PetStore"</c> for a root). Must not be <see langword="null"/>.
	/// </param>
	public CodeNamespace(string name)
		: base(name)
	{
	}

	// ------------------------------------------------------------------
	// Child collections
	// ------------------------------------------------------------------

	/// <summary>
	/// The classes declared directly in this namespace.
	/// </summary>
	public IReadOnlyList<CodeClass> Classes => _classes;

	/// <summary>
	/// The enum types declared directly in this namespace.
	/// </summary>
	public IReadOnlyList<CodeEnum> Enums => _enums;

	/// <summary>
	/// The immediate child namespaces.
	/// </summary>
	public IReadOnlyList<CodeNamespace> Namespaces => _namespaces;

	private readonly List<CodeClass> _classes = new List<CodeClass>();
	private readonly List<CodeEnum> _enums = new List<CodeEnum>();
	private readonly List<CodeNamespace> _namespaces = new List<CodeNamespace>();

	// ------------------------------------------------------------------
	// Mutators — used during CodeDOM construction and refinement
	// ------------------------------------------------------------------

	/// <summary>
	/// Adds a <see cref="CodeClass"/> to this namespace and sets its
	/// <see cref="CodeElement.Parent"/> to this namespace.
	/// </summary>
	/// <param name="codeClass">The class to add. Must not be <see langword="null"/>.</param>
	/// <returns>The added <paramref name="codeClass"/>, for fluent chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="codeClass"/> is <see langword="null"/>.
	/// </exception>
	public CodeClass AddClass(CodeClass codeClass) => AddChild(_classes, codeClass);

	/// <summary>
	/// Adds a <see cref="CodeEnum"/> to this namespace and sets its
	/// <see cref="CodeElement.Parent"/> to this namespace.
	/// </summary>
	/// <param name="codeEnum">The enum to add. Must not be <see langword="null"/>.</param>
	/// <returns>The added <paramref name="codeEnum"/>, for fluent chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="codeEnum"/> is <see langword="null"/>.
	/// </exception>
	public CodeEnum AddEnum(CodeEnum codeEnum) => AddChild(_enums, codeEnum);

	/// <summary>
	/// Adds an immediate child <see cref="CodeNamespace"/> and sets its
	/// <see cref="CodeElement.Parent"/> to this namespace.
	/// </summary>
	/// <param name="childNamespace">The child namespace to add. Must not be <see langword="null"/>.</param>
	/// <returns>The added <paramref name="childNamespace"/>, for fluent chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="childNamespace"/> is <see langword="null"/>.
	/// </exception>
	public CodeNamespace AddNamespace(CodeNamespace childNamespace) => AddChild(_namespaces, childNamespace);

	/// <summary>
	/// Creates and adds a new immediate child namespace with the given
	/// <paramref name="name"/>.
	/// </summary>
	/// <param name="name">
	/// The segment name for the child namespace (e.g., <c>"Models"</c>).
	/// Must not be <see langword="null"/>.
	/// </param>
	/// <returns>The newly created child <see cref="CodeNamespace"/>.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="name"/> is <see langword="null"/>.
	/// </exception>
	public CodeNamespace AddNamespace(string name)
	{
		var child = new CodeNamespace(name);
		return AddChild(_namespaces, child);
	}

	/// <summary>
	/// Resolves a namespace from a dot-separated qualified name relative to this
	/// namespace, creating missing segments along the way.
	/// <para>
	/// For example, calling <c>root.GetOrAddNamespace("Models.Pets")</c> will
	/// find or create a <c>Models</c> child, then find or create a <c>Pets</c>
	/// child within it.
	/// </para>
	/// </summary>
	/// <param name="qualifiedName">
	/// A dot-separated relative namespace path (e.g., <c>"Models.Pets"</c>).
	/// Must not be <see langword="null"/> or empty.
	/// </param>
	/// <returns>The resolved (or newly created) <see cref="CodeNamespace"/>.</returns>
	/// <exception cref="ArgumentException">
	/// <paramref name="qualifiedName"/> is <see langword="null"/> or empty.
	/// </exception>
	public CodeNamespace GetOrAddNamespace(string qualifiedName)
	{
		if (string.IsNullOrEmpty(qualifiedName))
		{
			throw new ArgumentException("Qualified namespace name must not be null or empty.", nameof(qualifiedName));
		}

		// Split on '.' and resolve each segment in turn.
		var segments = qualifiedName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
		var current = this;

		for (int i = 0; i < segments.Length; i++)
		{
			var segment = segments[i];
			var existing = current.FindChildNamespace(segment);
			if (existing != null)
			{
				current = existing;
			}
			else
			{
				current = current.AddNamespace(segment);
			}
		}

		return current;
	}

	// ------------------------------------------------------------------
	// Query helpers
	// ------------------------------------------------------------------

	/// <summary>
	/// Finds an immediate child namespace by <paramref name="name"/>
	/// (case-sensitive comparison).
	/// </summary>
	/// <param name="name">The segment name to search for.</param>
	/// <returns>
	/// The matching child namespace, or <see langword="null"/> if not found.
	/// </returns>
	public CodeNamespace FindChildNamespace(string name)
	{
		for (int i = 0; i < _namespaces.Count; i++)
		{
			if (string.Equals(_namespaces[i].Name, name, StringComparison.Ordinal))
			{
				return _namespaces[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Finds a class declared directly in this namespace by <paramref name="name"/>
	/// (case-sensitive comparison).
	/// </summary>
	/// <param name="name">The class name to search for.</param>
	/// <returns>
	/// The matching <see cref="CodeClass"/>, or <see langword="null"/> if not found.
	/// </returns>
	public CodeClass FindClass(string name)
	{
		for (int i = 0; i < _classes.Count; i++)
		{
			if (string.Equals(_classes[i].Name, name, StringComparison.Ordinal))
			{
				return _classes[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Finds an enum declared directly in this namespace by <paramref name="name"/>
	/// (case-sensitive comparison).
	/// </summary>
	/// <param name="name">The enum name to search for.</param>
	/// <returns>
	/// The matching <see cref="CodeEnum"/>, or <see langword="null"/> if not found.
	/// </returns>
	public CodeEnum FindEnum(string name)
	{
		for (int i = 0; i < _enums.Count; i++)
		{
			if (string.Equals(_enums[i].Name, name, StringComparison.Ordinal))
			{
				return _enums[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Recursively enumerates all <see cref="CodeClass"/> declarations in this
	/// namespace and all descendant namespaces, depth-first.
	/// </summary>
	/// <returns>An enumerable of every class in the subtree.</returns>
	public IEnumerable<CodeClass> GetAllClasses()
	{
		foreach (var cls in _classes)
		{
			yield return cls;
		}

		foreach (var child in _namespaces)
		{
			foreach (var cls in child.GetAllClasses())
			{
				yield return cls;
			}
		}
	}

	/// <summary>
	/// Recursively enumerates all <see cref="CodeEnum"/> declarations in this
	/// namespace and all descendant namespaces, depth-first.
	/// </summary>
	/// <returns>An enumerable of every enum in the subtree.</returns>
	public IEnumerable<CodeEnum> GetAllEnums()
	{
		foreach (var en in _enums)
		{
			yield return en;
		}

		foreach (var child in _namespaces)
		{
			foreach (var en in child.GetAllEnums())
			{
				yield return en;
			}
		}
	}
}
