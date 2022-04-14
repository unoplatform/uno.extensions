using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

partial class CollectionTracker
{
	[DebuggerDisplay("Remove {_items.Count} @ {Starts} (b: {_before.Count} / a: {_after.Count})")]
	private sealed class _Remove : ChangeBase
	{
		private readonly int _indexOffset;
		private readonly List<object> _items;

		public _Remove(int at, int capacity)
			: this(at, 0, capacity)
		{
		}

		public _Remove(int at, int indexOffset, int capacity)
			: base(at)
		{
			_indexOffset = indexOffset;
			_items = new List<object>(capacity);
		}

		public void Append(object item)
		{
			_items.Add(item);
		}

		public override RichNotifyCollectionChangedEventArgs ToEvent()
			=> RichNotifyCollectionChangedEventArgs.RemoveSome(_items, Starts + _indexOffset);

		/// <inheritdoc />
		protected override CollectionChangesQueue.Node VisitCore(ICollectionTrackingVisitor visitor)
			=> Visit(ToEvent(), visitor);

		internal static CollectionChangesQueue.Node Visit(RichNotifyCollectionChangedEventArgs args, ICollectionTrackingVisitor visitor)
		{
			var callback = new CollectionChangesQueue.Node(args);
			var items = args.OldItems;

			for (var i = 0; i < items.Count; i++)
			{
				visitor.RemoveItem(items[i], callback);
			}

			return callback;
		}

		/// <inheritdoc />
		public override string ToString()
			=> $"Remove {_items.Count} @ {Starts}s";
	}
}
