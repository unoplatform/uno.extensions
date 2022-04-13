using System;
using System.Linq;
using nVentive.Umbrella.Collections;
using Umbrella.Presentation.Feeds.Collections._BindableCollection.Facets;

namespace Umbrella.Presentation.Feeds.Collections._BindableCollection.Data
{
	/// <summary>
	/// The state holder of a layer of data
	/// </summary>
	internal interface ILayerHolder
	{
		/// <summary>
		/// Schedule an action on threading context to which this holder is associated to.
		/// </summary>
		void Schedule(Action action);

		/// <summary>
		/// Gets the reference collection of items of this layer
		/// </summary>
		CollectionFacet Items { get; }

		/// <summary>
		/// Creates a holder for a sub layer of data
		/// </summary>
		/// <param name="subItems">Current source of Sub items</param>
		/// <param name="context">The context of the update which drove to create a new holder for a sub layer</param>
		(DataLayer holder, DataLayerUpdate initializer) CreateSubLayer(IObservableCollection subItems, IUpdateContext context);

		/// <summary>
		/// Gets a facet managed by this holder
		/// </summary>
		TFacet GetFacet<TFacet>();
	}
}
