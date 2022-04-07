using System;
using System.Collections;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

/// <summary>
/// The the null pattern implementation of <see cref="ICollectionTrackingVisitor"/>
/// </summary>
internal class NullCollectionTrackingVisitor : ICollectionTrackingVisitor
{
	/// <summary>
	/// Singleton instance
	/// </summary>
	public static ICollectionTrackingVisitor Instance { get; } = new NullCollectionTrackingVisitor();

	private NullCollectionTrackingVisitor() { }

	/// <inheritdoc />
	public void AddItem(object item, ICollectionTrackingCallbacks callbacks) { }

	/// <inheritdoc />
	public void SameItem(object original, object updated, ICollectionTrackingCallbacks callbacks) { }

	/// <inheritdoc />
	public bool ReplaceItem(object original, object updated, ICollectionTrackingCallbacks callbacks) => false;

	/// <inheritdoc />
	public void RemoveItem(object item, ICollectionTrackingCallbacks callbacks) { }

	/// <inheritdoc />
	public void Reset(IList oldItems, IList newItems, ICollectionTrackingCallbacks callbacks) { }
}
