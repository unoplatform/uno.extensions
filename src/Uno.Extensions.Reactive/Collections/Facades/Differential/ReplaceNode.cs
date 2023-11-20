using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Uno.Extensions;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections.Facades.Differential;

/// <summary>
/// A node of a linked stack of <see cref="IDifferentialCollectionNode"/> which replace some items
/// </summary>
internal sealed class ReplaceNode : IDifferentialCollectionNode
{
	private readonly int _totalCount, _changeCount, _addedCount, _removedCount, _fromIndex, _addToIndex, _removeToIndex;
	private readonly IList _added;

	public ReplaceNode(IDifferentialCollectionNode previous, NotifyCollectionChangedEventArgs arg)
	{
		Debug.Assert(arg.Action is NotifyCollectionChangedAction.Replace);

		Previous = previous;

		_added = arg.NewItems!;
		_addedCount = arg.NewItems!.Count;
		//_removed = arg.OldItems; // Useless and prevent reference on removed items (TODO: Deref items in _previous)
		_removedCount = arg.OldItems!.Count;

		_changeCount = _addedCount - _removedCount;
		_totalCount = previous.Count + _changeCount;

		_fromIndex = arg.NewStartingIndex;
		_addToIndex = arg.NewStartingIndex + _addedCount;
		_removeToIndex = arg.OldStartingIndex + _removedCount;
	}

	public ReplaceNode(IDifferentialCollectionNode previous, object? newItem, int index)
		: this(previous, null, newItem, index)
	{
	}

	public ReplaceNode(IDifferentialCollectionNode previous, object? oldItem, object? newItem, int index)
	{
		Previous = previous;

		_added = new[] {newItem};
		_addedCount = 1;
		//_removed = arg.OldItems; // Useless and prevent reference on removed items (TODO: Deref items in _previous)
		_removedCount = 1;

		_changeCount = 0;
		_totalCount = previous.Count;

		_fromIndex = index;
		_addToIndex = index + 1;
		_removeToIndex = index + 1;
	}

	/// <inheritdoc />
	public IDifferentialCollectionNode Previous { get; }

	/// <summary>
	/// The index at which the replace occurs
	/// </summary>
	public int At => _fromIndex;

	/// <inheritdoc />
	public int Count => _totalCount;

	/// <inheritdoc />
	public object? ElementAt(int index)
	{
		if (index < _fromIndex)
		{
			return Previous.ElementAt(index);
		}
		else if (index < _addToIndex)
		{
			return _added[index - _fromIndex];
		}
		else
		{
			return Previous.ElementAt(index - _changeCount);
		}
	}

	/// <inheritdoc />
	public int IndexOf(object? value, int startingAt, IEqualityComparer? comparer)
	{
		if (startingAt < _fromIndex)
		{
			// If search begins before the 'added' items, we search the 'previous' version, and then search in 'added' items only 
			// if the result index from the 'previous' is after the 'added' items.

			var previousIndex = Previous.IndexOf(value, startingAt, comparer);
			var previousFound = previousIndex >= 0;
			if (previousFound && previousIndex < _fromIndex)
			{
				return previousIndex;
			}

			var addedIndex = _added.IndexOf(value, comparer);
			if (addedIndex >= 0)
			{
				return addedIndex + _fromIndex;
			}

			if (previousFound && previousIndex < _removeToIndex)
			{
				return Previous.IndexOf(value, _removeToIndex, comparer) + _changeCount;
			}

			return previousFound
				? previousIndex + _changeCount
				: -1;

		}
		else if (startingAt < _addToIndex)
		{
			// If the search begins in the 'added' items, first search in 'added', then search in 'previous'

			// for boucle == _added.IndexOf(value, _startingIndex) which does not exists on IList
			var safeComparer = comparer ?? EqualityComparer<object>.Default;
			for (var i = startingAt - _fromIndex; i < _addedCount; i++)
			{
				if (safeComparer.Equals(value, _added[i]))
				{
					return _fromIndex + i;
				}
			}

			var previousIndex = Previous.IndexOf(value, _fromIndex, comparer);
			var previousFound = previousIndex >= 0;

			if (previousFound && previousIndex < _removeToIndex)
			{
				return Previous.IndexOf(value, _removeToIndex, comparer) + _changeCount;
			}

			return previousFound
				? previousIndex + _changeCount
				: -1;
		}
		else // startAt >= _toIndex
		{
			var previousIndex = Previous.IndexOf(value, startingAt - _changeCount, comparer);
			var previousFound = previousIndex >= 0;

			if (previousFound && previousIndex < _removeToIndex)
			{
				return Previous.IndexOf(value, _removeToIndex, comparer) + _changeCount;
			}

			return previousFound
				? previousIndex + _changeCount
				: -1;
		}
	}
}
