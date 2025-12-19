using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Uno.Extensions.Edition;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Operators;

internal static class ListFeedSelection<
	[DynamicallyAccessedMembers(ListFeed.TRequirements)]
	T
>
{
	public static IListState<T> Create(IListFeed<T> source, IState<IImmutableList<T>> selectionState, string logTag)
	{
		var comparer = ListFeed<T>.DefaultComparer.Entity;

		return new ListFeedSelection<T, IImmutableList<T>>(source, selectionState, InfoToItems, ItemsToInfo, logTag);

		Option<IImmutableList<T>> InfoToItems(IImmutableList<T> items, SelectionInfo info, Option<IImmutableList<T>> _)
			=> info.GetSelectedItems(items) is { Count: > 0 } selectedItems
				? Extensions.Option<IImmutableList<T>>.Some(selectedItems)
				: Extensions.Option<IImmutableList<T>>.None();

		SelectionInfo ItemsToInfo(IImmutableList<T> items, Option<IImmutableList<T>> selectedItems)
		{
			if (selectedItems.IsSome(out var other))
			{
				if (SelectionInfo.TryCreateMultiple(items, other, out var info, comparer))
				{
					return info;
				}

				selectionState.Log().Warn(
					$"In the {logTag}, some items that have been set as selected in the state are not present in the list. "
					+ "Selection is being cleared on the list state.");
			}

			return SelectionInfo.Empty;
		}
	}

	public static IListState<T> Create(IListFeed<T> source, IState<T> selectionState, string logTag)
	{
		var comparer = ListFeed<T>.DefaultComparer.Entity;

		return new ListFeedSelection<T, T>(source, selectionState, InfoToItem, ItemToInfo, logTag);

		Option<T> InfoToItem(IImmutableList<T> items, SelectionInfo selection, Option<T> _)
			=> selection.TryGetSelectedItem(items, out var item)
				? Extensions.Option.Some(item)
				: Extensions.Option.None<T>();

		SelectionInfo ItemToInfo(IImmutableList<T> items, Option<T> selectedItem)
		{
			if (selectedItem.IsSome(out var other))
			{
				if (SelectionInfo.TryCreateSingle(items, other, out var selection, comparer))
				{
					return selection;
				}

				selectionState.Log().Warn(
					$"In the {logTag}, the item '{other}' has been set as the selected in the state but is not present in the list. "
					+ "Selection is being cleared on the list state.");
			}

			return SelectionInfo.Empty;
		}
	}

	public static IListState<T> Create<TKey, TOther>(
		IListFeed<T> source,
		IState<TOther> selectionState,
		Func<T, TKey> keySelector,
		IValueAccessor<TOther, TKey?> foreignKeySelector,
		Func<TOther> defaultFactory,
		TKey? emptySelectionKey,
		string logTag)
		where TKey : notnull
	{
		return new ListFeedSelection<T, TOther>(source, selectionState, InfoToOther, OtherToInfo, logTag);

		Option<TOther> InfoToOther(IImmutableList<T> items, SelectionInfo selection, Option<TOther> other)
		{
			var result = (selection.TryGetSelectedItem(items, out var item), other) switch
			{
				(true, { Type: OptionType.Some }) => Extensions.Option<TOther>.Some(foreignKeySelector.Set((TOther)other, keySelector(item!))),
				(true, _) => Extensions.Option<TOther>.Some(foreignKeySelector.Set(defaultFactory(), keySelector(item!))),
				(false, { Type: OptionType.Some }) => Extensions.Option<TOther>.Some(foreignKeySelector.Set((TOther)other, emptySelectionKey)),
				(false, _) => other
			};

			return result;
		}

		SelectionInfo OtherToInfo(IImmutableList<T> items, Option<TOther> otherOpt)
		{
			if (otherOpt.IsSome(out var other)
				&& foreignKeySelector.Get(other) is { } selectedKey)
			{
				var selectedIndex = IndexOfKey(items, keySelector, selectedKey);
				if (selectedIndex >= 0)
				{
					return SelectionInfo.Single((uint)selectedIndex);
				}

				selectionState.Log().Warn(
					$"In the {logTag}, an item with key '{selectedKey}' has been set as the selected in the state but is not present in the list. "
					+ "Selection is being cleared on the list state.");
			}

			return SelectionInfo.Empty;
		}
	}

	public static IListState<T> CreateValueType<TKey, TOther>(
		IListFeed<T> source,
		IState<TOther> selectionState,
		Func<T, TKey> keySelector,
		IValueAccessor<TOther, TKey?> foreignKeySelector,
		Func<TOther> defaultFactory,
		TKey? emptySelectionKey,
		string logTag)
		where TKey : struct
	{
		return new ListFeedSelection<T, TOther>(source, selectionState, InfoToOther, OtherToInfo, logTag);

		Option<TOther> InfoToOther(IImmutableList<T> items, SelectionInfo selection, Option<TOther> other)
		{
			var result = (selection.TryGetSelectedItem(items, out var item), other) switch
			{
				(true, { Type: OptionType.Some }) => Extensions.Option<TOther>.Some(foreignKeySelector.Set((TOther)other, keySelector(item!))),
				(true, _) => Extensions.Option<TOther>.Some(foreignKeySelector.Set(defaultFactory(), keySelector(item!))),
				(false, { Type: OptionType.Some }) => Extensions.Option<TOther>.Some(foreignKeySelector.Set((TOther)other, emptySelectionKey)),
				(false, _) => other
			};

			return result;
		}

		SelectionInfo OtherToInfo(IImmutableList<T> items, Option<TOther> otherOpt)
		{
			if (otherOpt.IsSome(out var other)
				&& foreignKeySelector.Get(other) is { } selectedKey)
			{
				var selectedIndex = IndexOfKey(items, keySelector, selectedKey);
				if (selectedIndex >= 0)
				{
					return SelectionInfo.Single((uint)selectedIndex);
				}

				selectionState.Log().Warn(
					$"In the {logTag}, an item with key '{selectedKey}' has been set as the selected in the state but is not present in the list. "
					+ "Selection is being cleared on the list state.");
			}

			return SelectionInfo.Empty;
		}
	}

	private static int IndexOfKey<TKey>(IImmutableList<T> items, Func<T, TKey> selector, TKey? key)
		where TKey : notnull
	{
		for (var i = 0; i < items.Count; i++)
		{
			var item = items[i];
			if (selector(item!).Equals(key))
			{
				return i;
			}
		}

		return -1;
	}
}
