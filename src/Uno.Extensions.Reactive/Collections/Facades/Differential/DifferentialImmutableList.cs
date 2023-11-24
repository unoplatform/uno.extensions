using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections.Facades.Differential;

internal sealed class DifferentialImmutableList<T> : IImmutableList<T>, IDifferentialCollection, IList
{
	public static DifferentialImmutableList<T> Empty { get; } = new(new EmptyNode());

	public DifferentialImmutableList(IDifferentialCollectionNode head)
		=> Head = head;

	public DifferentialImmutableList(IImmutableList<T> items)
		=> Head = new ResetNode<T>(items);

	/// <inheritdoc />
	public IDifferentialCollectionNode Head { get; }

	/// <inheritdoc cref="IImmutableList{T}" />
	public int Count => Head.Count;

	/// <inheritdoc />
	public bool IsFixedSize => true;

	/// <inheritdoc />
	public bool IsSynchronized => true;

	/// <inheritdoc />
	public object SyncRoot => this;

	/// <inheritdoc />
	public bool IsReadOnly => true;

	/// <inheritdoc />
	public T this[int index] => (T)Head.ElementAt(index)!;

	/// <inheritdoc />
	object? IList.this[int index]
	{
		get => (T)Head.ElementAt(index)!;
		set => throw NotSupported();
	}

	/// <inheritdoc />
	[Pure] public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
		=> Head.IndexOf(item, index, equalityComparer?.ToEqualityComparer());

	/// <inheritdoc />
	[Pure] public int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
		=> throw new NotSupportedException();

	/// <inheritdoc cref="IImmutableList{T}" />
	[Pure] public DifferentialImmutableList<T> Add(T value) => new (new AddNode(Head, value!, Count));
	[Pure] IImmutableList<T> IImmutableList<T>.Add(T value) => Add(value);

	/// <inheritdoc cref="IImmutableList{T}" />
	[Pure] public DifferentialImmutableList<T> AddRange(IImmutableList<T> items) => new(new AddNode(Head, items.AsUntypedList(), Count));
	[Pure] IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items) => AddRange(items.ToImmutableList());

	/// <inheritdoc cref="IImmutableList{T}" />
	[Pure] public DifferentialImmutableList<T> Insert(int index, T element) => new(new AddNode(Head, element!, index));
	[Pure] IImmutableList<T> IImmutableList<T>.Insert(int index, T element) => Insert(index, element);

	/// <inheritdoc cref="IImmutableList{T}" />
	[Pure] public DifferentialImmutableList<T> InsertRange(int index, IImmutableList<T> items) => new(new AddNode(Head, items.AsUntypedList(), index));
	[Pure] IImmutableList<T> IImmutableList<T>.InsertRange(int index, IEnumerable<T> items) => InsertRange(index, items.ToImmutableList());

	/// <inheritdoc cref="IImmutableList{T}" />
	[Pure] public DifferentialImmutableList<T> Remove(T value, IEqualityComparer<T>? equalityComparer)
	{
		var index = Head.IndexOf(value, 0, equalityComparer?.ToEqualityComparer());
		return new(new RemoveNode(Head, value, index));
	}
	[Pure] IImmutableList<T> IImmutableList<T>.Remove(T value, IEqualityComparer<T>? equalityComparer) => Remove(value, equalityComparer);

	/// <inheritdoc cref="IImmutableList{T}" />
	[Pure] public DifferentialImmutableList<T> RemoveAll(Predicate<T> match)
	{
		var head = Head;
		for (var i = Count - 1; i >= 0; i--)
		{
			var item = (T)Head.ElementAt(i)!;
			if (match(item))
			{
				head = new RemoveNode(head, item, i);
			}
		}

		return new(head);
	}
	[Pure] IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match) => RemoveAll(match);

	/// <inheritdoc cref="IImmutableList{T}" />
	//public DifferentialImmutableList<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer)
	[Pure] IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer) => throw new NotSupportedException();

	/// <inheritdoc cref="IImmutableList{T}" />
	[Pure] public DifferentialImmutableList<T> RemoveRange(int index, int count) => new(new RemoveNode(Head, index, count));
	[Pure] IImmutableList<T> IImmutableList<T>.RemoveRange(int index, int count) => RemoveRange(index, count);


	/// <inheritdoc cref="IImmutableList{T}" />
	[Pure] public DifferentialImmutableList<T> RemoveAt(int index) => new(new RemoveNode(Head, index, 1));
	[Pure] IImmutableList<T> IImmutableList<T>.RemoveAt(int index) => RemoveAt(index);

	/// <inheritdoc cref="IImmutableList{T}" />
	[Pure] public DifferentialImmutableList<T> Clear() => new(new EmptyNode());
	[Pure] IImmutableList<T> IImmutableList<T>.Clear() => Clear();

	/// <inheritdoc cref="IImmutableList{T}" />
	[Pure] public DifferentialImmutableList<T> SetItem(int index, T value) => new(new ReplaceNode(Head, value, index));
	[Pure] IImmutableList<T> IImmutableList<T>.SetItem(int index, T value) => SetItem(index, value);

	/// <inheritdoc cref="IImmutableList{T}" />
	//public DifferentialImmutableList<T> Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer) => throw new NotSupportedException();
	[Pure] IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer) => throw new NotSupportedException();

	/// <summary>
	/// Moves a range of items within the collection
	/// </summary>
	/// <param name="fromIndex">The original index of moved items</param>
	/// <param name="toIndex">The target index of moved items</param>
	/// <param name="count">The count of moved items</param>
	/// <returns>A new instance of immutable list where items has been moved.</returns>
	[Pure] public DifferentialImmutableList<T> MoveRange(int fromIndex, int toIndex, int count) => new(new MoveNode(Head, fromIndex, toIndex, count));

	/// <summary>
	/// Moves a range of items within the collection
	/// </summary>
	/// <param name="index">Index of the element that is being replaced.</param>
	/// <param name="newValue">The new value</param>
	/// <returns>A new instance of immutable list where item has been replaced.</returns>
	[Pure] public DifferentialImmutableList<T> ReplaceAt(int index, T newValue) => new(new ReplaceNode(Head, Head.ElementAt(index), newValue, index));

	/// <inheritdoc />
	bool IList.Contains(object? value) => ((IList)this).IndexOf(value) >= 0;

	/// <inheritdoc />
	int IList.IndexOf(object? value) => Head.IndexOf(value, 0);

	/// <inheritdoc />
	int IList.Add(object? value) => throw NotSupported();

	/// <inheritdoc />
	void IList.Insert(int index, object? value) => throw NotSupported();

	/// <inheritdoc />
	void IList.Remove(object? value) => throw NotSupported();

	/// <inheritdoc />
	void IList.RemoveAt(int index) => throw NotSupported();

	/// <inheritdoc />
	void IList.Clear() => throw NotSupported();

	/// <inheritdoc />
	[Pure] public IEnumerator<T> GetEnumerator() => new Enumerator<T>(Head);

	/// <inheritdoc />
	[Pure] IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	/// <inheritdoc />
	public void CopyTo(Array array, int index)
	{
		foreach (var item in this)
		{
			array.SetValue(item, index++);
		}
	}

	private InvalidOperationException NotSupported([CallerMemberName] string? method = null)
		=> new($"Cannot '{method}' on a read only list.");
}
