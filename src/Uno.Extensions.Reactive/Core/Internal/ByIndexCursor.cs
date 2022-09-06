using System;
using System.Collections.Immutable;
using System.Linq;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive.Core;

internal record ByIndexCursor<T>(uint Index, uint TotalCount)
{
	public static ByIndexCursor<T> First { get; } = new(0, 0);

	public static GetPage<ByIndexCursor<T>, T> GetPage(AsyncFunc<Reactive.PageRequest, IImmutableList<T>> getPage) => async (cursor, desiredCount, ct) =>
	{
		var request = new Reactive.PageRequest
		{
			Index = cursor.Index,
			CurrentCount = cursor.TotalCount,
			DesiredSize = desiredCount
		};

		if (await getPage(request, ct).ConfigureAwait(false) is { Count: > 0 } items)
		{
			var nextCursor = cursor with { Index = cursor.Index + 1, TotalCount = cursor.TotalCount + (uint)items.Count };

			return new PageResult<ByIndexCursor<T>, T>(items, nextCursor);
		}
		else
		{
			return PageResult<ByIndexCursor<T>, T>.Empty;
		}
	};
}
