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
		private readonly ICollectionTrackingVisitor _visitor;
		private readonly List<object> _items;

		public _Remove(int at, ICollectionTrackingVisitor visitor, int capacity)
			: this(at, 0, visitor, capacity)
		{
		}

		public _Remove(int at, int indexOffset, ICollectionTrackingVisitor visitor, int capacity)
			: base(at, capacity)
		{
			_indexOffset = indexOffset;
			_visitor = visitor;
			_items = new List<object>(capacity);
		}

		public void Append(object item)
		{
			_visitor.RemoveItem(item, this);
			_items.Add(item);
		}

		public override RichNotifyCollectionChangedEventArgs ToEvent()
			=> RichNotifyCollectionChangedEventArgs.RemoveSome(_items, Starts + _indexOffset);

		/// <inheritdoc />
		public override string ToString()
			=> $"Remove {_items.Count} @ {Starts} (b: {_before.Count} / a: {_after.Count})";
	}
}
