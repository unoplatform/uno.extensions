using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Collections.Facades.Differential;

internal sealed class DifferentialReadOnlyList : IList
{
	private readonly IDifferentialCollectionNode _head;

	public DifferentialReadOnlyList(IDifferentialCollectionNode head) => _head = head;

	/// <inheritdoc />
	public object? this[int index]
	{
		get => index >= 0 && index < Count ? ElementAt(index)! : throw new IndexOutOfRangeException();
		set => throw new NotSupportedException("'__setItem[]' not supported on read only collection.");
	}

	/// <inheritdoc />
	public int Count => _head.Count;

	private object? ElementAt(int index) => _head.ElementAt(index);
	/// <inheritdoc />
	public int IndexOf(object? value) => _head.IndexOf(value, 0);
	/// <inheritdoc />
	public bool Contains(object? value) => _head.IndexOf(value, 0) >= 0;

	/// <inheritdoc />
	public IEnumerator GetEnumerator() => _head.GetEnumerator();

	/// <inheritdoc />
	public void CopyTo(Array array, int index)
	{
		var enumerator = GetEnumerator();
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
	public void Insert(int index, object? value) => throw new NotSupportedException("'Insert' not supported on read only collection.");
	/// <summary>Not supported on this collection</summary>
	/// <exception cref="NotSupportedException">In any cases, this method is not supported on this collection.</exception>
	public void RemoveAt(int index) => throw new NotSupportedException("'RemoveAt' not supported on read only collection.");
	/// <summary>Not supported on this collection</summary>
	/// <exception cref="NotSupportedException">In any cases, this method is not supported on this collection.</exception>
	public void Remove(object? value) => throw new NotSupportedException("'Remove' not supported on read only collection.");
	/// <summary>Not supported on this collection</summary>
	/// <exception cref="NotSupportedException">In any cases, this method is not supported on this collection.</exception>
	public void Clear() => throw new NotSupportedException("'Clear' not supported on read only collection.");
}
