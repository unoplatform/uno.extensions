using System;
using System.Linq;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Tracking;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Data
{
	/// <summary>
	/// The collection tracker to use to maintain a layer of data
	/// </summary>
	internal interface ILayerTracker
	{
		IUpdateContext Context { get; }

		/// <summary>
		/// Gets the changes when updating the source collection
		/// </summary>
		CollectionUpdater GetChanges(IObservableCollectionSnapshot? oldItems, IObservableCollectionSnapshot newItems, bool shouldUseSmartTracking = true);

		/// <summary>
		/// Gets the effective changes when receiving a collection change from the source
		/// </summary>
		CollectionUpdater GetChanges(RichNotifyCollectionChangedEventArgs arg, bool shouldUseSmartTracking = true);
	}
}
