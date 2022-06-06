using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Uno.Extensions.Reactive.Collections;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections.Tracking;

/// <summary>
/// Set of helpers to track changes on collections
/// </summary>
internal partial class CollectionAnalyzer
{
	internal delegate int IndexOf(object item, int startIndex, int count);
	internal delegate int IndexOf<in TCollection>(object item, TCollection snapshot, int startIndex);
	internal delegate bool VersionEqual(object oldItem, object newItem);

	private readonly Func<IList, IndexOf> _indexOfFactory;
	private readonly VersionEqual? _versionEqual;

	/// <summary>
	/// Creates a new instance using the given set of comparer.
	/// </summary>
	/// <param name="comparer">The set of comparer to use to track items.</param>
	public CollectionAnalyzer(ItemComparer comparer)
		: this(
			list => list.GetIndexOf(comparer.Entity),
			comparer.Version == null ? null : (VersionEqual)(comparer.Version.Equals))
	{
	}

	protected CollectionAnalyzer(
		Func<IList, IndexOf> indexOfFactory,
		VersionEqual? versionEqual)
	{
		_indexOfFactory = indexOfFactory;
		_versionEqual = versionEqual;
	}


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
		=> new(GetChangesCore(oldItems, newItems));

	/// <summary>
	/// Determines the set of effective changes produced by a <see cref="NotifyCollectionChangedEventArgs"/>.
	/// </summary>
	/// <param name="arg">The event arg to adjust</param>
	/// <returns>A list of changes that have to be applied to move properly apply the provided event arg.</returns>
	public CollectionChangeSet GetChanges(RichNotifyCollectionChangedEventArgs arg)
		=> new(GetChangesCore(arg));

	/// <summary>
	/// Creates a set of changes that contains only a reset event.
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <param name="visitor">A visitor that can be used to track changes while detecting them.</param>
	/// <returns>A list of changes containing only a 'Reset' event that can be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	public CollectionUpdater GetResetUpdater(IList? oldItems, IList newItems, ICollectionUpdaterVisitor visitor)
		=> GetUpdater(RichNotifyCollectionChangedEventArgs.Reset(oldItems, newItems), visitor);

	/// <summary>
	/// Determines the set of changes between two snapshot of an <see cref="IList{T}"/>
	/// </summary>
	/// <param name="oldItems">The source snapshot</param>
	/// <param name="newItems">The target snapshot</param>
	/// <param name="visitor">A visitor that can be used to track changes while detecting them.</param>
	/// <returns>A list of changes that have to be applied to move a collection from <paramref name="oldItems"/> to <paramref name="newItems"/>.</returns>
	public CollectionUpdater GetUpdater(IList oldItems, IList newItems, ICollectionUpdaterVisitor visitor)
		=> CreateUpdaterCore(oldItems, newItems, visitor);

	/// <summary>
	/// Determines the set of effective changes produced by a <see cref="NotifyCollectionChangedEventArgs"/>.
	/// </summary>
	/// <param name="arg">The event arg to adjust</param>
	/// <param name="visitor">A visitor that can be used to track changes while detecting them.</param>
	/// <returns>A list of changes that have to be applied to move properly apply the provided event arg.</returns>
	public CollectionUpdater GetUpdater(RichNotifyCollectionChangedEventArgs arg, ICollectionUpdaterVisitor visitor)
	{
		var changesHead = GetChangesCore(arg);
		var updaterHead = changesHead?.ToUpdater(visitor);

		return updaterHead is null
			? CollectionUpdater.Empty
			: new(updaterHead);
	}

	private CollectionUpdater CreateUpdaterCore<TSnapshot>(
		TSnapshot oldItems,
		TSnapshot newItems,
		ICollectionUpdaterVisitor visitor,
		int eventArgsOffset = 0)
		where TSnapshot : IList
	{
		var changesHead = GetChangesCore(oldItems, newItems, eventArgsOffset);
		var updaterHead = changesHead?.ToUpdater(visitor);

		return updaterHead is null
			? CollectionUpdater.Empty
			: new(updaterHead);
	}

	private Change? GetChangesCore(RichNotifyCollectionChangedEventArgs arg)
	{
		switch (arg.Action)
		{
			case NotifyCollectionChangedAction.Add:
			case NotifyCollectionChangedAction.Remove:
			case NotifyCollectionChangedAction.Move:
			case NotifyCollectionChangedAction.Reset:
				return new _Event(arg);

			case NotifyCollectionChangedAction.Replace:
				return GetChangesCore(arg.OldItems, arg.NewItems, arg.OldStartingIndex);

			default:
				throw new ArgumentOutOfRangeException(nameof(arg), arg.Action, $"Action '{arg.Action}' not supported.");
		}
	}

	private Change? GetChangesCore(
		IList oldItems,
		IList newItems,
		int eventArgsOffset = 0)
		=> GetChangesCore((oldItems, _indexOfFactory(oldItems)), (newItems, _indexOfFactory(newItems)), _versionEqual, eventArgsOffset);

	private static Change? GetChangesCore(
		(IList items, IndexOf indexOf) old,
		(IList items, IndexOf indexOf) @new,
		VersionEqual? itemVersionComparer,
		int eventArgsOffset = 0)
	{
		/*
		* OLD: the source collection we are going to update to the NEW
		* NEW: the collection that we want to go to
		* RESULT (a.k.a. virtual old) : This makes sense only while synchronizing, it's represents the OLD with the changes already applied. 
		*								 At the end it's expected to be sequence equals to the NEW.
		* 
		* We detect changes to update from OLD to NEW. We could have choose the other way, but as the consumer (i.e. the view) knows the OLD, it's a bit more logical.
		* 
		* Note: comparer may be null for IObservableCollectionSnapshot.IndexOf(). 
		*		 It's more performant to forward the 'null' instead of defaulting to EqualityComparer.Default
		* 
		*/

		int added = 0, moved = 0, removed = 0;
		var buffer = new ChangesBuffer(old.items.Count, @new.items.Count, eventArgsOffset);

		var oldEnumerator = new SourceEnumerator(old.items, old.indexOf);
		while (oldEnumerator.MoveNext())
		{
			var oldIndex = oldEnumerator.CurrentIndex;
			var oldItem = oldEnumerator.Current!;
			var resultIndex = oldIndex - removed + added + moved - oldEnumerator.Ignored; // The current index in the (virtual) result collection (i.e. oldIndex ignoring the removed/added items)
			var newIndex = @new.indexOf(oldItem, resultIndex, @new.items.Count - resultIndex);

			if (newIndex < 0)
			{
				// Item is no more present in the new collection : Remove it

				buffer.Remove(oldItem, resultIndex);
				removed++;

				continue;
			}

			if (newIndex < resultIndex)
			{
				throw new InvalidOperationException("The index return by the IndexOf is invalid");
			}

			// First raise replace if the instance/version of the item changed
			UpdateInstanceFromOld(oldItem, oldIndex, newIndex);

			if (newIndex > resultIndex)
			{
				// Item is AFTER the expected index in old, this means that some items was inserted / moved before

				var movedBeforeItemLocal = 0;
				for (var missingItemNewIndex = resultIndex; missingItemNewIndex < newIndex; missingItemNewIndex++)
				{
					var missingItem = @new.items[missingItemNewIndex]; // The item that is missing in the old collection
					var (missingItemOldIndex, missingItemOldIndexOffset) = oldEnumerator.NextIndexOf(missingItem);
					if (missingItemOldIndex >= 0)
					{
						// The missing item was already present in the old snapshot. We only have to move it.

						// The 'fromOffset' counts only the number of items that have been move from 'after' the item to 'before' it.
						// We include this backward moves as they are already offsetting the virtual old index, but we do not include
						// any other moves (so we are not using 'moved') as they haven't any impact on the virtual old index.
						var from = missingItemOldIndex - removed + added;
						var fromOffset = missingItemOldIndexOffset;

						// As the item will be handle here (moved), ignore the index to ensure that we won't try to move it again later
						oldEnumerator.Ignore(missingItemOldIndex);

						// First update the instance if needed, then move it to it new position
						UpdateInstanceFromNew(missingItem, missingItemOldIndex);
						buffer.Move(missingItem, from: from, fromOffset: fromOffset, to: missingItemNewIndex, max: newIndex);
						moved++;
						movedBeforeItemLocal++;
					}
					else
					{
						// The missing item is a new item, we have to add it.

						buffer.Add(missingItem, at: missingItemNewIndex, max: newIndex);
						added++;
					}
				}
			}

			Debug.Assert(newIndex == oldIndex - removed + added + moved - oldEnumerator.Ignored);
		}

		Debug.Assert(moved - oldEnumerator.Ignored == 0);

		// Finally add items that remains at the end of the newItems (i.e. was missing in the previous)
		var resultItemsCount = old.items.Count - removed + added;
		var toAddCount = @new.items.Count - resultItemsCount;
		if (toAddCount > 0)
		{
			var add = new _Add(at: resultItemsCount, indexOffset: eventArgsOffset, capacity: toAddCount);
			for (var i = 0; i < toAddCount; i++)
			{
				var item = @new.items[i + resultItemsCount];
				add.Append(item);
			}

			buffer.Append(add);
		}

		return buffer.GetChanges();


		/*
		*	About equality checks and instance update:
		*
		*	Usually the 'itemVersionComparer' checks for full Equality, while the 'itemComparer' only checks for the KeyEquality.
		*	The idea is to be able to properly track multiple versions of the same item (for instance if a property of the item changed).
		*
		*	Here, the "item tracking" was already done, and we are only validating 2 versions of the same item
		*  (we already determined that they have the same key using the 'itemComparer').
		*
		*	If the 'itemVersionComparer' is 'null' (or if the value is a boxed value type) we assume that there is no
		*  notion of version of an item and we rely only on the `itemComparer` to check equality.
		*  Note: in this case we don't raise any 'Replace'.
		*
		*	If the 'itemVersionComparer' returns 'true' (or if it's 'null') that means that they are not only KeyEquals but also Equals.
		*	So as we are usually working with immutable objects, we can ignore that a new instance is available and
		*  we don't raise any event ('changesBuffer.Update'). Note: We still have to notify the visitor!
		*
		*  If the 'itemVersionComparer' returns 'false' that means items are only key equals, but not same version.
		*  So we have to raise a 'Replace' event.
		*/

		void UpdateInstanceFromOld(object oldItem, int oldIndex, int newIndex)
		{
			if (itemVersionComparer is null || oldItem is ValueType)
			{
				buffer.Update(oldItem, oldItem, oldIndex);

				return;
			}

			var newItem = @new.items[newIndex];
			if (itemVersionComparer(oldItem, newItem))
			{
				buffer.Update(oldItem, newItem, oldIndex);
			}
			else
			{
				buffer.Replace(oldItem, newItem, oldIndex);
			}
		}

		void UpdateInstanceFromNew(object newItem, int oldIndex)
		{
			if (itemVersionComparer is null || newItem is ValueType)
			{
				buffer.Update(newItem, newItem, oldIndex);

				return;
			}

			var oldItem = old.items[oldIndex];
			if (itemVersionComparer(oldItem, newItem))
			{
				buffer.Update(oldItem, newItem, oldIndex);
			}
			else
			{
				buffer.Replace(oldItem, newItem, oldIndex);
			}
		}
	}
}
