using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections.Tracking;

partial class CollectionAnalyzer
{
	private sealed class _Replace<T> : EntityChange<T>
	{
		private readonly bool _isReplaceOfSameEntities;

		/// <inheritdoc />
		public _Replace(int at, int indexOffset, bool isReplaceOfSameEntities = true)
			: base(at, indexOffset)
		{
			_isReplaceOfSameEntities = isReplaceOfSameEntities;
		}

		public override RichNotifyCollectionChangedEventArgs ToEvent()
			=> RichNotifyCollectionChangedEventArgs.ReplaceSome<T>(_oldItems, _newItems, Starts + _indexOffset, _isReplaceOfSameEntities);

		/// <inheritdoc />
		protected internal override void Visit(ICollectionChangeSetVisitor<T> visitor)
			=> visitor.Replace(_oldItems, _newItems, Starts + _indexOffset);


		/// <inheritdoc />
		protected override CollectionUpdater.Update ToUpdaterCore(ICollectionUpdaterVisitor visitor)
		{
			if (_oldItems is {Count: 0})
			{
				// Safety case, should never happen
				return new();
			}

			CollectionUpdater.Update? head = default, tail = default;

			// Some elements are handled by callbacks only and some are not.
			// For handled elements we don't need any event (usually when a nested group is updated).
			var tailIsForHandled = false;

			// An intermediate Update that is used to get the callbacks set by the vivitor.
			// Depending of the tailIsForHandled it will be either merged to the current, either append in the chained list.
			var intermediate = new CollectionUpdater.Update();
			var from = 0;

			for (var i = 0; i < _oldItems.Count; i++)
			{
				var handled = visitor.ReplaceItem(_oldItems[i], _newItems[i], intermediate);
				if (i is 0)
				{
					head = tail = intermediate;
					tailIsForHandled = handled;
					intermediate = new();
				}
				else if (handled == tailIsForHandled)
				{
					intermediate.FlushTo(tail!);
				}
				else
				{
					if (!tailIsForHandled)
					{
						// If the current was not for handled replace, we do have to set its event
						tail!.Event = CreateEvent(from, i - from);
					}

					// Move the cursor to next 
					tail = tail!.Next = intermediate;
					intermediate = new();
					tailIsForHandled = handled;
					from = i;
				}
			}

			if (!tailIsForHandled)
			{
				// If the current was not for handled replace, we do have to set its event
				tail!.Event = CreateEvent(from, _oldItems.Count - from);
			}

			return head!;
		}

		private RichNotifyCollectionChangedEventArgs CreateEvent(int from, int count)
#if NET8_0_OR_GREATER
			=> RichNotifyCollectionChangedEventArgs.ReplaceSome<T>(
#else
			=> RichNotifyCollectionChangedEventArgs.ReplaceSome(
#endif
				_oldItems!.Slice(from, count),
				_newItems!.Slice(from, count),
				Starts + _indexOffset + from,
				_isReplaceOfSameEntities);

		/// <inheritdoc />
		public override string ToString()
			=> $"Replace {_oldItems.Count} items at {Starts}";
	}

	
}
