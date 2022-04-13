using System;
using System.Linq;
using nVentive.Umbrella.Collections;
using nVentive.Umbrella.Collections.Tracking;

namespace Umbrella.Presentation.Feeds.Collections._BindableCollection.Data
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
		CollectionChangesQueue GetChanges(IObservableCollectionSnapshot? oldItems, IObservableCollectionSnapshot newItems, bool shouldUseSmartTracking = true);

		/// <summary>
		/// Gets the effective changes when receiving a collection change from the source
		/// </summary>
		CollectionChangesQueue GetChanges(RichNotifyCollectionChangedEventArgs arg, bool shouldUseSmartTracking = true);
	}
}
