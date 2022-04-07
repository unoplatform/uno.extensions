using System;
using System.Diagnostics;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

partial class CollectionTracker
{
	[DebuggerDisplay("Same - i.e. Callbacks to update instances (b: {_before.Count} / a: {_after.Count})")]
	private sealed class _Same : ChangeBase
	{
		public _Same(int capacity)
			: base(at: -1, capacity: capacity) // We don't use the visitor for a move
		{
		}

		public bool HasCallbacks => _before.Count > 0 || _after.Count > 0;

		public override RichNotifyCollectionChangedEventArgs ToEvent()
			=> throw new NotSupportedException("Same changes cannot be converted to event (callbacks only).");

		protected override void RaiseTo(CollectionChangesQueue.IHandler handler, bool silently)
		{
			// Nothing to notify to handler, callbacks only !
		}

		/// <inheritdoc />
		public override string ToString()
			=> $"Same - i.e. Callbacks to update instances (b: {_before.Count} / a: {_after.Count})";
	}
}
