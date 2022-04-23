using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections.Tracking;

/// <summary>
/// Set of helpers to track changes on collections
/// </summary>
internal class CollectionAnalyzer<T> : CollectionAnalyzer
{
	public static CollectionAnalyzer<T> Default { get; } = new(); // TODO uno: use default and key equlity

	/// <param name="itemComparer">
	/// Comparer used to detect multiple versions of the **same entity (T)**, or null to use default.
	/// <remarks>Usually this should only compare the ID of the entities in order to properly track the changes made on an entity.</remarks>
	/// <remarks>For better performance, prefer provide null instead of <see cref="EqualityComparer{T}.Default"/>.</remarks>
	/// </param>
	/// <param name="itemVersionComparer">
	/// Comparer used to detect multiple instance of the **same version** of the **same entity (T)**, or null to rely only on the <paramref name="itemComparer"/> (not recommanded).
	/// <remarks>
	/// This comparer will determine if two instances of the same entity (which was considered as equals by the <paramref name="itemComparer"/>),
	/// are effectively equals or not (i.e. same version or not).
	/// <br />
	/// * If **Equals**: it's 2 **instances** of the **same version** of the **same entity** (all properties are equals), so we don't have to raise a <see cref="NotifyCollectionChangedAction.Replace"/>.<br />
	/// * If **NOT Equals**: it's 2 **distinct versions** of the **same entity** (not all properties are equals) and we have to raise a 'Replace' to re-evaluate those properties.
	/// </remarks>
	/// </param>
	public CollectionAnalyzer(
		IEqualityComparer<T>? itemComparer = null,
		IEqualityComparer<T>? itemVersionComparer = null)
		: base(
			(o, s, i) => ((IList<T>)s).IndexOf((T)o, i, itemComparer),
			IndexOfInList(itemComparer),
			itemVersionComparer == null ? null : (VersionEqual)((l, r) => itemVersionComparer.Equals((T)l, (T)r)))
	{
	}

	private static IndexOf<IList> IndexOfInList(IEqualityComparer<T>? itemComparer)
	{
		if (itemComparer is null)
		{
			return (o, s, i) => s.IndexOf(o, i, null);
		}
		else
		{
			var untyped = itemComparer.ToEqualityComparer();
			return (o, s, i) => s.IndexOf(o, i, untyped);
		}
	}

	/// <summary>
	/// Determines the set of changes between two snapshot of an <see cref="IList{T}"/>
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <param name="visitor">A visitor that can be used to track changes while detecting them.</param>
	/// <returns>A list of changes that have to be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	public CollectionUpdater GetUpdater(IList<T> oldItems, IList<T> newItems, ICollectionUpdaterVisitor visitor)
		=> base.GetUpdater((IList)oldItems, (IList)newItems, visitor);

	/// <summary>
	/// Determines the set of changes between two snapshot of an <see cref="IList{T}"/>
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <returns>A list of changes that have to be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	public CollectionChangeSet GetChanges(IList<T> oldItems, IList<T> newItems)
		=> base.GetChanges((IList)oldItems, (IList)newItems);

	internal CollectionChangeSet GetChanges(IImmutableList<T> oldItems, IImmutableList<T> newItems)
		=> base.GetChanges((IList)oldItems, (IList)newItems);

	internal CollectionChangeSet GetReset(IImmutableList<T> oldItems, IImmutableList<T> newItems)
		=> base.GetReset((IList)oldItems, (IList)newItems);
}
