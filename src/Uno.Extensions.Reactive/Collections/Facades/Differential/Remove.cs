using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;

namespace Umbrella.Collections.Facades.Differential;

/// <summary>
/// A node of a linked stack of <see cref="IDifferentialCollectionNode"/> which remove some items
/// </summary>
internal sealed class Remove : IDifferentialCollectionNode
{
	private readonly int _totalCount, _removedCount, _fromIndex, _toIndex;
	private readonly IDifferentialCollectionNode _previous;

	public Remove(IDifferentialCollectionNode previous, NotifyCollectionChangedEventArgs addArg)
	{
		_previous = previous;
		//_removed = addArg.OldItems; // Useless and prevent reference on removed items (TODO: Deref items in _previous)
		_removedCount = addArg.OldItems.Count;

		_totalCount = previous.Count - _removedCount;
		_fromIndex = addArg.OldStartingIndex;
		_toIndex = addArg.OldStartingIndex + _removedCount;
	}

	public Remove(IDifferentialCollectionNode previous, object item, int index)
	{
		_previous = previous;
		//_removed // Useless and prevent reference on removed items (TODO: Deref items in _previous)
		_removedCount = 1;

		_totalCount = previous.Count - 1;
		_fromIndex = index;
		_toIndex = index + 1;
	}

	/// <summary>
	/// The index at which the remove occurs
	/// </summary>
	public int At => _fromIndex;

	/// <summary>
	/// The number of removed items
	/// </summary>
	public int Range => _removedCount;

	/// <inheritdoc />
	public int Count => _totalCount;

	/// <inheritdoc />
	public object? ElementAt(int index)
	{
		if (index >= _fromIndex)
		{
			index += _removedCount;
		}

		return _previous.ElementAt(index);
	}

	/// <inheritdoc />
	public int IndexOf(object? value, int startingAt, IEqualityComparer? comparer)
	{
		if (startingAt >= _fromIndex)
		{
			startingAt += _removedCount;
		}

		var previousIndex = _previous.IndexOf(value, startingAt, comparer);
		if (previousIndex < _fromIndex)
		{
			return previousIndex;
		}
		else if (previousIndex < _toIndex)
		{
			return Math.Max(-1, _previous.IndexOf(value, _toIndex, comparer) - _removedCount);
		}
		else
		{
			return previousIndex - _removedCount;
		}
	}
}
