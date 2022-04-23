using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive;

public record ChangeCollection : IReadOnlyCollection<MessageAxis>
{
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

	public bool Contains(MessageAxis axis, out IChangeSet? changeSet)
		=> _values.TryGetValue(axis, out changeSet);

	internal void Add(MessageAxis axis, IChangeSet? changeSet = null)
		=> _values.Add(axis, changeSet);
}
