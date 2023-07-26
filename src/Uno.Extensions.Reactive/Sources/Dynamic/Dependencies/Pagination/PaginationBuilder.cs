using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Sources.Pagination;

internal struct PaginationBuilder<TItem>
{
	public ByIndexPaginationBuilder<TItem> ByIndex(uint firstPage = 0)
		=> new(firstPage);

	public ByCursorPaginationBuilder<TCursor, TItem> ByCursor<TCursor>(TCursor firstPage)
		=> new(firstPage);
}

internal record struct ByIndexPaginationBuilder<TItem>(uint FirstPageIndex)
{
	public PaginationConfiguration<TItem> GetPage(AsyncFunc<PageRequest, IImmutableList<TItem>> getPage)
	{
		var firstPage = new ByIndexCursor<TItem>(FirstPageIndex, 0);
		return new(ct => new PageEnumerator<ByIndexCursor<TItem>, TItem>(firstPage, ByIndexCursor<TItem>.GetPage(getPage), ct));
	}
}

internal record struct ByCursorPaginationBuilder<TCursor, TItem>(TCursor FirstPageCursor)
{
	public PaginationConfiguration<TItem> GetPage(GetPage<TCursor, TItem> getPage)
	{
		var firstPage = FirstPageCursor;
		return new(ct => new PageEnumerator<TCursor, TItem>(firstPage, getPage, ct));
	}
}

internal interface IPageEnumerator<TItem>
{
	public IImmutableList<TItem> Current { get; }

	public ValueTask<bool> MoveNextAsync(uint? desiredPageSize);
}
