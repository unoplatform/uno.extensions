using System;
using System.Linq;

namespace Umbrella.Presentation.Feeds.Collections._BindableCollection
{
	/// <summary>
	/// Represents the structure of the data which is handle by a <see cref="BindableCollection"/>
	/// </summary>
	internal interface IBindableCollectionDataStructure
	{
		/// <summary>
		/// Gets the strategy to use for the first layer of data
		/// </summary>
		IBindableCollectionDataLayerStrategy GetRoot();
	}
}
