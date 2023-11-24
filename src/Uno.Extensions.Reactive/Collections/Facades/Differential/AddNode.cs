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
/// A node of a linked stack of <see cref="IDifferentialCollectionNode"/> which add some items
/// </summary>
internal sealed class AddNode : IDifferentialCollectionNode
{
	private readonly int _totalCount, _addedCount, _fromIndex, _toIndex;
	private readonly IList _added;
	private readonly IDifferentialCollectionNode _previous;

	public AddNode(IDifferentialCollectionNode previous, NotifyCollectionChangedEventArgs addArg)
	{
		Debug.Assert(addArg.Action is NotifyCollectionChangedAction.Add);

		_previous = previous;
		_added = addArg.NewItems!;
		_addedCount = addArg.NewItems!.Count;

		_totalCount = previous.Count + _addedCount;
		_fromIndex = addArg.NewStartingIndex;
		_toIndex = addArg.NewStartingIndex + _addedCount;
	}

	public AddNode(IDifferentialCollectionNode previous, IList items, int index)
	{
		_previous = previous;
		_added = items;
		_addedCount = items.Count;

		_totalCount = previous.Count + _addedCount;
		_fromIndex = index;
		_toIndex = index + _addedCount;
	}

	public AddNode(IDifferentialCollectionNode previous, object item, int index)
	{
		_previous = previous;
		_added = new[] { item };
		_addedCount = 1;

		_totalCount = previous.Count + 1;
		_fromIndex = index;
		_toIndex = index + 1;
	}


	/// <summary>
	/// The index at which the add occurs
	/// </summary>
	public int At => _fromIndex;

	/// <summary>
	/// The number of added items
	/// </summary>
	public int Range => _addedCount;

	/// <inheritdoc />
	public IDifferentialCollectionNode Previous => _previous;

	/// <inheritdoc />
	public int Count => _totalCount;

	/// <inheritdoc />
	public object? ElementAt(int index)
	{
		if (index < _fromIndex)
		{
			return _previous.ElementAt(index);
		}
		else if (index < _toIndex)
		{
			return _added[index - _fromIndex];
		}
		else
		{
			return _previous.ElementAt(index - _addedCount);
		}
	}

	/// <inheritdoc />
	public int IndexOf(object? value, int startingAt, IEqualityComparer? comparer)
	{
		if (startingAt < _fromIndex)
		{
			// If search begins before the 'added' items, we search the 'previous' version, and then search in 'added' items only 
			// if the result index from the 'previous' is after the 'added' items.

			var previousIndex = _previous.IndexOf(value, startingAt, comparer);
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

			return previousFound
				? previousIndex + _addedCount
				: -1;

		}
		else if (startingAt < _fromIndex + _addedCount)
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

			var previousIndex = _previous.IndexOf(value, _fromIndex, comparer);
			var previousFound = previousIndex >= 0;

			return previousFound
				? previousIndex + _addedCount
				: -1;
		}
		else // startAt >= _toIndex
		{
			var previousIndex = _previous.IndexOf(value, startingAt - _addedCount, comparer);
			var previousFound = previousIndex >= 0;

			return previousFound
				? previousIndex + _addedCount
				: -1;
		}
	}
}
