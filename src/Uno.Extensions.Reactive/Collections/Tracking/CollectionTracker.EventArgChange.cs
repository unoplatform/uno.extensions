using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

partial class CollectionTracker
{
	[DebuggerDisplay("Event: {_args.Action} {_args.OldItems?.Count}/{_args.NewItems?.Count} @ {_args.OldStartingIndex}/{_args.NewStartingIndex} (b: {_before.Count} / a: {_after.Count})")]
	private class _EventArgChange : ChangeBase
	{
		private readonly RichNotifyCollectionChangedEventArgs _args;

		public _EventArgChange(RichNotifyCollectionChangedEventArgs args)
			: base(at: -1, capacity: 4)
		{
			_args = args;
		}

		public _EventArgChange(RichNotifyCollectionChangedEventArgs args, IList items, Action<object, ICollectionTrackingCallbacks> visit)
			: base(at: -1, capacity: items.Count)
		{
			_args = args;

			foreach (var item in items)
			{
				visit(item, this);
			}
		}

		public override RichNotifyCollectionChangedEventArgs ToEvent()
			=> _args;

		/// <inheritdoc />
		public override string ToString()
			=> $"Event: {_args.Action} {_args.OldItems?.Count}/{_args.NewItems?.Count} @ {_args.OldStartingIndex}/{_args.NewStartingIndex} (b: {_before.Count} / a: {_after.Count})";
	}
}
