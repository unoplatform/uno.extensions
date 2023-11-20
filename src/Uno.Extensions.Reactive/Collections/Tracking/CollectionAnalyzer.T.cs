using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Collections;
using Uno.Extensions.Reactive.Utils;
using static Uno.Extensions.Collections.Tracking.CollectionAnalyzer;

namespace Uno.Extensions.Collections.Tracking;

/// <summary>
/// Set of helpers to track changes on collections
/// </summary>
internal class CollectionAnalyzer<T>
{
	private readonly IEqualityComparer<T>? _comparer;
	private readonly ComparerRef<T>? _versionComparer;

	/// <summary>
	/// Creates a new instance using the given set of comparer.
	/// </summary>
	/// <param name="comparer">The set of comparer to use to track items.</param>
	public CollectionAnalyzer(ItemComparer<T> comparer)
	{
		_comparer = comparer.Entity;
		_versionComparer = CollectionAnalyzer.GetRef(comparer.Version);
	}

	private ListRef<T> GetRef(IList list)
		=> new(list, list.Count, i => (T)list[i]!, list.GetIndexOf(_comparer));

	private ListRef<T> GetRef(IList<T> list)
		=> new(list, list.Count, i => list[i], list.GetIndexOf(_comparer));

	private ListRef<T> GetRef(IImmutableList<T> list)
		=> new(list, list.Count, i => list[i], list.GetIndexOf(_comparer));

	/// <summary>
	/// Creates a set of changes that only contains a reset event.
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <returns>A list of changes only containing a 'Reset' event that can be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	internal CollectionChangeSet<T> GetResetChange(IList<T> oldItems, IList<T> newItems)
		=> new(GetChangesCore(RichNotifyCollectionChangedEventArgs.Reset(oldItems, newItems), GetRef, _versionComparer));

	/// <summary>
	/// Creates a set of changes that contains only a reset event.
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <returns>A list of changes only containing a 'Reset' event that can be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	internal CollectionChangeSet<T> GetResetChange(IImmutableList<T> oldItems, IImmutableList<T> newItems)
		=> new(GetChangesCore(RichNotifyCollectionChangedEventArgs.Reset(oldItems, newItems), GetRef, _versionComparer));

	/// <summary>
	/// Determines the set of changes between two snapshot of an <see cref="IList{T}"/>
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <returns>A list of changes that have to be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	public CollectionChangeSet<T> GetChanges(IList<T> oldItems, IList<T> newItems)
		=> new(GetChangesCore(GetRef(oldItems), GetRef(newItems), _versionComparer));

	/// <summary>
	/// Determines the set of changes between two snapshot of an <see cref="IImmutableList{T}"/>
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <returns>A list of changes that have to be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	internal CollectionChangeSet<T> GetChanges(IImmutableList<T> oldItems, IImmutableList<T> newItems)
		=> new(GetChangesCore(GetRef(oldItems), GetRef(newItems), _versionComparer));

	/// <summary>
	/// Determines the set of effective changes produced by a <see cref="NotifyCollectionChangedEventArgs"/>.
	/// </summary>
	/// <param name="arg">The event arg to adjust</param>
	/// <returns>A list of changes that have to be applied to move properly apply the provided event arg.</returns>
	public CollectionChangeSet<T> GetChanges(RichNotifyCollectionChangedEventArgs arg)
		=> new(GetChangesCore(arg, GetRef, _versionComparer));

	/// <summary>
	/// Determines the set of changes between two snapshot of an <see cref="IList{T}"/>
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <param name="visitor">A visitor that can be used to track changes while detecting them.</param>
	/// <returns>A list of changes that have to be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	// DEPRECATED: We need to complete extraction of the CollectionUpdater concept
	public CollectionUpdater GetUpdater(IList<T> oldItems, IList<T> newItems, ICollectionUpdaterVisitor visitor)
		=> ToUpdater(GetChangesCore(GetRef(oldItems), GetRef(newItems), _versionComparer), visitor);

	// DEPRECATED: We need to complete extraction of the CollectionUpdater concept
	internal CollectionUpdater GetUpdater(IImmutableList<T> oldItems, IImmutableList<T> newItems, ICollectionUpdaterVisitor visitor)
		=> ToUpdater(GetChangesCore(GetRef(oldItems), GetRef(newItems), _versionComparer), visitor);
}
