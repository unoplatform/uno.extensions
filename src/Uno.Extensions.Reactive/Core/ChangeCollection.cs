using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A collection that describes which <see cref="MessageAxes"/> has changed between 2 <see cref="Message{T}"/>.
/// </summary>
public record ChangeCollection : IReadOnlyCollection<MessageAxis>
{
	/// <summary>
	/// An empty collection indicating that nothing has changed.
	/// </summary>
	public static ChangeCollection Empty { get; } = new();

	private readonly Dictionary<MessageAxis, IChangeSet?> _values = new();

	/// <inheritdoc />
	public IEnumerator<MessageAxis> GetEnumerator()
		=> _values.Keys.GetEnumerator();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	/// <inheritdoc />
	public int Count => _values.Count;

	/// <summary>
	/// Determines if a given axis has changed.
	/// </summary>
	/// <param name="axis">The axis to validate.</param>
	/// <returns>True if the given axis has been flagged as changed, false otherwise.</returns>
	public bool Contains(MessageAxis axis)
		=> _values.ContainsKey(axis);

	/// <summary>
	/// Determines if a given axis has changed returning details about the change, if any provided.
	/// </summary>
	/// <param name="axis">The axis to validate.</param>
	/// <param name="changeSet">A set of changes that describes the changes that has been made for the given axis, if any provided.</param>
	/// <returns>True if the given axis has been flagged as changed, false otherwise.</returns>
	public bool Contains(MessageAxis axis, out IChangeSet? changeSet)
		=> _values.TryGetValue(axis, out changeSet);

	internal void Set(MessageAxis axis, IChangeSet? changeSet = null)
		=> _values[axis] = changeSet;
}
