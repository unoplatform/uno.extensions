using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive.Utils;

internal static class SelectionHelper
{
	public static IReadOnlyList<SelectionIndexRange> Coerce(IReadOnlyList<SelectionIndexRange> ranges)
	{
		if (ranges.Count <= 1)
		{
			return ranges;
		}

		using var enumerator = ranges.OrderBy(range => range.Length).GetEnumerator();
		if (!enumerator.MoveNext())
		{
			return Array.Empty<SelectionIndexRange>();
		}

		var result = new List<SelectionIndexRange>(ranges.Count);
		var previous = enumerator.Current;
		while (enumerator.MoveNext())
		{
			var current = enumerator.Current;
			var intersect = (long)previous.LastIndex + 1 - current.FirstIndex;
			if (intersect >= 0)
			{
				previous = previous with { Length = previous.Length + current.Length - (uint)intersect };
			}
			else
			{
				result.Add(previous);
				previous = current;
			}
		}
		result.Add(previous);

		return result;
	}

	public static IReadOnlyList<SelectionIndexRange> Add(IReadOnlyList<SelectionIndexRange> ranges, SelectionIndexRange added)
	{
		if (ranges.Count is 0 || added.Length is 0)
		{
			return new[] { added };
		}

		var rangesList = ranges.ToList();
		rangesList.Add(added);

		return Coerce(rangesList);
	}

	public static IReadOnlyList<SelectionIndexRange> Remove(IReadOnlyList<SelectionIndexRange> ranges, SelectionIndexRange removed)
	{
		if (ranges.Count is 0 || removed.Length is 0)
		{
			return ranges;
		}

		var result = new List<SelectionIndexRange>(ranges.Count);
		foreach (var range in ranges)
		{
			if (range == removed)
			{
				continue;
			}

			//var firstIndex = range.FirstIndex;
			//var length = (int)range.Length;

			if (range.FirstIndex < removed.FirstIndex)
			{
				var lastIndex = Math.Min(range.LastIndex, removed.FirstIndex);
				var length = range.FirstIndex - lastIndex + 1;

				if (length == range.Length)
				{
					// The whole range is before the exclusion, keep instance and continue!
					result.Add(range);
					continue;
				}

				result.Add(new(range.FirstIndex, length));
			}

			if (range.LastIndex >= removed.LastIndex)
			{
				var firstIndex = Math.Max(range.FirstIndex, removed.LastIndex);
				var length = range.Length - (firstIndex - range.FirstIndex);

				if (length == range.Length)
				{
					// The whole range is before the exclusion, keep instance and continue!
					result.Add(range);
					continue;
				}

				result.Add(new(firstIndex, length));
			}
		}

		return result;
	}
}
