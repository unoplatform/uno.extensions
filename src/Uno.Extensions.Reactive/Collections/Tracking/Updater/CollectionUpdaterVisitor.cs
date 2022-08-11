using System;
using System.Collections;
using System.Linq;

namespace Uno.Extensions.Collections.Tracking;

/// <summary>
/// Base class to ease implementation of <see cref="ICollectionUpdaterVisitor"/>.
/// </summary>
internal abstract class CollectionUpdaterVisitor : ICollectionUpdaterVisitor
{
	public static ICollectionUpdaterVisitor Null { get; } = new NullVisitor();

	/// <inheritdoc />
	public virtual void AddItem(object? item, ICollectionUpdateCallbacks callbacks) { }

	/// <inheritdoc />
	public virtual void SameItem(object? original, object? updated, ICollectionUpdateCallbacks callbacks) { }

	/// <inheritdoc />
	public virtual bool ReplaceItem(object? original, object? updated, ICollectionUpdateCallbacks callbacks) => false;

	/// <inheritdoc />
	public virtual void RemoveItem(object? item, ICollectionUpdateCallbacks callbacks) { }

	/// <inheritdoc />
	public virtual void Reset(IList oldItems, IList newItems, ICollectionUpdateCallbacks callbacks) { }

	private class NullVisitor : CollectionUpdaterVisitor
	{
	}
}
