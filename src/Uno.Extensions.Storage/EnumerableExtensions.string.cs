namespace Uno.Extensions.Storage.Enumerable;

/// <summary>
/// Provides <see cref="IEnumerable{TResult}"/> extension methods for working with <see cref="IStorage"/>
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Selects string typed items from the input collection based on the specified ranges.
    /// </summary>
    /// <param name="source">The collection of string typed items to select from.</param>
    /// <param name="ranges">
    /// A collection of ranges, where each range is a tuple containing a start and end index.
    /// The start index specifies the first item to include, and the end index specifies the last item to include.
    /// </param>
    /// <param name="isNullBased">
    /// Indicates whether the range indices are 0-based (<c>true</c>) or 1-based (<c>false</c>).
    /// If <c>true</c>, the start and end indices are treated as 0-based; otherwise, they are treated as 1-based.
    /// </param>
    /// <returns>
    /// An enumerable collection of strings, where each string represents the items within a specified range.
    /// If a range is invalid (e.g. start is greater than the last one), an empty string is returned for that range.
    /// </returns>
    public static IEnumerable<string> SelectItemsByRanges(this IEnumerable<string> source, IEnumerable<(int Start, int End)> ranges, bool isNullBased = true)
    {
		if (source is null || ranges is null)
		{
			yield return string.Empty;
			yield break;
		}

		if (!ranges.Any())
		{
			yield return source.JoinBy(Environment.NewLine);
			yield break;
		}

		foreach (var range in ranges)
        {
            yield return source.GetItemsWithinRange(range, isNullBased);
        }
    }

    /// <summary>
    /// Retrieves the concatenated items within the specified range as one single <see langword="string"/> joined by <see cref="Environment.NewLine"/> character.
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable{TData}"/> to select from.</param>
    /// <param name="range">
    /// A tuple containing the start and end indices of the range as <see langword="int"/>.
    /// The start index specifies the first item to include, and the end index specifies the last item to include.
    /// </param>
    /// <param name="isNullBased">
    /// Indicates whether the range indices are 0-based (<c>true</c>) or 1-based (<c>false</c>).
    /// If <c>true</c>, the start and end indices are treated as 0-based; otherwise, they are treated as 1-based.
    /// </param>
    /// <returns>
    /// A string containing the string typed items of <paramref name="source"/> within the specified range, joined by the system's newline character.
    /// </returns>
    public static string GetItemsWithinRange(this IEnumerable<string> source, (int Start, int End) range, bool isNullBased = true) // TODO: Consider to limit int to min 0 value instead of implicit allowing negative.
    {
		if (source is null)
		{
			return string.Empty;
		}

		var list = source as IList<string> ?? [.. source];
		if (list.Count == 0)
		{
			return string.Empty;
		}

		var startIndex = Math.Clamp(
            value: range.Start - (isNullBased ? 0 : 1),
            min: 0,
            max: list.Count);

        var endIndex = Math.Clamp(
            value: range.End - (isNullBased ? 0 : 1),
            min: startIndex,
            max: list.Count); // Ensure 'End' does not exceed available lines

        return list.Skip(startIndex)
                   .Take(endIndex - startIndex)
                   .JoinBy(Environment.NewLine);
    }
}
