using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections;

internal class RichNotifyCollectionChangedEventArgs : NotifyCollectionChangedEventArgs
{
	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Add"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs Add(object? item, int index)
		=> new(NotifyCollectionChangedAction.Add, new[] { item }, index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Add"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs Add<T>(T item, int index)
		=> new(NotifyCollectionChangedAction.Add, new[] { item }, index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Add"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs AddSome(IList items, int index)
		=> new(NotifyCollectionChangedAction.Add, items, index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Add"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs AddSome<T>(IList<T> items, int index)
		=> new(NotifyCollectionChangedAction.Add, items.AsUntypedList(), index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Remove"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs Remove(object? item, int index)
		=> new(NotifyCollectionChangedAction.Remove, new[] { item }, index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Remove"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs Remove<T>(T item, int index)
		=> new(NotifyCollectionChangedAction.Remove, new [] { item }, index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Remove"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs RemoveSome(IList items, int index)
		=> new(NotifyCollectionChangedAction.Remove, items, index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Remove"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs RemoveSome<T>(IList<T> items, int index)
		=> new(NotifyCollectionChangedAction.Remove, items.AsUntypedList(), index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Replace"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs Replace(object? oldItem, object? newItem, int index, bool isReplaceOfSameEntity = false)
		=> new(NotifyCollectionChangedAction.Replace, new[] { newItem }, new[] { oldItem }, index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Replace"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs Replace<T>(T oldItem, T newItem, int index, bool isReplaceOfSameEntity = false)
		=> new(NotifyCollectionChangedAction.Replace, new[] { newItem }, new[] { oldItem }, index) { IsReplaceOfSameEntities = isReplaceOfSameEntity};

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Replace"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs ReplaceSome(IList oldItems, IList newItems, int index, bool isReplaceOfSameEntities = false)
	{
		if (isReplaceOfSameEntities && oldItems.Count != newItems.Count)
		{
			throw new InvalidOperationException("For a replace flagged with isReplaceOfSameEntities (a.k.a. update), the number of oldItems must be the same as newItems.");
		}

		return new(NotifyCollectionChangedAction.Replace, newItems, oldItems, index) { IsReplaceOfSameEntities = isReplaceOfSameEntities };
	}

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Replace"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs ReplaceSome<T>(IList<T> oldItems, IList<T> newItems, int index, bool isReplaceOfSameEntities = false)
	{
		if (isReplaceOfSameEntities && oldItems.Count != newItems.Count)
		{
			throw new InvalidOperationException("For a replace flagged with isReplaceOfSameEntities (a.k.a. update), the number of oldItems must be the same as newItems.");
		}

		return new(NotifyCollectionChangedAction.Replace, newItems.AsUntypedList(), oldItems.AsUntypedList(), index) { IsReplaceOfSameEntities = isReplaceOfSameEntities };
	}

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Move"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs Move(object? item, int oldIndex, int newIndex)
		=> new(NotifyCollectionChangedAction.Move, new[] { item }, newIndex, oldIndex);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Move"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs Move<T>(T item, int oldIndex, int newIndex)
		=> new(NotifyCollectionChangedAction.Move, new [] { item }, newIndex, oldIndex);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Move"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs MoveSome(IList items, int oldIndex, int newIndex)
		=> new(NotifyCollectionChangedAction.Move, items, newIndex, oldIndex);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Move"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs MoveSome<T>(IList<T> items, int oldIndex, int newIndex)
		=> new(NotifyCollectionChangedAction.Move, items.AsUntypedList(), newIndex, oldIndex);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Reset"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs Reset(IList? oldItems, IList? newItems)
		=> new(NotifyCollectionChangedAction.Reset)
		{
			ResetOldItems = oldItems ?? Array.Empty<object>(),
			ResetNewItems = newItems ?? Array.Empty<object>()
		};

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Reset"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs Reset<T>(IList<T>? oldItems, IList<T>? newItems)
		=> new(NotifyCollectionChangedAction.Reset)
		{
			ResetOldItems = oldItems?.AsUntypedList() ?? Array.Empty<T>(),
			ResetNewItems = newItems?.AsUntypedList() ?? Array.Empty<T>()
		};

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Reset"/> collection changed event args
	/// </summary>
	public static RichNotifyCollectionChangedEventArgs Reset<T>(IImmutableList<T>? oldItems, IImmutableList<T>? newItems)
		=> new(NotifyCollectionChangedAction.Reset)
		{
			ResetOldItems = oldItems?.AsUntypedList() ?? Array.Empty<T>(),
			ResetNewItems = newItems?.AsUntypedList() ?? Array.Empty<T>()
		};

	private RichNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action)
		: base(action)
	{
	}

	private RichNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? changedItem)
		: base(action, changedItem)
	{
	}

	private RichNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? changedItem, int index)
		: base(action, changedItem, index)
	{
	}

	private RichNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems)
		: base(action, changedItems)
	{
	}

	private RichNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems, int startingIndex)
		: base(action, changedItems, startingIndex)
	{
	}

	private RichNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? newItem, object? oldItem)
		: base(action, newItem, oldItem)
	{
	}

	private RichNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? newItem, object? oldItem, int index)
		: base(action, newItem, oldItem, index)
	{
	}

	private RichNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems)
		: base(action, newItems, oldItems)
	{
	}

	private RichNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems, int startingIndex)
		: base(action, newItems, oldItems, startingIndex)
	{
	}

	private RichNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? changedItem, int index, int oldIndex)
		: base(action, changedItem, index, oldIndex)
	{
	}

	private RichNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems, int index, int oldIndex)
		: base(action, changedItems, index, oldIndex)
	{
	}

	/// <summary>
	/// For a Replace change, indicates if entities are actually new versions of the same (same Id / Key, cf. KeyEquality) entities.
	/// If true, this change should be considered as an "update" instead of a real "replace".
	/// (I.e. entities have same IDs but have some fields that has been updated).
	/// </summary>
	public bool IsReplaceOfSameEntities { get; private set; }

	/// <summary>
	/// Gets the old items for a reset change
	/// </summary>
	public IList? ResetOldItems { get; private set; }

	/// <summary>
	/// Gets the new items for a reset change
	/// </summary>
	public IList? ResetNewItems { get; private set; }

	/// <summary>
	/// Apply an offset to the old and new items indexes of a <see cref="RichNotifyCollectionChangedEventArgs"/>.
	/// </summary>
	/// <exception cref="InvalidOperationException">If the <see cref="NotifyCollectionChangedEventArgs.Action"/> is <see cref="NotifyCollectionChangedAction.Reset"/>.</exception>
	/// <param name="offset">The offset to apply to the indexes</param>
	public RichNotifyCollectionChangedEventArgs OffsetIndexBy(int offset)
	{
		if (offset == 0)
		{
			return this;
		}

		switch (Action)
		{
			case NotifyCollectionChangedAction.Add:
				return new RichNotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Add,
					NewItems!,
					NewStartingIndex + offset);

			case NotifyCollectionChangedAction.Move:
				return new RichNotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Move,
					NewItems!,
					NewStartingIndex + offset,
					OldStartingIndex + offset);

			case NotifyCollectionChangedAction.Replace:
				return new RichNotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Replace,
					NewItems!,
					OldItems!,
					NewStartingIndex + offset);

			case NotifyCollectionChangedAction.Remove:
				return new RichNotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Remove,
					OldItems!,
					OldStartingIndex + offset);

			case NotifyCollectionChangedAction.Reset:
				throw new InvalidOperationException("Cannot offset a 'Reset' change.");

			default:
				throw new ArgumentOutOfRangeException("Action", $"Unknown collection type action {Action}");
		}
	}

	///// <summary>
	///// Converts the old and new items of an <see cref="NotifyCollectionChangedEventArgs"/> using a <see cref="IConverter{TFrom,TTo}"/>
	///// </summary>
	///// <typeparam name="TFrom">Type of the items in the original args</typeparam>
	///// <typeparam name="TTo">Type of the items in the result args</typeparam>
	///// <param name="args">Original args</param>
	///// <param name="converter">Converter to use to convert items</param>
	///// <returns>A <see cref="NotifyCollectionChangedEventArgs"/> of <typeparamref name="TTo"/>.</returns>
	//public RichNotifyCollectionChangedEventArgs ConvertItems<TFrom, TTo>(IConverter<TFrom, TTo> converter)
	//{
	//	switch (Action)
	//	{
	//		case NotifyCollectionChangedAction.Add:
	//			return new RichNotifyCollectionChangedEventArgs(
	//				NotifyCollectionChangedAction.Add,
	//				new MapReadOnlyList<TFrom, TTo>(converter, NewItems),
	//				NewStartingIndex);

	//		case NotifyCollectionChangedAction.Move:
	//			return new RichNotifyCollectionChangedEventArgs(
	//				NotifyCollectionChangedAction.Move,
	//				new MapReadOnlyList<TFrom, TTo>(converter, NewItems),
	//				NewStartingIndex,
	//				OldStartingIndex);

	//		case NotifyCollectionChangedAction.Replace:
	//			return new RichNotifyCollectionChangedEventArgs(
	//				NotifyCollectionChangedAction.Replace,
	//				new MapReadOnlyList<TFrom, TTo>(converter, NewItems),
	//				new MapReadOnlyList<TFrom, TTo>(converter, OldItems),
	//				NewStartingIndex);

	//		case NotifyCollectionChangedAction.Remove:
	//			return new RichNotifyCollectionChangedEventArgs(
	//				NotifyCollectionChangedAction.Remove,
	//				new MapReadOnlyList<TFrom, TTo>(converter, OldItems),
	//				OldStartingIndex);

	//		case NotifyCollectionChangedAction.Reset:
	//			return new RichNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
	//			{
	//				ResetNewItems = new MapReadOnlyList<TFrom, TTo>(converter, ResetNewItems)
	//			};

	//		default:
	//			throw new ArgumentOutOfRangeException("Action", $"Unknown collection type action {Action}");
	//	}
	//}

	/// <inheritdoc />
	public override string ToString()
	{
		switch (Action)
		{
			case NotifyCollectionChangedAction.Add:
				return $"Add {NewItems!.Count} items at {NewStartingIndex}";

			case NotifyCollectionChangedAction.Move:
				return $"Move {NewItems!.Count} items from {OldStartingIndex} to {NewStartingIndex}";

			case NotifyCollectionChangedAction.Remove:
				return $"Remove {OldItems!.Count} items at {OldStartingIndex}";

			case NotifyCollectionChangedAction.Replace:
				return $"Replace {OldItems!.Count} items at {OldStartingIndex} by {NewItems!.Count} items";

			case NotifyCollectionChangedAction.Reset:
				return $"Reset from {ResetOldItems!.Count} items to {ResetNewItems!.Count} items";

			default:
				return $"Unknown change {Action}";
		}
	}
}
