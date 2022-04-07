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
		private readonly ICollectionTrackingVisitor _visitor;
		private readonly List<object> _items;

		public _Add(int at, ICollectionTrackingVisitor visitor, int capacity)
			: this(at, 0, visitor, capacity)
		{
		}

		public _Add(int at, int indexOffset, ICollectionTrackingVisitor visitor, int capacity)
			: base(at, capacity)
		{
			_indexOffset = indexOffset;
			_visitor = visitor;
			_items = new List<object>(capacity);
		}

		public void Append(object item)
		{
			_visitor.AddItem(item, this);
			_items.Add(item);
			Ends++;
		}

		public override RichNotifyCollectionChangedEventArgs ToEvent()
			=> RichNotifyCollectionChangedEventArgs.AddSome(_items, Starts + _indexOffset);

		/// <inheritdoc />
		public override string ToString()
			=> $"Add {_items.Count} @ {Starts} (b: {_before.Count} / a: {_after.Count})";
	}
}
