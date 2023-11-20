using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Collections.Tracking;

partial class CollectionAnalyzer
{
	private sealed class _Remove<T> : Change<T>
	{
		private readonly int _indexOffset;
		private readonly List<T> _items;

		public _Remove(int at, int capacity)
			: this(at, 0, capacity)
		{
		}

		public _Remove(int at, int indexOffset, int capacity)
			: base(at)
		{
			_indexOffset = indexOffset;
			_items = new List<T>(capacity);
		}

		public void Append(T item)
		{
			_items.Add(item);
		}

		public override RichNotifyCollectionChangedEventArgs ToEvent()
			=> RichNotifyCollectionChangedEventArgs.RemoveSome<T>(_items, Starts + _indexOffset);

		/// <inheritdoc />
		protected internal override void Visit(ICollectionChangeSetVisitor<T> visitor)
			=> visitor.Remove(_items, Starts + _indexOffset);

		/// <inheritdoc />
		protected override CollectionUpdater.Update ToUpdaterCore(ICollectionUpdaterVisitor visitor)
			=> Visit(ToEvent(), visitor);

		internal static CollectionUpdater.Update Visit(RichNotifyCollectionChangedEventArgs args, ICollectionUpdaterVisitor visitor)
		{
			var callback = new CollectionUpdater.Update(args);
			var items = args.OldItems!;

			for (var i = 0; i < items.Count; i++)
			{
				visitor.RemoveItem(items[i], callback);
			}

			return callback;
		}

		/// <inheritdoc />
		public override string ToString()
			=> $"Remove {_items.Count} items at {Starts}";
	}
}
