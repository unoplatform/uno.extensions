using System;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation.Collections;

namespace Umbrella.Presentation.Feeds.Collections._BindableCollection.Facets
{
	[Flags]
	internal enum ObservableCollectionKind
	{
		/// <summary>
		/// Is not an observble collection
		/// </summary>
		None = 0,

		/// <summary>
		/// A collection which implements <see cref="INotifyCollectionChanged"/>.
		/// </summary>
		Collection = 1,

		/// <summary>
		/// A collection which implements <see cref="IObservableVector{T}"/>
		/// </summary>
		Vector = 2,

		/// <summary>
		/// Implements all way to notify changes on a collection
		/// </summary>
		All = Collection | Vector
	}
}