using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Uno.Extensions.Collections.Tracking;

internal partial class CollectionAnalyzer
{
	internal static ComparerRef<object?>? GetRef(IEqualityComparer? comparer)
		=> comparer is null ? null : comparer.Equals;

	internal static ComparerRef<T>? GetRef<T>(IEqualityComparer<T>? comparer)
		=> comparer is null ? null : comparer.Equals;

	// DEPRECATED: We need to complete extraction of the CollectionUpdater concept
	internal static CollectionUpdater ToUpdater<T>(Change<T>? changed, ICollectionUpdaterVisitor visitor)
	{
		var updaterHead = changed?.ToUpdater(visitor);

		return updaterHead is null
			? CollectionUpdater.Empty
			: new(updaterHead);
	}

	internal static Change<T>? GetChangesCore<T>(
		RichNotifyCollectionChangedEventArgs arg,
		Func<IList, ListRef<T>> getRef,
		ComparerRef<T>? versionComparer)
	{
		switch (arg.Action)
		{
			case NotifyCollectionChangedAction.Add:
			case NotifyCollectionChangedAction.Remove:
			case NotifyCollectionChangedAction.Move:
			case NotifyCollectionChangedAction.Reset:
				return new _Event<T>(arg);

			// For the replace, we are digging in the collection to determine if we could simplify the change considering the given comparers
			case NotifyCollectionChangedAction.Replace:
				return GetChangesCore(getRef(arg.OldItems!), getRef(arg.NewItems!), versionComparer, arg.OldStartingIndex);

			default:
				throw new ArgumentOutOfRangeException(nameof(arg), arg.Action, $"Action '{arg.Action}' not supported.");
		}
	}

	internal static Change<T>? GetChangesCore<T>(
		ListRef<T> oldItems,
		ListRef<T> newItems,
		ComparerRef<T>? versionComparer,
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
		var buffer = new ChangesBuffer<T>(oldItems.Count, newItems.Count, eventArgsOffset);

		var oldEnumerator = new SourceEnumerator<T>(oldItems);
		while (oldEnumerator.MoveNext())
		{
			var oldIndex = oldEnumerator.CurrentIndex;
			var oldItem = oldEnumerator.Current;
			var resultIndex = oldIndex - removed + added + moved - oldEnumerator.Ignored; // The current index in the (virtual) result collection (i.e. oldIndex ignoring the removed/added items)
			var newIndex = newItems.IndexOf(oldItem, resultIndex, newItems.Count - resultIndex);

			if (newIndex < 0)
			{
				// Item is no more present in the new collection : Remove it

				buffer.Remove(oldItem, resultIndex);
				removed++;

				continue;
			}

			if (newIndex < resultIndex)
			{
				throw new InvalidOperationException("The index returned by the IndexOf is invalid");
			}

			// First raise replace if the instance/version of the item changed
			UpdateInstanceFromOld(oldItem, oldIndex, newIndex);

			if (newIndex > resultIndex)
			{
				// Item is AFTER the expected index in old, this means that some items was inserted / moved before

				for (var missingItemNewIndex = resultIndex; missingItemNewIndex < newIndex; missingItemNewIndex++)
				{
					var missingItem = newItems.ElementAt(missingItemNewIndex); // The item that is missing in the old collection
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
		var resultItemsCount = oldItems.Count - removed + added;
		var toAddCount = newItems.Count - resultItemsCount;
		if (toAddCount > 0)
		{
			var add = new _Add<T>(at: resultItemsCount, indexOffset: eventArgsOffset, capacity: toAddCount);
			for (var i = 0; i < toAddCount; i++)
			{
				var item = newItems.ElementAt(i + resultItemsCount);
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
		*	If the 'itemVersionComparer' is 'null' we assume that there is no
		*	notion of version of an item and we rely only on the `itemComparer` to check (true/full) equality.
		*	Note: in this case we don't raise any 'Replace'.
		*
		*	If the 'itemVersionComparer' returns 'true' that means that they are not only KeyEquals but also Equals.
		*	So as we are usually working with immutable objects, we can ignore that a new instance is available and
		*	we don't raise any event ('changesBuffer.Update'). Note: We still have to notify the visitor!
		*
		*	If the 'itemVersionComparer' returns 'false' that means items are only key equals, but not same version.
		*	So we have to raise a 'Replace' event.
		*	Note: The replace event is flagged as "SameEntity" so the view can raise some "INPC.Item[]" instead of real "INCC.Replace"
		*/

		void UpdateInstanceFromOld(T oldItem, int oldIndex, int newIndex)
		{
			if (versionComparer is null)
			{
				buffer.Keep(oldItem, oldItem, oldIndex);

				return;
			}

			var newItem = newItems.ElementAt(newIndex);
			if (versionComparer(oldItem, newItem))
			{
				buffer.Keep(oldItem, newItem, oldIndex);
			}
			else
			{
				buffer.Replace(oldItem, newItem, oldIndex);
			}
		}

		void UpdateInstanceFromNew(T newItem, int oldIndex)
		{
			if (versionComparer is null)
			{
				buffer.Keep(newItem, newItem, oldIndex);

				return;
			}

			var oldItem = oldItems.ElementAt(oldIndex);
			if (versionComparer(oldItem, newItem))
			{
				buffer.Keep(oldItem, newItem, oldIndex);
			}
			else
			{
				buffer.Replace(oldItem, newItem, oldIndex);
			}
		}
	}
}
