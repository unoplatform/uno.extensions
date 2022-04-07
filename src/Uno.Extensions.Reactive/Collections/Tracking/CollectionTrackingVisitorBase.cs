using System;
using System.Collections;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

/// <summary>
/// Base class to ease implementation of <see cref="ICollectionTrackingVisitor"/>.
/// </summary>
internal abstract class CollectionTrackingVisitorBase : ICollectionTrackingVisitor
{
	/// <inheritdoc />
	public virtual void AddItem(object item, ICollectionTrackingCallbacks callbacks) { }

	/// <inheritdoc />
	public virtual void SameItem(object original, object updated, ICollectionTrackingCallbacks callbacks) { }

	/// <inheritdoc />
	public virtual bool ReplaceItem(object original, object updated, ICollectionTrackingCallbacks callbacks) => false;

	/// <inheritdoc />
	public virtual void RemoveItem(object item, ICollectionTrackingCallbacks callbacks) { }

	/// <inheritdoc />
	public virtual void Reset(IList oldItems, IList newItems, ICollectionTrackingCallbacks callbacks) { }
}
