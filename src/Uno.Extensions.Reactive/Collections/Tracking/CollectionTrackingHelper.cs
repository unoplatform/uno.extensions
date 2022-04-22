using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Uno.Extensions.Collections.Tracking;

internal static class CollectionTrackingHelper
{ 
	/// <summary>
	/// Determines the set of changes between two snapshot of an <see cref="IList{T}"/>
	/// </summary>
	/// <typeparam name="T">Type of the items in collections</typeparam>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <param name="itemComparer">
	/// Comparer used to detect multiple versions of the **same entity (T)**, or null to use default.
	/// <remarks>Usually this should only compare the ID of the entities in order to properly track the changes made on an entity.</remarks>
	/// <remarks>For better performance, prefer provide null instead of <see cref="EqualityComparer{T}.Default"/>.</remarks>
	/// </param>
	/// <param name="itemVersionComparer">
	/// Comparer used to detect multiple instance of the **same version** of the **same entity (T)**, or null to rely only on the <paramref name="itemComparer"/> (not recommended).
	/// <remarks>
	/// This comparer will determine if two instances of the same entity (which was considered as equals by the <paramref name="itemComparer"/>),
	/// are effectively equals or not (i.e. same version or not).
	/// <br />
	/// * If **Equals**: it's 2 **instances** of the **same version** of the **same entity** (all properties are equals), so we don't have to raise a <see cref="NotifyCollectionChangedAction.Replace"/>.<br />
	/// * If **NOT Equals**: it's 2 **distinct versions** of the **same entity** (not all properties are equals) and we have to raise a 'Replace' to re-evaluate those properties.
	/// </remarks>
	/// </param>
	/// <returns>A list of changes that have to be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	public static ICollection<RichNotifyCollectionChangedEventArgs> GetChanges<T>(
		IList<T> oldItems,
		IList<T> newItems,
		IEqualityComparer<T> itemComparer,
		IEqualityComparer<T>? itemVersionComparer)
	{
		var tracker = new CollectionAnalyzer<T>(itemComparer, itemVersionComparer);
		var changes = tracker.GetChanges(oldItems, newItems);

		return changes.ToCollectionChanges();
	}

	/// <summary>
	/// Determines the set of changes between two snapshot of an <see cref="IList{T}"/>
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <param name="itemComparer">
	/// Comparer used to detect multiple versions of the **same entity (T)**, or null to use default.
	/// <remarks>Usually this should only compare the ID of the entities in order to properly track the changes made on an entity.</remarks>
	/// <remarks>For better performance, prefer provide null instead of <see cref="EqualityComparer{T}.Default"/>.</remarks>
	/// </param>
	/// <param name="itemVersionComparer">
	/// Comparer used to detect multiple instance of the **same version** of the **same entity (T)**, or null to rely only on the <paramref name="itemComparer"/> (not recommended).
	/// <remarks>
	/// This comparer will determine if two instances of the same entity (which was considered as equals by the <paramref name="itemComparer"/>),
	/// are effectively equals or not (i.e. same version or not).
	/// <br />
	/// * If **Equals**: it's 2 **instances** of the **same version** of the **same entity** (all properties are equals), so we don't have to raise a <see cref="NotifyCollectionChangedAction.Replace"/>.<br />
	/// * If **NOT Equals**: it's 2 **distinct versions** of the **same entity** (not all properties are equals) and we have to raise a 'Replace' to re-evaluate those properties.
	/// </remarks>
	/// </param>
	/// <returns>A list of changes that have to be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	public static ICollection<RichNotifyCollectionChangedEventArgs> GetChanges(
		IList oldItems,
		IList newItems,
		IEqualityComparer itemComparer,
		IEqualityComparer? itemVersionComparer)
	{
		var tracker = new CollectionAnalyzer(itemComparer, itemVersionComparer);
		var changes = tracker.GetChanges(oldItems, newItems);

		return changes.ToCollectionChanges();
	}
}
