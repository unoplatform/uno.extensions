using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

partial class CollectionTracker
{
	[DebuggerDisplay("Add {_items.Count} @ {Starts} (b: {_before.Count} / a: {_after.Count})")]
	private sealed class _Add : ChangeBase
	{
		private readonly int _indexOffset;
		private readonly List<object> _items;

		public _Add(int at, int capacity)
			: this(at, 0, capacity)
		{
		}

		public _Add(int at, int indexOffset, int capacity)
			: base(at)
		{
			_indexOffset = indexOffset;
			_items = new List<object>(capacity);
		}

		public void Append(object item)
		{
			_items.Add(item);
			Ends++;
		}

		public override RichNotifyCollectionChangedEventArgs ToEvent()
			=> RichNotifyCollectionChangedEventArgs.AddSome(_items, Starts + _indexOffset);

		/// <inheritdoc />
		protected override CollectionChangesQueue.Node VisitCore(ICollectionTrackingVisitor visitor)
			=> Visit(ToEvent(), visitor);

		internal static CollectionChangesQueue.Node Visit(RichNotifyCollectionChangedEventArgs args, ICollectionTrackingVisitor visitor)
		{
			var callback = new CollectionChangesQueue.Node(args);
			var items = args.NewItems;

			for (var i = 0; i < items.Count; i++)
			{
				visitor.AddItem(items[i], callback);
			}

			return callback;
		}

		/// <inheritdoc />
		public override string ToString()
			=> $"Add {_items.Count} @ {Starts}";
	}
}
