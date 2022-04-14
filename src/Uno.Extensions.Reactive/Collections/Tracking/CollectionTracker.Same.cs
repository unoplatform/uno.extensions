//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;

//namespace nVentive.Umbrella.Collections.Tracking;

//partial class CollectionTracker
//{
//	[DebuggerDisplay("Same - i.e. Callbacks to update instances (b: {_before.Count} / a: {_after.Count})")]
//	private sealed class _Same : ChangeBase
//	{
//		private readonly int _indexOffset;
//		private readonly List<object> _oldItems = new();
//		private readonly List<object> _newItems = new();

//		public _Same(int capacity)
//			: base(at: -1, capacity: capacity) // We don't use the visitor for a move
//		{
//		}

//		public new _Same? Next
//		{
//			get => base.Next as _Same;
//			set => base.Next = value;
//		}

//		public _Same(object oldItem, object newItem, int at, int indexOffset)
//			: base(at, capacity: 4)
//		{
//			_indexOffset = indexOffset;
//			Ends = at;

//			Append(oldItem, newItem);
//		}

//		public void Append(object oldItem, object newItem)
//		{
//			_oldItems.Add(oldItem);
//			_newItems.Add(newItem);
//			Ends++;

//			// Try to merge with the next if possible
//			// Note: The next must be a _Replace when in 'edition' mode.
//			var next = Next; // Use local variable to avoid multiple cast
//			while (next != null && Ends == next.Starts)
//			{
//				_oldItems.AddRange(next._oldItems);
//				_newItems.AddRange(next._newItems);
//				Ends = next.Ends;
//				Next = next.Next;

//				next = Next; //update local variable
//			}
//		}

//		public override RichNotifyCollectionChangedEventArgs ToEvent()
//			=> throw new NotSupportedException("Same changes cannot be converted to event (callbacks only).");

//		//protected override void RaiseTo(CollectionChangesQueue.IHandler handler, bool silently)
//		//{
//		//	// Nothing to notify to handler, callbacks only !
//		//}

//		/// <inheritdoc />
//		public override string ToString()
//			=> "Same - Same instance that may be require a deep diff to detect change from previous snapshot";
//	}
//}
using System;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

partial class CollectionTracker
{
	private sealed class _Same : EntityChangeBase
	{
		/// <inheritdoc />
		public _Same(int at, int indexOffset)
			: base(at, indexOffset)
		{
		}

		public override RichNotifyCollectionChangedEventArgs? ToEvent()
		{
			return null;
		}

		/// <inheritdoc />
		protected override CollectionChangesQueue.Node VisitCore(ICollectionTrackingVisitor visitor)
		{
			var callback = new CollectionChangesQueue.Node(ToEvent());

			for (var i = 0; i < _oldItems.Count; i++)
			{
				visitor.SameItem(_oldItems[i], _newItems[i], callback);
			}

			return callback;
		}

		/// <inheritdoc />
		public override string ToString()
			=> "Same - Same instance that may be require a deep diff to detect change from previous snapshot";
	}
}
