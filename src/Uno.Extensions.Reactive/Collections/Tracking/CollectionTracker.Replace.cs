using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

partial class CollectionTracker
{
	[DebuggerDisplay("Replace {(IsSilent?\"-SILENT-\":\"\")} {_oldItems.Count} @ {Starts} (b: {_before.Count} / a: {_after.Count})")]
	private sealed class _Replace : ChangeBase
	{
		private readonly int _indexOffset;
		private readonly List<object> _oldItems = new List<object>();
		private readonly List<object> _newItems = new List<object>();

		public bool IsSilent { get; }

		public new _Replace? Next
		{
			get => base.Next as _Replace;
			set => base.Next = value;
		}

		public _Replace(object oldItem, object newItem, int at, int indexOffset, bool isSilent, CallbacksBuffer callbacks)
			: base(at, capacity: 4)
		{
			_indexOffset = indexOffset;
			Ends = at;
			IsSilent = isSilent;

			Append(oldItem, newItem, callbacks);
		}

		public void Append(object oldItem, object newItem, CallbacksBuffer callbacks)
		{
			_oldItems.Add(oldItem);
			_newItems.Add(newItem);
			callbacks.FlushTo(_before, _after);
			Ends++;

			// Try to merge with the next if possible
			// Note: The next must be a _Replace when in 'edition' mode.
			var next = Next; // Use local variable to avoid multiple cast
			while (next != null && IsSilent == next.IsSilent && Ends == next.Starts)
			{
				_oldItems.AddRange(next._oldItems);
				_newItems.AddRange(next._newItems);
				_before.AddRange(next._before);
				_after.AddRange(next._after);
				Ends = next.Ends;
				Next = next.Next;

				next = Next; //update local variable
			}
		}

		public override RichNotifyCollectionChangedEventArgs ToEvent()
			=> RichNotifyCollectionChangedEventArgs.ReplaceSome(_oldItems, _newItems, Starts + _indexOffset);

		protected override void RaiseTo(CollectionChangesQueue.IHandler handler, bool silently)
		{
			if (IsSilent || silently)
			{
				handler.ApplySilently(ToEvent());
			}
			else
			{
				handler.Raise(ToEvent());
			}
		}

		/// <inheritdoc />
		public override string ToString()
			=> $"Replace {(IsSilent ?"-SILENT-":"")} {_oldItems.Count} @ {Starts} (b: {_before.Count} / a: {_after.Count})";

		public class CallbacksBuffer : ICollectionTrackingCallbacks
		{
			private readonly List<object> _before = new List<object>();
			private readonly List<object> _after = new List<object>();

			void ICollectionTrackingCallbacks.Prepend(BeforeCallback callback) => _before.Add(callback);
			void ICollectionTrackingCallbacks.Prepend(ICompositeCallback child) => _before.Add(child);
			void ICollectionTrackingCallbacks.Append(AfterCallback callback) => _after.Add(callback);
			void ICollectionTrackingCallbacks.Append(ICompositeCallback child) => _after.Add(child);

			public void FlushTo(List<object> before, List<object> after)
			{
				before.AddRange(_before);
				after.AddRange(_after);

				_before.Clear();
				_after.Clear();
			}
		}
	}
}
