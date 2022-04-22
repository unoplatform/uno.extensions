using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections.Tracking;

partial class CollectionAnalyzer
{
	private sealed class _Replace : EntityChange
	{
		/// <inheritdoc />
		public _Replace(int at, int indexOffset)
			: base(at, indexOffset)
		{
		}

		public override RichNotifyCollectionChangedEventArgs ToEvent()
			=> RichNotifyCollectionChangedEventArgs.ReplaceSome(_oldItems, _newItems, Starts + _indexOffset);

		/// <inheritdoc />
		protected override CollectionUpdater.Update ToUpdaterCore(ICollectionUpdaterVisitor visitor)
		{
			if (_oldItems is {Count: 0})
			{
				// Safety case, should never happen
				return new();
			}

			CollectionUpdater.Update? head = default, tail = default;

			var tailIsForHandled = false;
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
					// Some elements are handled by callbacks only and some are not.
					// For handled elements we don't need any event (usually when a nested group is updated).

					if (!tailIsForHandled)
					{
						// If the current was not for handled replace, we do have to set its event
						var count = i - from;
						tail!.Event = RichNotifyCollectionChangedEventArgs.ReplaceSome(
							_oldItems.Slice(from, count),
							_newItems.Slice(from, count),
							Starts + _indexOffset + from);
					}

					// Move the cursor to next 
					tail = tail!.Next = intermediate;
					intermediate = new();
					tailIsForHandled = handled;
					from = i;
				}
			}

			return head!;
		}

		/// <inheritdoc />
		public override string ToString()
			=> $"Replace {_oldItems.Count} items at {Starts}";
	}

	
}
