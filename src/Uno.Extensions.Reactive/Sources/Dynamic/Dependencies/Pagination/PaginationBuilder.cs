using System;
using System.Collections.Immutable;
using System.Linq;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Sources;

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

internal class PageEnumerator<TCursor, TItem> : IPageEnumerator<TItem>
{
	private TCursor? _nextPage;
	private readonly GetPage<TCursor, TItem> _getPage;
	private readonly CancellationToken _ct;

	public PageEnumerator(TCursor firstPage, GetPage<TCursor, TItem> getPage, CancellationToken ct)
	{
		_nextPage = firstPage;
		_getPage = getPage;
		_ct = ct;
	}

	/// <inheritdoc />
	public IImmutableList<TItem> Current { get; private set; } = ImmutableList<TItem>.Empty;

	/// <inheritdoc />
	public async ValueTask<bool> MoveNextAsync(uint? desiredPageSize)
	{
		var nextPage = _nextPage;
		if (nextPage is null)
		{
			return false;
		}

		(Current, _nextPage) = await _getPage(nextPage, desiredPageSize, _ct).ConfigureAwait(false);

		return true;
	}
}

internal record struct PaginationConfiguration<TItem>(Func<CancellationToken, IPageEnumerator<TItem>> GetEnumerator);

internal interface IPageEnumerator<TItem>
{
	public IImmutableList<TItem> Current { get; }

	public ValueTask<bool> MoveNextAsync(uint? desiredPageSize);
}
