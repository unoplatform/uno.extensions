using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace Uno.Extensions.Collections.Facades.Differential;

/// <summary>
/// A node of a linked stack of <see cref="IDifferentialCollectionNode"/> which remove some items
/// </summary>
internal sealed class RemoveNode : IDifferentialCollectionNode
{
	private readonly int _totalCount, _removedCount, _fromIndex, _toIndex;

	public RemoveNode(IDifferentialCollectionNode previous, NotifyCollectionChangedEventArgs arg)
	{
		Debug.Assert(arg.Action is NotifyCollectionChangedAction.Remove);

		Previous = previous;
		_removedCount = arg.OldItems!.Count;

		_totalCount = previous.Count - _removedCount;
		_fromIndex = arg.OldStartingIndex;
		_toIndex = arg.OldStartingIndex + _removedCount;
	}

	public RemoveNode(IDifferentialCollectionNode previous, int index, int count)
	{
		Previous = previous;
		_removedCount = count;

		_totalCount = previous.Count - count;
		_fromIndex = index;
		_toIndex = index + count;
	}

	public RemoveNode(IDifferentialCollectionNode previous, object? item, int index)
	{
		Previous = previous;
		//_removed // Useless and prevent reference on removed items (TODO: Deref items in _previous)
		_removedCount = 1;

		_totalCount = previous.Count - 1;
		_fromIndex = index;
		_toIndex = index + 1;
	}

	/// <inheritdoc />
	public IDifferentialCollectionNode Previous { get; }

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

		return Previous.ElementAt(index);
	}

	/// <inheritdoc />
	public int IndexOf(object? value, int startingAt, IEqualityComparer? comparer)
	{
		if (startingAt >= _fromIndex)
		{
			startingAt += _removedCount;
		}

		var previousIndex = Previous.IndexOf(value, startingAt, comparer);
		if (previousIndex < _fromIndex)
		{
			return previousIndex;
		}
		else if (previousIndex < _toIndex)
		{
			return Math.Max(-1, Previous.IndexOf(value, _toIndex, comparer) - _removedCount);
		}
		else
		{
			return previousIndex - _removedCount;
		}
	}
}
