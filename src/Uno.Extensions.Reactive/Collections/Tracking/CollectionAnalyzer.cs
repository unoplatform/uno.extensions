using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Uno.Extensions.Reactive.Collections;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections.Tracking;

/// <summary>
/// Set of helpers to track changes on collections
/// </summary>
internal partial class CollectionAnalyzer
{
	private readonly IEqualityComparer? _entityComparer;
	private readonly ComparerRef<object?>? _versionComparer;

	/// <summary>
	/// Creates a new instance using the given set of comparer.
	/// </summary>
	/// <param name="comparer">The set of comparer to use to track items.</param>
	public CollectionAnalyzer(ItemComparer comparer)
	{
		_entityComparer = comparer.Entity;
		_versionComparer = GetRef(comparer.Version);
	}

	private ListRef<object?> GetRef(IList list)
		=> new(list, list.Count, i => list[i], list.GetIndexOf(_entityComparer));

	/// <summary>
	/// Creates a set of changes that contains only a reset event.
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <returns>A list of changes containing only a 'Reset' event that can be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	public CollectionChangeSet GetResetChange(IList? oldItems, IList newItems)
		=> GetChanges(RichNotifyCollectionChangedEventArgs.Reset(oldItems, newItems));

	/// <summary>
	/// Determines the set of changes between two snapshot of an <see cref="IList{T}"/>
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <returns>A list of changes that have to be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	public CollectionChangeSet GetChanges(IList oldItems, IList newItems)
		=> new CollectionChangeSet<object?>(GetChangesCore(GetRef(oldItems), GetRef(newItems), _versionComparer));

	/// <summary>
	/// Determines the set of effective changes produced by a <see cref="NotifyCollectionChangedEventArgs"/>.
	/// </summary>
	/// <param name="arg">The event arg to adjust</param>
	/// <returns>A list of changes that have to be applied to move properly apply the provided event arg.</returns>
	public CollectionChangeSet GetChanges(RichNotifyCollectionChangedEventArgs arg)
		=> new CollectionChangeSet<object?>(GetChangesCore(arg, GetRef, _versionComparer));

	/// <summary>
	/// Creates a set of changes that contains only a reset event.
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <param name="visitor">A visitor that can be used to track changes while detecting them.</param>
	/// <returns>A list of changes containing only a 'Reset' event that can be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	// DEPRECATED: We need to complete extraction of the CollectionUpdater concept
	public CollectionUpdater GetResetUpdater(IList? oldItems, IList newItems, ICollectionUpdaterVisitor visitor)
		=> GetUpdater(RichNotifyCollectionChangedEventArgs.Reset(oldItems, newItems), visitor);

	/// <summary>
	/// Determines the set of changes between two snapshot of an <see cref="IList{T}"/>
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <param name="visitor">A visitor that can be used to track changes while detecting them.</param>
	/// <returns>A list of changes that have to be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	// DEPRECATED: We need to complete extraction of the CollectionUpdater concept
	public CollectionUpdater GetUpdater(IList oldItems, IList newItems, ICollectionUpdaterVisitor visitor)
		=> ToUpdater(GetChangesCore(GetRef(oldItems), GetRef(newItems), _versionComparer), visitor);

	/// <summary>
	/// Determines the set of effective changes produced by a <see cref="NotifyCollectionChangedEventArgs"/>.
	/// </summary>
	/// <param name="arg">The event arg to adjust</param>
	/// <param name="visitor">A visitor that can be used to track changes while detecting them.</param>
	/// <returns>A list of changes that have to be applied to move properly apply the provided event arg.</returns>
	// DEPRECATED: We need to complete extraction of the CollectionUpdater concept
	public CollectionUpdater GetUpdater(RichNotifyCollectionChangedEventArgs arg, ICollectionUpdaterVisitor visitor)
		=> ToUpdater(GetChangesCore(arg, GetRef, _versionComparer), visitor);
}
