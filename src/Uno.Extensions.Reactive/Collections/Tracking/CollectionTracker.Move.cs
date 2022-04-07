using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

partial class CollectionTracker
{
	[DebuggerDisplay("Move {_items.Count} from {Starts} to {_to} (b: {_before.Count} / a: {_after.Count})")]
	private sealed class _Move : ChangeBase
	{
		private readonly int _to;
		private readonly int _indexOffset;
		private readonly List<object> _items;

		public _Move(int from, int to, int capacity)
			: this(from, to, 0, capacity)
		{
		}

		public _Move(int from, int to, int indexOffset, int capacity)
			: base(from, capacity) // We don't use the visitor for a move
		{
			_to = to;
			_indexOffset = indexOffset;
			_items = new List<object>(capacity);
		}

		public void Append(object item)
		{
			_items.Add(item);
			Ends++;
		}

		public override RichNotifyCollectionChangedEventArgs ToEvent()
			=> RichNotifyCollectionChangedEventArgs.MoveSome(_items, Starts + _indexOffset, _to + _indexOffset);

		/// <inheritdoc />
		public override string ToString()
			=> $"Move {_items.Count} from {Starts} to {_to} (b: {_before.Count} / a: {_after.Count})";
	}
}
