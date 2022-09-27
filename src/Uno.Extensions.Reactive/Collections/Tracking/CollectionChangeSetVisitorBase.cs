using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Uno.Extensions.Collections.Tracking;

/// <summary>
/// An helper base case to ease implementation of <see cref="ICollectionChangeSetVisitor{T}"/>
/// </summary>
/// <typeparam name="T">Type of the items.</typeparam>
/// <remarks>
/// This base class allows you to implement only operations you are interested in.
/// A typical minimum implementation interested only in object instances would override AddItem and RemoveItem.
/// For index tracking you should consider to override almost all actions (Add, Replace, Move, Remove, Reset).
/// </remarks>
internal class CollectionChangeSetVisitorBase<T> : ICollectionChangeSetVisitor<T>
{
	/// <inheritdoc />
	/// <remarks>The default implementation will invoke <see cref="AddItem"/> for each item in <paramref name="items"/>.</remarks>
	public virtual void Add(IReadOnlyList<T> items, int index)
	{
		for (var i = 0; i < items.Count; i++)
		{
			AddItem(items[i], index + i);
		}
	}

	/// <summary>
	/// Invoked when an item has been kept as is.
	/// </summary>
	/// <param name="index">The index where the item has been added.</param>
	/// <param name="item">The added item.</param>
	/// <remarks>The default implementation does nothing.</remarks>
	protected virtual void AddItem(T item, int index)
	{
	}

	/// <inheritdoc />
	/// <remarks>The default implementation will invoke <see cref="SameItem"/> for each item in <paramref name="original"/> and <paramref name="updated" />.</remarks>
	public virtual void Same(IReadOnlyList<T> original, IReadOnlyList<T> updated, int index)
	{
		if (original.Count != updated.Count)
		{
			throw new InvalidOperationException("CollectionChangeSet is expected to always have same number of items for Replace operation.");
		}

		for (var i = 0; i < original.Count; i++)
		{
			SameItem(original[i], updated[i], index + i);
		}
	}

	/// <summary>
	/// Invoked when an item has been kept as is.
	/// </summary>
	/// <param name="index">The index where the item has been kept.</param>
	/// <param name="original">The old item.</param>
	/// <param name="updated">The updated item.</param>
	/// <remarks>The default implementation does nothing.</remarks>
	protected virtual void SameItem(T original, T updated, int index)
	{
	}

	/// <inheritdoc />
	/// <remarks>
	/// The default implementation will invoke <see cref="Remove"/> with the <paramref name="original"/>
	/// and then <see cref="Add"/> with the <paramref name="updated"/>.
	/// </remarks>
	public virtual void Replace(IReadOnlyList<T> original, IReadOnlyList<T> updated, int index)
	{
		if (original.Count != updated.Count)
		{
			throw new InvalidOperationException("CollectionChangeSet is expected to always have same number of items for Replace operation.");
		}

		Remove(original, index);
		Add(updated, index);
	}

	/// <summary>
	/// Invoked when an item has been replaced.
	/// </summary>
	/// <param name="index">The index where the item has been replaced.</param>
	/// <param name="original">The old item.</param>
	/// <param name="updated">The updated item.</param>
	/// <remarks>The default implementation will do <see cref="RemoveItem"/> then <see cref="AddItem"/>.</remarks>
	protected virtual void ReplaceItem(T original, T updated, int index)
	{
		RemoveItem(original, index);
		AddItem(updated, index);
	}

	/// <inheritdoc />
	public virtual void Move(IReadOnlyList<T> items, int fromIndex, int toIndex)
	{
		for (var i = 0; i < items.Count; i++)
		{
			MoveItem(items[i], fromIndex + i, toIndex + i);
		}
	}

	/// <summary>
	/// Invoked when an item has been moved from an index to another one.
	/// </summary>
	/// <param name="from">The index where the item was.</param>
	/// <param name="to">The index where the item is.</param>
	/// <param name="item">The moved item.</param>
	/// <remarks>The default implementation does nothing.</remarks>
	protected virtual void MoveItem(T item, int from, int to)
	{
	}

	/// <inheritdoc />
	/// <remarks>The default implementation will invoke <see cref="RemoveItem"/> for each item in <paramref name="items"/>.</remarks>
	public virtual void Remove(IReadOnlyList<T> items, int index)
	{
		for (var i = 0; i < items.Count; i++)
		{
			RemoveItem(items[i], index + i);
		}
	}

	/// <summary>
	/// Invoked when an item has been kept as is.
	/// </summary>
	/// <param name="index">The index where the item has been added.</param>
	/// <param name="item">The added item.</param>
	/// <remarks>The default implementation does nothing.</remarks>
	protected virtual void RemoveItem(T item, int index)
	{
	}

	/// <inheritdoc />
	/// <remarks>
	/// The default implementation will invoke <see cref="Remove"/> with the <paramref name="oldItems"/>
	/// and then <see cref="Add"/> with the <paramref name="newItems"/>.
	/// </remarks>
	public virtual void Reset(IReadOnlyList<T> oldItems, IReadOnlyList<T> newItems)
	{
		Remove(oldItems, 0);
		Add(newItems, 0);
	}
}
