using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// Load a page of items of a paginated list.
/// </summary>
/// <typeparam name="TCursor">Type of the cursor used by the pagination</typeparam>
/// <typeparam name="TItem">Type of the items of the list.</typeparam>
/// <param name="cursor">The cursor of teh page to load.</param>
/// <param name="desiredPageSize">The desired page size if any.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask<PageResult<TCursor, TItem>> GetPage<TCursor, TItem>(TCursor cursor, uint? desiredPageSize, CancellationToken ct);

/// <summary>
/// Load a page of items of a paginated list.
/// </summary>
/// <typeparam name="TParam">Type of the parameter used to build the paginated list.</typeparam>
/// <typeparam name="TCursor">Type of the cursor used by the pagination</typeparam>
/// <typeparam name="TItem">Type of the items of the list.</typeparam>
/// <param name="parameter">The parameter used to create the paginated list (changing the value of this will reset the cursor to the first page).</param>
/// <param name="cursor">The cursor of the page to load.</param>
/// <param name="desiredPageSize">The desired page size if any.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask<PageResult<TCursor, TItem>> GetPage<in TParam, TCursor, TItem>(TParam parameter, TCursor cursor, uint? desiredPageSize, CancellationToken ct);

/// <summary>
/// A page of items for a paginated list of items.
/// </summary>
/// <typeparam name="TCursor">The cursor used to track the pagination.</typeparam>
/// <typeparam name="TItem">The type of items of the collection.</typeparam>
/// <param name="Items">The items</param>
/// <param name="NextPage">The cursor to use to get the next page, if any.</param>
public record struct PageResult<TCursor, TItem>(IImmutableList<TItem> Items, TCursor? NextPage)
{
	/// <summary>
	/// An empty page indicating the end of the list.
	/// </summary>
	public static PageResult<TCursor, TItem> Empty => new(ImmutableList<TItem>.Empty, NextPage: default);
}
