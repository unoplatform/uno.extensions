using System;
using System.Collections;
using System.Linq;
using nVentive.Umbrella.Collections;
using nVentive.Umbrella.Collections.Tracking;

namespace Umbrella.Presentation.Feeds.Collections._BindableCollection.Data
{
	internal sealed class DataLayerTracker : ILayerTracker
	{
		private readonly CollectionTracker _diffAnalyzer;
		private readonly ICollectionTrackingVisitor _visitor;

		public IUpdateContext Context { get; }

		public DataLayerTracker(
			IUpdateContext context,
			CollectionTracker diffAnalyzer, 
			ICollectionTrackingVisitor visitor)
		{
			Context = context;
			_diffAnalyzer = diffAnalyzer;
			_visitor = visitor;
		}

		/// <inheritdoc />
		public CollectionChangesQueue GetChanges(IObservableCollectionSnapshot? oldItems, IObservableCollectionSnapshot newItems, bool shouldUseSmartTracking = true)
		{
			var mode = Context.Mode;
			if (oldItems is null 
				|| mode == TrackingMode.Reset
				|| (mode == TrackingMode.Auto && !shouldUseSmartTracking)
				|| Context.HasReachedLimit)
			{
				return _diffAnalyzer.GetReset(oldItems, newItems, _visitor);
			}
			else
			{
				var visitor = new CounterVisitor(Context, _visitor);
				var changes = _diffAnalyzer.GetChanges(oldItems, newItems, visitor);

				return changes;
			}
		}	

		/// <inheritdoc />
		public CollectionChangesQueue GetChanges(RichNotifyCollectionChangedEventArgs arg, bool shoudlUseSmartTracking = true)
		{
			// Note: we do not use the _counter on changes. As we use a differential collection, 
			//		 we cannot convert the event to a single reset (we don't have the final collection state)

			return _diffAnalyzer.GetChanges(arg, _visitor);
		}

		private class CounterVisitor : ICollectionTrackingVisitor
		{
			private readonly ICollectionTrackingVisitor _inner;
			private readonly IUpdateContext _counter;

			public CounterVisitor(IUpdateContext counter, ICollectionTrackingVisitor inner)
			{
				_counter = counter;
				_inner = inner;
			}

			/// <inheritdoc />
			public void AddItem(object item, ICollectionTrackingCallbacks callbacks)
			{
				_counter.NotifyAdd();
				_inner.AddItem(item, callbacks);
			}

			/// <inheritdoc />
			public void SameItem(object original, object updated, ICollectionTrackingCallbacks callbacks)
			{
				_counter.NotifySameItem();
				_inner.SameItem(original, updated, callbacks);
			}

			/// <inheritdoc />
			public bool ReplaceItem(object original, object updated, ICollectionTrackingCallbacks callbacks)
			{
				_counter.NotifyReplace();
				return _inner.ReplaceItem(original, updated, callbacks);
			}

			/// <inheritdoc />
			public void RemoveItem(object item, ICollectionTrackingCallbacks callbacks)
			{
				_counter.NotifyRemove();
				_inner.RemoveItem(item, callbacks);
			}

			/// <inheritdoc />
			public void Reset(IList oldItems, IList newItems, ICollectionTrackingCallbacks callbacks)
			{
				_counter.NotifyReset();
				_inner.Reset(oldItems, newItems, callbacks);
			}
		}
	}
}
