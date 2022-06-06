using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using Uno.Extensions.Reactive.Collections;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections.Tracking;

/// <summary>
/// Set of helpers to track changes on collections
/// </summary>
internal class CollectionAnalyzer<T> : CollectionAnalyzer
{
	public static CollectionAnalyzer<T> Default { get; } = new(default);

	/// <summary>
	/// Creates a new instance using the given set of comparer.
	/// </summary>
	/// <param name="comparer">The set of comparer to use to track items.</param>
	public CollectionAnalyzer(ItemComparer<T> comparer)
		: base(
			list => list.GetIndexOf(comparer.Entity),
			comparer.Version is null ? null : (VersionEqual)((l, r) => comparer.Version.Equals((T)l, (T)r)))
	{
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
		=> base.GetResetChange((IList)oldItems, (IList)newItems);
}
