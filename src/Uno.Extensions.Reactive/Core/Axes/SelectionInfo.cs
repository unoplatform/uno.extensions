using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Contains information about selected items of a collection.
/// </summary>
public sealed record SelectionInfo
{
	/// <summary>
	/// The collection of the selected ranges.
	/// </summary>
	public IReadOnlyList<SelectionIndexRange> Ranges { get; }

	// The ctor is private so we can assume that we don't have any empty range
	// (So Ranges.Count == 0 means empty and Ranges.Count > 1 means multiple!)
	// This also ensure that we don't have any overlap.
	private SelectionInfo(IReadOnlyList<SelectionIndexRange> ranges)
	{
		Ranges = ranges;
	}

	/// <summary>
	/// Gets an instance containing no selected item.
	/// </summary>
	public static SelectionInfo Empty { get; } = new(ImmutableList<SelectionIndexRange>.Empty);

	/// <summary>
	/// Creates a new instance containing only a single selected item.
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public static SelectionInfo Single(uint index) => new(new[] { new SelectionIndexRange(index, 1) });

	internal static bool TryCreateSingle<T>(
		IImmutableList<T> items,
		T selectedItem,
		[NotNullWhen(true)] out SelectionInfo? selectionInfo,
		IEqualityComparer<T>? comparer = null)
	{
		if (items is null or { Count: 0 })
		{
			selectionInfo = default;
			return false;
		}

		var selectedIndex = comparer is null ? items.IndexOf(selectedItem) : items.IndexOf(selectedItem, comparer);
		if (selectedIndex < 0)
		{
			selectionInfo = default;
			return false;
		}

		selectionInfo = Single((uint)selectedIndex);
		return true;
	}

	internal static bool TryCreateMultiple<T>(
		IImmutableList<T> items,
		IImmutableList<T> selectedItems,
		[NotNullWhen(true)] out SelectionInfo? selectionInfo,
		IEqualityComparer<T>? comparer = null)
	{
		if (selectedItems is null or { Count: 0 })
		{
			selectionInfo = Empty;
			return true;
		}

		if (items is null or { Count: 0 })
		{
			selectionInfo = default;
			return false;
		}

		var indexOf = comparer is null ? (Func<T, int>)items.IndexOf : item => items.IndexOf(item, comparer);
		var selectedIndexes = selectedItems.Select(indexOf).ToList();
		selectedIndexes.Sort();
		if (selectedIndexes[0] < 0)
		{
			selectionInfo = default;
			return false;
		}

		var ranges = new List<SelectionIndexRange>();
		(int start, uint count) range = (-1, 0);
		foreach (var index in selectedIndexes)
		{
			if (range is {start: -1})
			{
				range = (index, 1);
			}
			else if (range.start + range.count == index)
			{
				range.count++;
			}
			else
			{
				ranges.Add(new SelectionIndexRange((uint)range.start, range.count));
				range = (index, 1);
			}
		}

		// The current 'range' cannot be empty here since the selectedItems has at least one element!
		ranges.Add(new SelectionIndexRange((uint)range.start, range.count));

		selectionInfo = new SelectionInfo(ranges);
		return true;
	}

	/// <summary>
	/// Indicates if there is any selected item or not.
	/// </summary>
	public bool IsEmpty => Ranges is null or { Count: 0 };

	/// <summary>
	/// Gets total number of selected items.
	/// </summary>
	public uint Count => (uint?)Ranges?.Sum(range => range.Length) ?? 0;

	// Note: There no way to properly aggregate selection on multiple sources.
	internal static SelectionInfo Aggregate(IReadOnlyCollection<SelectionInfo> values)
		=> Empty;

	internal bool TryGetSelectedItem<T>(IImmutableList<T> items, [NotNullWhen(true)] out T? selectedItem, bool failIfOutOfRange = true, bool failIfMultiple = false)
	{
		if (TryGetSelectedIndex(items, out var selectedIndex, failIfOutOfRange, failIfMultiple))
		{
			selectedItem = items[(int)selectedIndex]!;
			return true;
		}

		selectedItem = default!;
		return true;
	}

	internal bool TryGetSelectedIndex<T>(IImmutableList<T> items, [NotNullWhen(true)] out uint? selectedIndex, bool failIfOutOfRange = true, bool failIfMultiple = false)
	{
		if (IsEmpty)
		{
			selectedIndex = default;
			return false;
		}

		if (failIfMultiple && (Ranges.Count > 0 || Ranges[0].Length > 1))
		{
			throw new InvalidOperationException("Multiple selected items.");
		}

		var index = Ranges[0].FirstIndex;
		if (index >= items.Count)
		{
			if (failIfOutOfRange)
			{
				throw new IndexOutOfRangeException($"This selection info starts with index {index}, but the provided collection has only {items.Count} items.");
			}

			selectedIndex = default!;
			return false;
		}

		selectedIndex = index;
		return true;
	}

	internal T? GetSelectedItem<T>(IImmutableList<T> items, bool failIfOutOfRange = true, bool failIfMultiple = false)
		=> TryGetSelectedItem(items, out var selectedItem, failIfOutOfRange, failIfMultiple) ? selectedItem : default;

	internal uint? GetSelectedIndex<T>(IImmutableList<T> items, bool failIfOutOfRange = true, bool failIfMultiple = false)
		=> TryGetSelectedIndex(items, out var selectedIndex, failIfOutOfRange, failIfMultiple) ? selectedIndex : default;

	internal IImmutableList<T> GetSelectedItems<T>(IImmutableList<T> items, bool failIfOutOfRange = true)
	{
		if (IsEmpty)
		{
			return ImmutableList<T>.Empty;
		}

		// TODO: Create a sparse collection for that
		var selectedItems = ImmutableList.CreateBuilder<T>();
		foreach (var range in Ranges)
		{
			if (failIfOutOfRange && range.LastIndex >= items.Count)
			{
				throw new IndexOutOfRangeException($"This selection includes items from {range.FirstIndex} to {range.LastIndex} (included), but the provided collection has only {items.Count} items.");
			}
			selectedItems.AddRange(items.Skip((int)range.FirstIndex).Take((int)range.Length));
		}

		return selectedItems.ToImmutable();
	}

	/// <inheritdoc />
	public override string ToString()
		=> IsEmpty
			? "--Empty--"
			: string.Join(" & ", Ranges);
}
