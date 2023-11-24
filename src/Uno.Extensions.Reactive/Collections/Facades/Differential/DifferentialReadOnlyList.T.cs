using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Collections.Facades.Differential;

internal class DifferentialReadOnlyList<T> : IList, IList<T>, IReadOnlyList<T>
{
	private readonly IDifferentialCollectionNode _head;

	public DifferentialReadOnlyList(IDifferentialCollectionNode head)
		=> _head = head;

	/// <inheritdoc cref="IList{T}" />
	public T this[int index]
	{
		get => (T)_head.ElementAt(index)!; // ! => The node only wraps the T which either is nullable, either didn't permitted the add
		set => throw new NotSupportedException("'__setItem[]' not supported on read only collection.");
	}
	object? IList.this[int index]
	{
		get => _head.ElementAt(index);
		set => throw new NotSupportedException("'__setItem[]' not supported on read only collection.");
	}

	/// <inheritdoc />
	public int Count => _head.Count;

	/// <inheritdoc />
	public int IndexOf(T value) => _head.IndexOf(value!, 0);
	/// <inheritdoc />
	public int IndexOf(object? value) => _head.IndexOf(value, 0);
	/// <inheritdoc />
	public bool Contains(T value) => _head.IndexOf(value!, 0) >= 0;
	/// <inheritdoc />
	public bool Contains(object? value) => _head.IndexOf(value, 0) >= 0;

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator() => new Enumerator<T>(_head);
	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_head);

	/// <inheritdoc />
	public void CopyTo(T[] array, int index)
	{
		using var enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			array.SetValue(enumerator.Current, index++);
		}
	}
	/// <inheritdoc />
	public void CopyTo(Array array, int index)
	{
		using var enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			array.SetValue(enumerator.Current, index++);
		}
	}

	/// <inheritdoc />
	public bool IsReadOnly => true;
	/// <inheritdoc />
	public bool IsFixedSize => false;
	/// <inheritdoc />
	public bool IsSynchronized => true;
	/// <inheritdoc />
	public object SyncRoot { get; } = new();

	/// <summary>Not supported on this collection</summary>
	/// <exception cref="NotSupportedException">In any cases, this method is not supported on this collection.</exception>
	public int Add(object? value) => throw new NotSupportedException("'Add' not supported on read only collection.");
	/// <summary>Not supported on this collection</summary>
	/// <exception cref="NotSupportedException">In any cases, this method is not supported on this collection.</exception>
	public void Add(T value) => throw new NotSupportedException("'Add' not supported on read only collection.");
	/// <summary>Not supported on this collection</summary>
	/// <exception cref="NotSupportedException">In any cases, this method is not supported on this collection.</exception>
	public void Insert(int index, object? value) => throw new NotSupportedException("'Insert' not supported on read only collection.");
	/// <summary>Not supported on this collection</summary>
	/// <exception cref="NotSupportedException">In any cases, this method is not supported on this collection.</exception>
	public void Insert(int index, T value) => throw new NotSupportedException("'Insert' not supported on read only collection.");
	/// <summary>Not supported on this collection</summary>
	/// <exception cref="NotSupportedException">In any cases, this method is not supported on this collection.</exception>
	public void RemoveAt(int index) => throw new NotSupportedException("'RemoveAt' not supported on read only collection.");
	/// <summary>Not supported on this collection</summary>
	/// <exception cref="NotSupportedException">In any cases, this method is not supported on this collection.</exception>
	public void Remove(object? value) => throw new NotSupportedException("'Remove' not supported on read only collection.");
	/// <summary>Not supported on this collection</summary>
	/// <exception cref="NotSupportedException">In any cases, this method is not supported on this collection.</exception>
	public bool Remove(T value) => throw new NotSupportedException("'Remove' not supported on read only collection.");
	/// <summary>Not supported on this collection</summary>
	/// <exception cref="NotSupportedException">In any cases, this method is not supported on this collection.</exception>
	public void Clear() => throw new NotSupportedException("'Clear' not supported on read only collection.");
}
