using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace Uno.Extensions.Reactive.Collections.Facades.Adapters;

internal struct ImmutableListToUntypedList<T> : IList
{
	private readonly IImmutableList<T> _inner;

	public ImmutableListToUntypedList(IImmutableList<T> inner)
	{
		_inner = inner;
	}

	/// <inheritdoc />
	public int Count => _inner.Count;

	/// <inheritdoc />
	public bool IsFixedSize => true;

	/// <inheritdoc />
	public bool IsReadOnly => true;

	/// <inheritdoc />
	public bool IsSynchronized => true;

	/// <inheritdoc />
	public object SyncRoot { get; } = new();

	/// <inheritdoc />
	public object? this[int index]
	{
		get => _inner[index];
		set => throw NotSupported();
	}

	/// <inheritdoc />
	public bool Contains(object value)
		=> IndexOf(value) >= 0;

	/// <inheritdoc />
	public int IndexOf(object value)
		=> value is T t ? _inner.IndexOf(t) : -1;

	/// <inheritdoc />
	public IEnumerator GetEnumerator()
		=> _inner.GetEnumerator();

	/// <inheritdoc />
	public void CopyTo(Array array, int index)
		=> ((ICollection)_inner).CopyTo(array, index);

	/// <inheritdoc />
	public int Add(object value)
		=> throw NotSupported();

	/// <inheritdoc />
	public void Insert(int index, object value)
		=> throw NotSupported();

	/// <inheritdoc />
	public void Remove(object value)
		=> throw NotSupported();

	/// <inheritdoc />
	public void RemoveAt(int index)
		=> throw NotSupported();

	/// <inheritdoc />
	public void Clear()
		=> throw NotSupported();

	private InvalidOperationException NotSupported([CallerMemberName] string? method = null)
		=> new($"Cannot '{method}' on a read only list.");
}
