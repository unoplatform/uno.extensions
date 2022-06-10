using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Reactive.Collections;

namespace Uno.Extensions.Collections.Tracking;

internal static class CollectionTrackingHelper
{ 
	/// <summary>
	/// Determines the set of changes between two snapshot of an <see cref="IList{T}"/>
	/// </summary>
	/// <typeparam name="T">Type of the items in collections</typeparam>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <param name="comparer">The set of comparer to use to track items</param>
	/// <returns>A list of changes that have to be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	public static ICollection<RichNotifyCollectionChangedEventArgs> GetChanges<T>(
		IList<T> oldItems,
		IList<T> newItems,
		ItemComparer<T> comparer)
	{
		var tracker = new CollectionAnalyzer<T>(comparer);
		var changes = tracker.GetChanges(oldItems, newItems);

		return changes.ToCollectionChanges();
	}

	/// <summary>
	/// Determines the set of changes between two snapshot of an <see cref="IList{T}"/>
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <param name="comparer">The set of comparer to use to track items</param>
	/// <returns>A list of changes that have to be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	public static ICollection<RichNotifyCollectionChangedEventArgs> GetChanges(
		IList oldItems,
		IList newItems,
		ItemComparer comparer)
	{
		var tracker = new CollectionAnalyzer(comparer);
		var changes = tracker.GetChanges(oldItems, newItems);

		return changes.ToCollectionChanges();
	}
}
