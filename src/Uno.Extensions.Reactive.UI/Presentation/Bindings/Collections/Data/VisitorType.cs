using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Data
{
	internal enum VisitorType
	{
		/// <summary>
		/// Initializing the collection
		/// </summary>
		InitializeCollection,

		/// <summary>
		/// A new version of the collection is being published
		/// </summary>
		UpdateCollection,

		/// <summary>
		/// The current collection is raising a collection changed
		/// </summary>
		CollectionChanged
	}
}