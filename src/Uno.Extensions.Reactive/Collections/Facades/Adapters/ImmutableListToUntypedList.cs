using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace Uno.Extensions.Reactive.Collections.Facades.Adapters;

internal class ImmutableListToUntypedList<T> : IList, IList<T>, IImmutableList<T>, IReadOnlyList<T>, ICollectionAdapter
{
	private readonly IImmutableList<T> _inner;

	object ICollectionAdapter.Adaptee => _inner;

	public ImmutableListToUntypedList(IImmutableList<T> inner)
	{
		_inner = inner;
	}

	/// <inheritdoc cref="IList" />
	public int Count => _inner.Count;

	/// <inheritdoc />
	public bool IsFixedSize => true;

	/// <inheritdoc cref="IList" />
	public bool IsReadOnly => true;

	/// <inheritdoc />
	public bool IsSynchronized => false;

	/// <inheritdoc />
	public object SyncRoot { get; } = new();

	/// <inheritdoc />
	T IReadOnlyList<T>.this[int index] => _inner[index];

	/// <inheritdoc />
	object? IList.this[int index]
	{
		get => _inner[index];
		set => throw NotSupported();
	}

	/// <inheritdoc />
	T IList<T>.this[int index]
	{
		get => _inner[index];
		set => throw NotSupported();
	}

	/// <inheritdoc />
	public bool Contains(object? value)
		=> IndexOf(value) >= 0;

	/// <inheritdoc />
	public bool Contains(T item)
		=> IndexOf(item) >= 0;

	/// <inheritdoc />
	public int IndexOf(object? value)
		=> value is T t ? _inner.IndexOf(t) : -1;

	/// <inheritdoc />
	public int IndexOf(T item)
		=> _inner.IndexOf(item);

	/// <inheritdoc />
	public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
		=> _inner.IndexOf(item, index, count, equalityComparer);

	/// <inheritdoc />
	public int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
		=> _inner.LastIndexOf(item, index, count, equalityComparer);

	/// <inheritdoc />
	public IEnumerator GetEnumerator()
		=> _inner.GetEnumerator();

	/// <inheritdoc />
	IEnumerator<T> IEnumerable<T>.GetEnumerator()
		=> _inner.GetEnumerator();

	/// <inheritdoc />
	public void CopyTo(Array array, int index)
		=> ((ICollection)_inner).CopyTo(array, index);

	/// <inheritdoc />
	public void CopyTo(T[] array, int arrayIndex)
		=> ((ICollection<T>)_inner).CopyTo(array, arrayIndex);

	/// <inheritdoc />
	int IList.Add(object? value)
		=> throw NotSupported();

	/// <inheritdoc />
	void ICollection<T>.Add(T item)
		=> throw NotSupported();

	/// <inheritdoc />
	public IImmutableList<T> Add(T value)
		=> _inner.Add(value);

	/// <inheritdoc />
	public IImmutableList<T> AddRange(IEnumerable<T> items)
		=> _inner.AddRange(items);

	/// <inheritdoc />
	void IList.Insert(int index, object? value)
		=> throw NotSupported();

	/// <inheritdoc />
	void IList<T>.Insert(int index, T item)
		=> throw NotSupported();

	/// <inheritdoc />
	public IImmutableList<T> InsertRange(int index, IEnumerable<T> items)
		=> _inner.InsertRange(index, items);

	/// <inheritdoc />
	public IImmutableList<T> Insert(int index, T element)
		=> _inner.Insert(index, element);

	/// <inheritdoc />
	public IImmutableList<T> SetItem(int index, T value)
		=> _inner.SetItem(index, value);

	/// <inheritdoc />
	public IImmutableList<T> Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
		=> _inner.Replace(oldValue, newValue, equalityComparer);

	/// <inheritdoc />
	void IList.Remove(object? value)
		=> throw NotSupported();

	/// <inheritdoc />
	public bool Remove(T item)
		=> throw NotSupported();

	/// <inheritdoc />
	public IImmutableList<T> Remove(T value, IEqualityComparer<T>? equalityComparer)
		=> _inner.Remove(value, equalityComparer);

	/// <inheritdoc />
	public IImmutableList<T> RemoveAll(Predicate<T> match)
		=> _inner.RemoveAll(match);

	/// <inheritdoc />
	public IImmutableList<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
		=> _inner.RemoveRange(items, equalityComparer);

	/// <inheritdoc />
	public IImmutableList<T> RemoveRange(int index, int count)
		=> _inner.RemoveRange(index, count);

	/// <inheritdoc />
	void IList.RemoveAt(int index)
		=> throw NotSupported();

	/// <inheritdoc />
	void IList<T>.RemoveAt(int index)
		=> throw NotSupported();

	/// <inheritdoc />
	public IImmutableList<T> RemoveAt(int index)
		=> _inner.RemoveAt(index);

	/// <inheritdoc />
	void IList.Clear()
		=> throw NotSupported();

	/// <inheritdoc />
	void ICollection<T>.Clear()
		=> throw NotSupported();

	/// <inheritdoc />
	public IImmutableList<T> Clear()
		=> _inner.Clear();

	private InvalidOperationException NotSupported([CallerMemberName] string? method = null)
		=> new($"Cannot '{method}' on a read only list.");
}
