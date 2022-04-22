using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection
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
