using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Uno.Extensions.Collections.Facades.Differential;

namespace Uno.Extensions.Collections.Tracking;

partial class CollectionAnalyzer
{
	private sealed class _Add<T> : Change<T>
	{
		private readonly int _indexOffset;
		private readonly List<T> _items;

		public _Add(int at, int capacity)
			: this(at, 0, capacity)
		{
		}

		public _Add(int at, int indexOffset, int capacity)
			: base(at)
		{
			_indexOffset = indexOffset;
			_items = new List<T>(capacity);
		}

		public void Append(T item)
		{
			_items.Add(item);
			Ends++;
		}

		public override RichNotifyCollectionChangedEventArgs ToEvent()
			=> RichNotifyCollectionChangedEventArgs.AddSome<T>(_items, Starts + _indexOffset);

		/// <inheritdoc />
		protected internal override void Visit(ICollectionChangeSetVisitor<T> visitor)
			=> visitor.Add(_items, Starts + _indexOffset);

		/// <inheritdoc />
		protected override CollectionUpdater.Update ToUpdaterCore(ICollectionUpdaterVisitor visitor)
			=> Visit(ToEvent(), visitor);

		internal static CollectionUpdater.Update Visit(RichNotifyCollectionChangedEventArgs args, ICollectionUpdaterVisitor visitor)
		{
			var callback = new CollectionUpdater.Update(args);
			var items = args.NewItems!;

			for (var i = 0; i < items.Count; i++)
			{
				visitor.AddItem(items[i], callback);
			}

			return callback;
		}

		/// <inheritdoc />
		public override string ToString()
			=> $"Add {_items.Count} items at {Starts}";
	}
}
