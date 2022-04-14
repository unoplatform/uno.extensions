using System;
using System.Linq;

namespace Uno.Extensions.Collections.Tracking;

/// <summary>
/// Set of callbacks that are part of a <see cref="CollectionUpdater"/>.
/// </summary>
internal interface ICollectionUpdateCallbacks
{
	/// <summary>
	/// Adds a callback which will be invoked before raising the collection changed event
	/// </summary>
	void Prepend(BeforeCallback callback);

	/// <summary>
	/// Adds a <see cref="ICompositeCallback"/> which will be invoked before raising the collection changed event
	/// </summary>
	void Prepend(ICompositeCallback child);

	/// <summary>
	/// Adds a callback which will be invoked after raising the collection changed event
	/// </summary>
	void Append(AfterCallback callback);

	/// <summary>
	/// Adds a <see cref="ICompositeCallback"/> which will be invoked after raising the collection changed event
	/// </summary>
	void Append(ICompositeCallback child);
}
