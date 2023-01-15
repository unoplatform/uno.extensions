using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Collections.Tracking;

/// <summary>
/// A visitor which can be used when tracking changes between 2 collections.
/// </summary>
internal interface ICollectionChangeSetVisitor<in T>
{
	/// <summary>
	/// Invoked when an item is added in the target collection.
	/// </summary>
	/// <param name="items">The added items</param>
	/// <param name="index">The index where the items have been added.</param>
	void Add(IReadOnlyList<T> items, int index);

	/// <summary>
	/// Invoked when an Equals item appears in both previous and target collections.
	/// </summary>
	/// <remarks>No collection changed event is created for this item.</remarks>
	/// <param name="original">The instance of the item in the previous collection.</param>
	/// <param name="updated">The instance of the item in the target collection.</param>
	/// <param name="index">The index where the items have been kept.</param>
	void Same(IReadOnlyList<T> original, IReadOnlyList<T> updated, int index);

	/// <summary>
	/// Invoked when a new version of an item is present in the target collection.
	/// </summary>
	/// <param name="original">The previous version</param>
	/// <param name="updated">The updated version</param>
	/// <param name="index">The index where the items have been replaced.</param>
	void Replace(IReadOnlyList<T> original, IReadOnlyList<T> updated, int index);

	/// <summary>
	/// Invoked when items have been moved in the collection.
	/// </summary>
	/// <param name="items">The moved items.</param>
	/// <param name="fromIndex">The index where the items was.</param>
	/// <param name="toIndex">The index where the items are.</param>
	void Move(IReadOnlyList<T> items, int fromIndex, int toIndex);

	/// <summary>
	/// Invoked when an item is removed in the target collection.
	/// </summary>
	/// <param name="items">The removed item</param>
	/// <param name="index">The index where the items have been removed.</param>
	void Remove(IReadOnlyList<T> items, int index);

	/// <summary>
	/// Invoked when a reset event is raised instead of properly tracking the changes between the collections.
	/// </summary>
	/// <param name="oldItems">A list of all the items removed from the target collection</param>
	/// <param name="newItems">A list of all the new items present in the target collection</param>
	void Reset(IReadOnlyList<T> oldItems, IReadOnlyList<T> newItems);
}
