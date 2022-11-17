using System;
using System.Linq;
using Uno.Extensions.Reactive.Dispatching;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection;

internal interface IBindableCollectionViewSource : IServiceProvider
{
	/// <summary>
	/// Gets the source of the parent data layer, if any.
	/// </summary>
	IBindableCollectionViewSource? Parent { get; }

	event EventHandler<CurrentSourceUpdateEventArgs> CurrentSourceChanging;

	event EventHandler<CurrentSourceUpdateEventArgs> CurrentSourceChanged;

	/// <summary>
	/// Gets the dispatcher to which this collection view source belongs.
	/// </summary>
	/// <remarks>This can be null if this collection belongs to background threads (uncommon).</remarks>
	IDispatcher? Dispatcher { get; }

	/// <summary>
	/// Get a specific facet of this collection.
	/// </summary>
	/// <typeparam name="TFacet">Type of the facet</typeparam>
	/// <returns>The requested facet.</returns>
	/// <exception cref="InvalidOperationException">If the requested facet is not available on this collection.</exception>
	TFacet GetFacet<TFacet>();
}
