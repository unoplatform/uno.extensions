using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

public interface IChangeSet : IEnumerable<IChange>
{
}

public interface IChange
{
}

public interface ICollectionChange : IChange
{
	RichNotifyCollectionChangedEventArgs? ToEventArgs();
}

public interface IEntityChange : IChange
{
	PropertyChangedEventArgs? ToEventArgs();
}

//public record CollectionChangeSet(params CollectionChange[] changes) : IEnumerable<CollectionChange>, IChangeSet
//{
//	private readonly CollectionChange[] changes = changes;

//	/// <inheritdoc />
//	public IEnumerator<CollectionChange> GetEnumerator()
//		=> ((IEnumerable<CollectionChange>)changes).GetEnumerator();

//	/// <inheritdoc />
//	IEnumerator IEnumerable.GetEnumerator()
//		=> changes.GetEnumerator();
//}

//public record CollectionUpdateCollection : IList, IEnumerable<(object? entity, IChangeSet changeset)>
//{

//}


///// <summary>
///// Describes a change on a collection.
///// </summary>
///// <param name="type">The type of the change.</param>
///// <param name="OldItems">The items that have been removed, replaced, updated, moved, rested.</param>
///// <param name="OldIndex">The index from where the <see cref="OldItems"/> has been removed, replaced, updated, moved, rested.</param>
///// <param name="NewItems">The items that have been added, replaced, updated, moved (same as OldItems), rested.</param>
///// <param name="NewIndex">The index from where the <see cref="NewItems"/> has been added, replaced, updated, moved, rested.</param>
//public record CollectionChange(CollectionChangeType type, IList OldItems, int OldIndex, IList NewItems, int NewIndex)
//{
//	/// <summary>
//	/// Creates a <see cref="CollectionChangeType.Add"/> collection change.
//	/// </summary>
//	public static CollectionChange Add(object? item, int index)
//		=> new(CollectionChangeType.Add, Array.Empty<object?>(), -1, new[] { item }, index);

//	/// <summary>
//	/// Creates a <see cref="CollectionChangeType.Add"/> collection change.
//	/// </summary>
//	public static CollectionChange AddSome(IList items, int index)
//		=> new(CollectionChangeType.Add, Array.Empty<object?>(), -1, items, index);

//	/// <summary>
//	/// Creates a <see cref="CollectionChangeType.Remove"/> collection change.
//	/// </summary>
//	public static CollectionChange Remove(object? item, int index)
//		=> new(CollectionChangeType.Remove, new[] { item }, index, Array.Empty<object?>(), -1);

//	/// <summary>
//	/// Creates a <see cref="CollectionChangeType.Remove"/> collection change.
//	/// </summary>
//	public static CollectionChange RemoveSome(IList items, int index)
//		=> new(CollectionChangeType.Remove, items, index, Array.Empty<object?>(), -1);

//	/// <summary>
//	/// Creates a <see cref="CollectionChangeType.Replace"/> collection change.
//	/// </summary>
//	public static CollectionChange Replace(object? oldItem, object? newItem, int index)
//		=> new(CollectionChangeType.Replace, new[] { newItem }, index, new[] { oldItem }, index);

//	/// <summary>
//	/// Creates a <see cref="CollectionChangeType.Replace"/> collection change.
//	/// </summary>
//	public static CollectionChange ReplaceSome(IList oldItems, IList newItems, int index)
//		=> new(CollectionChangeType.Replace, newItems, index, oldItems, index);

//	/// <summary>
//	/// Creates a <see cref="CollectionChangeType.Replace"/> collection change.
//	/// </summary>
//	public static CollectionChange Update(object? oldItem, object? newItem, int index)
//		=> new(CollectionChangeType.Replace, new[] { newItem }, index, new[] { oldItem }, index);

//	/// <summary>
//	/// Creates a <see cref="CollectionChangeType.Replace"/> collection change.
//	/// </summary>
//	public static CollectionChange UpdateSome(IList oldItems, IList newItems, int index)
//		=> new(CollectionChangeType.Replace, newItems, index, oldItems, index);

//	/// <summary>
//	/// Creates a <see cref="CollectionChangeType.Replace"/> collection change.
//	/// </summary>
//	public static CollectionChange Update(object? oldItem, object? newItem, IChangeSet changes, int index)
//		=> new(CollectionChangeType.Replace, new[] { newItem }, index, new[] { oldItem }, index);

//	/// <summary>
//	/// Creates a <see cref="CollectionChangeType.Replace"/> collection change.
//	/// </summary>
//	public static CollectionChange UpdateSome(IList oldItems, IList<(object? entity, IChangeSet changes)> newItems, int index)
//		=> new(CollectionChangeType.Replace, newItems, index, oldItems, index);


//	/// <summary>
//	/// Creates a <see cref="CollectionChangeType.Move"/> collection change.
//	/// </summary>
//	public static CollectionChange Move(object? item, int oldIndex, int newIndex)
//		=> Move(new[] { item }, oldIndex, newIndex);

//	/// <summary>
//	/// Creates a <see cref="CollectionChangeType.Move"/> collection change.
//	/// </summary>
//	public static CollectionChange MoveSome(IList items, int oldIndex, int newIndex)
//		=> new(CollectionChangeType.Move, items, newIndex, items, oldIndex);

//	/// <summary>
//	/// Creates a <see cref="CollectionChangeType.Reset"/> collection change.
//	/// </summary>
//	public static CollectionChange Reset(IList oldItems, IList newItems)
//		=> new(CollectionChangeType.Reset, oldItems, 0, newItems, 0);
//}

//public enum CollectionChangeType
//{
//	/// <summary>
//	/// One or more items has been inserted in the collection at the given newIndex.
//	/// </summary>
//	Add,

//	/// <summary>
//	/// One or more items has been removed from the collection at the given oldIndex.
//	/// </summary>
//	Remove,

//	/// <summary>
//	/// One or more items has been replaced in the collection at the given index.
//	/// This indicates that some items has been removed and some others has been added as replacement.
//	/// </summary>
//	Replace,

//	/// <summary>
//	/// One or more items has been updated at the given oldIndex.
//	/// Unlike the Replace, this means that the items logically the same (same ID), but some properties has been updated.
//	/// From a UI perspective, it means that the container does not have to been recycled, we only have to re-evalute bindings.
//	/// </summary>
//	Update,

//	/// <summary>
//	/// Some items has been move within the collection from the oldIndex to the newIndex.
//	/// </summary>
//	Move,

//	/// <summary>
//	/// The collection has changed significantly from the oldItems to the newItems.
//	/// Some items may remain in the collection, but too much changes occurred to get a valid diff.
//	/// </summary>
//	Reset
//}
