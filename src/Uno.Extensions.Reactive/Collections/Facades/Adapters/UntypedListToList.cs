using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uno.Extensions.Reactive.Collections.Facades.Adapters;

internal class UntypedListToList<T> : IList, IList<T>, IReadOnlyList<T>, ICollectionAdapter
{
	private readonly IList _inner;

	object ICollectionAdapter.Adaptee => _inner;

	public UntypedListToList(IList inner)
	{
		_inner = inner;
	}

	/// <inheritdoc cref="IList" />
	public int Count => _inner.Count;

	/// <inheritdoc />
	public bool IsSynchronized => _inner.IsSynchronized;

	/// <inheritdoc />
	public object SyncRoot => _inner.SyncRoot;

	/// <inheritdoc cref="IList" />
	public bool IsReadOnly => _inner.IsReadOnly;

	/// <inheritdoc />
	public bool IsFixedSize => _inner.IsFixedSize;

	/// <inheritdoc cref="IList{T}" />
	public T this[int index]
	{
		get => (T)_inner[index]!;
		set => _inner[index] = value;
	}

	/// <inheritdoc />
	object? IList.this[int index]
	{
		get => _inner[index];
		set => _inner[index] = value;
	}

	/// <inheritdoc />
	public bool Contains(object? value)
		=> _inner.Contains(value);

	/// <inheritdoc />
	public bool Contains(T item)
		=> _inner.Contains(item);

	/// <inheritdoc />
	public int IndexOf(object? value)
		=> _inner.IndexOf(value);

	/// <inheritdoc />
	public int IndexOf(T item)
		=> _inner.IndexOf(item);

	/// <inheritdoc />
	public void CopyTo(Array array, int index)
		=> _inner.CopyTo(array, index);

	/// <inheritdoc />
	public void CopyTo(T[] array, int arrayIndex)
		=> _inner.CopyTo(array, arrayIndex);

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
		=> _inner.GetEnumerator();

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator()
		=> _inner.Cast<T>().GetEnumerator();

	/// <inheritdoc />
	public void Add(T item)
		=> _inner.Add(item);

	/// <inheritdoc />
	public int Add(object? value)
		=> _inner.Add(value);

	/// <inheritdoc />
	public void Insert(int index, object? value)
		=> _inner.Insert(index, value);

	/// <inheritdoc />
	public void Insert(int index, T item)
		=> _inner.Insert(index, item);

	/// <inheritdoc cref="IList{T}" />
	public void RemoveAt(int index)
		=> _inner.RemoveAt(index);

	/// <inheritdoc />
	public void Remove(object? value)
		=> _inner.Remove(value);

	/// <inheritdoc />
	public bool Remove(T item)
	{
		var previousCount = _inner.Count;
		_inner.Remove(item);

		return _inner.Count != previousCount;
	}

	/// <inheritdoc cref="IList" />
	public void Clear()
		=> _inner.Clear();
}
