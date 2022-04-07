using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Data;
using nVentive.Umbrella.Collections;
using nVentive.Umbrella.Collections.Tracking;
using Umbrella.Presentation.Feeds.Collections._BindableCollection.Facets;
using Umbrella.Presentation.Feeds.Collections._BindableCollection.Data;

namespace Umbrella.Presentation.Feeds.Collections._BindableCollection
{
	/// <summary>
	/// The strategy used by a <see cref="DataLayerHolder"/> to maintain a layer of data.
	/// </summary>
	internal interface IBindableCollectionDataLayerStrategy
	{
		/// <summary>
		/// Creates the view
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		(
			DifferentialObservableCollection source,  // The underlying collection used for the tracking
			ICollectionView view, // The main view used for this layer
			IEnumerable<object> facets // The facets that are accessible for the view
		) 
		CreateView(IBindableCollectionViewSource source);

		/// <summary>
		/// Creates a new context object for to update the collection
		/// </summary>
		IUpdateContext CreateUpdateContext(VisitorType type, TrackingMode mode);

		/// <summary>
		/// Gets the collection tracker to use for this layer
		/// </summary>
		ILayerTracker GetTracker(IBindableCollectionViewSource source, IUpdateContext context);

		/// <summary>
		/// Gets a strategy to use for a sub layer of data.
		/// </summary>
		IBindableCollectionDataLayerStrategy CreateSubLayer();
	}
}
