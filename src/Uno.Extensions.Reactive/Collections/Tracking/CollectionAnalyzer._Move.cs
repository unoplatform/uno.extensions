using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Uno.Extensions.Collections.Tracking;

partial class CollectionAnalyzer
{
	private sealed class _Move<T> : Change<T>
	{
		private readonly int _to;
		private readonly int _indexOffset;
		private readonly List<T> _items;

		public _Move(int from, int to, int capacity)
			: this(from, to, 0, capacity)
		{
		}

		public _Move(int from, int to, int indexOffset, int capacity)
			: base(from) // We don't use the visitor for a move
		{
			_to = to;
			_indexOffset = indexOffset;
			_items = new(capacity);
		}

		public void Append(T item)
		{
			_items.Add(item);
			Ends++;
		}

		public override RichNotifyCollectionChangedEventArgs ToEvent()
			=> RichNotifyCollectionChangedEventArgs.MoveSome<T>(_items, Starts + _indexOffset, _to + _indexOffset);

		/// <inheritdoc />
		protected internal override void Visit(ICollectionChangeSetVisitor<T> visitor)
			=> visitor.Move(_items, Starts + _indexOffset, _to + _indexOffset);

		/// <inheritdoc />
		protected override CollectionUpdater.Update ToUpdaterCore(ICollectionUpdaterVisitor visitor)
			=> new(ToEvent());

		/// <inheritdoc />
		public override string ToString()
			=> $"Move {_items.Count} items from {Starts} to {_to}";
	}
}
