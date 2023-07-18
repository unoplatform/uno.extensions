using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources.Pagination;

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
