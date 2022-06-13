using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection;

internal interface IBindableCollectionViewSource : IServiceProvider
{
	/// <summary>
	/// Gets the source of the parent data layer, if any.
	/// </summary>
	IBindableCollectionViewSource? Parent { get; }

	event EventHandler<CurrentSourceUpdateEventArgs> CurrentSourceChanging;

	event EventHandler<CurrentSourceUpdateEventArgs> CurrentSourceChanged;

	TFacet GetFacet<TFacet>();
}
