using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uno.Extensions.Reactive.Collections.Facades.Adapters;

internal class ListToUntypedList<T> : IList, IList<T>, IReadOnlyList<T>, ICollectionAdapter
{
	private readonly IList<T> _inner;

	object ICollectionAdapter.Adaptee => _inner;

	public ListToUntypedList(IList<T> inner)
	{
		_inner = inner;
	}

	/// <inheritdoc cref="IList" />
	public int Count => _inner.Count;

	/// <inheritdoc />
	public bool IsSynchronized => false;

	/// <inheritdoc />
	public object SyncRoot { get; } = new();

	/// <inheritdoc cref="IList" />
	public bool IsReadOnly => _inner.IsReadOnly;

	/// <inheritdoc />
	public bool IsFixedSize => ((IList)_inner).IsFixedSize;

	/// <inheritdoc />
	object? IList.this[int index]
	{
		get => _inner[index];
		set => _inner[index] = (T)(value ?? throw new ArgumentNullException(nameof(value)));
	}

	/// <inheritdoc cref="IList" />
	public T this[int index]
	{
		get => _inner[index];
		set => _inner[index] = value;
	}

	/// <inheritdoc />
	public bool Contains(object? value)
		=> _inner.Contains((T)value!); // Ignore null check as the T might be a value type or a nullable type

	/// <inheritdoc />
	public bool Contains(T item)
		=> _inner.Contains(item);

	/// <inheritdoc />
	public int IndexOf(object? value)
		=> _inner.IndexOf((T)value!); // Ignore null check as the T might be a value type or a nullable type

	/// <inheritdoc />
	public int IndexOf(T item)
		=> _inner.IndexOf(item);

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
		=> _inner.GetEnumerator();

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator()
		=> _inner.Cast<T>().GetEnumerator();

	/// <inheritdoc />
	public void CopyTo(Array array, int index)
		=> ((ICollection)_inner).CopyTo(array, index);

	/// <inheritdoc />
	public void CopyTo(T[] array, int arrayIndex)
		=> _inner.CopyTo(array, arrayIndex);

	/// <inheritdoc />
	public int Add(object? value)
	{
		_inner.Add((T)value!); // Ignore null check as the T might be a value type or a nullable type
		return _inner.Count - 1;
	}

	/// <inheritdoc />
	public void Add(T item)
		=> _inner.Add(item);

	
	/// <inheritdoc />
	public void Insert(int index, object? value)
	{
		switch (value)
		{
			case null:
				throw new ArgumentNullException(nameof(value));
			case T t:
				_inner.Insert(index, t);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(value), $"Value must be of type {typeof(T)}");
		}
	}

	/// <inheritdoc />
	public void Insert(int index, T item)
		=> _inner.Insert(index, item);

	/// <inheritdoc cref="IList" />
	public void RemoveAt(int index)
		=> _inner.RemoveAt(index);

	/// <inheritdoc />
	public void Remove(object? value)
	{
		if (value is T t)
		{
			_inner.Remove(t);
		}
	}

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
