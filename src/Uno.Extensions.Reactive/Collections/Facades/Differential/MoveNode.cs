using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections.Facades.Differential;

/// <summary>
/// A node of a linked stack of <see cref="IDifferentialCollectionNode"/> which move some items
/// </summary>
internal sealed class MoveNode : IDifferentialCollectionNode
{
	private readonly int _movedCount, _oldFromIndex, _oldToIndex, _newFromIndex, _newToIndex;
	private readonly IList _moved;
	private readonly bool _isBackward;

	public MoveNode(IDifferentialCollectionNode previous, NotifyCollectionChangedEventArgs arg)
	{
		Debug.Assert(arg.Action is NotifyCollectionChangedAction.Move);

		Previous = previous;
		Count = previous.Count;

		_moved = arg.OldItems!;
		_movedCount = _moved.Count;

		_oldFromIndex = arg.OldStartingIndex;
		_oldToIndex = _oldFromIndex + _movedCount;
		_newFromIndex = arg.NewStartingIndex;
		_newToIndex = _newFromIndex + _movedCount;

		_isBackward = _oldFromIndex > _newFromIndex;
	}

	public MoveNode(IDifferentialCollectionNode previous, int fromIndex, int toIndex, int count)
	{
		Previous = previous;
		Count = previous.Count;

		_moved = previous.AsList().Slice(fromIndex, count);
		_movedCount = count;

		_oldFromIndex = fromIndex;
		_oldToIndex = _oldFromIndex + _movedCount;
		_newFromIndex = toIndex;
		_newToIndex = _newFromIndex + _movedCount;

		_isBackward = _oldFromIndex > _newFromIndex;
	}

	/// <inheritdoc />
	public IDifferentialCollectionNode Previous { get; }

	/// <inheritdoc />
	public int Count { get; }

	/// <inheritdoc />
	public object? ElementAt(int index)
	{
		if (index < _newFromIndex)
		{
			if (index >= _oldFromIndex)
			{
				index += _movedCount;
			}

			return Previous.ElementAt(index);
		}
		else if (index < _newToIndex)
		{
			return _moved[index - _newFromIndex];
		}
		else
		{
			if (index < _oldToIndex)
			{
				index -= _movedCount;
			}

			return Previous.ElementAt(index);
		}
	}

	/// <inheritdoc />
	public int IndexOf(object? value, int startingAt, IEqualityComparer? comparer = null)
	{
		if (startingAt > _newToIndex)
		{
			// Fast path: we are staring after the affected range, we can just search in previous
			return Previous.IndexOf(value, _oldToIndex, comparer);
		}

		if (_isBackward)
		{
			var previousIndex = Previous.IndexOf(value, startingAt, comparer);
			if (previousIndex < _newFromIndex)
			{
				// item is before the moved range, index is valid
				// **/item/**[**NEW**]****[**OLD**]****
				return previousIndex;
			}
			else if (previousIndex >= _oldToIndex)
			{
				// Item is after the moved range, index is valid
				// ****[**NEW****]****[**OLD**]**/item/**

				return previousIndex;
			}
			else if (previousIndex >= _oldFromIndex)
			{
				// Item is either in the moved range, adjust index and use it
				// ****[**NEW**]****[**OLD**/item/**]****

				return previousIndex - (_oldFromIndex - _newFromIndex);
			}
			else // previousIndex >= _newFromIndex
			{
				// item is before the moved item, we must search in the moved items if another version is available
				// ****[**NEW**]**/item/**[**OLD**]****

				for (var i = 0; i < _moved.Count; i++)
				{
					if (_moved[i] == value)
					{
						// There is a matching items in the moved items
						// ****[**NEW**/item/**]****[**OLD**]****

						return _newFromIndex + i;
					}
				}

				// no items found, adjust the index, and use it
				return previousIndex + _movedCount;
			}
		}
		else
		{
			var previousIndex = Previous.IndexOf(value, startingAt, comparer);
			if (previousIndex < _oldFromIndex)
			{
				// item is before the moved range, index is valid
				// **/item/**[**OLD**]****[**NEW**]****
				return previousIndex;
			}
			else if (previousIndex < _oldToIndex)
			{
				// item is in the moved range. We must validate that another ocurence of the is not present before the place of the item
				// ****[**OLD**/item/**]****[**NEW**]****

				var alternativePreviousIndex = Previous.IndexOf(value, _oldToIndex, comparer);
				if (alternativePreviousIndex >= 0 && alternativePreviousIndex < _newFromIndex)
				{
					// the other occurrence is between the old position of moved items nad their noew position, we have to use this other index
					// ****[**OLD**/item/**]**/altItem/**[**NEW**]****

					return alternativePreviousIndex - _movedCount;
				}
				else
				{
					// the items is after the new position of moved items, we can use the item in the moved range
					// ****[**OLD**/item/**]****[**NEW**]**/altitem/**

					return previousIndex + (_newFromIndex - _oldFromIndex);
				}
			}
			else if (previousIndex < _newFromIndex + _movedCount)
			{
				// item is between the old position and the new position
				// ****[**OLD**]**/item/**[**NEW**]****

				return previousIndex - _movedCount;
			}
			else // previousIndex >= _newToIndex
			{
				// ****[**OLD**]****[**NEW**]**/item/**

				return previousIndex;
			}
		}
	}
}
