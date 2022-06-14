using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Info about the pagination of a ListFeed
/// </summary>
internal record PaginationInfo
{
	/// <summary>
	/// Indicates that the source does has more items to load or not.
	/// </summary>
	public bool HasMoreItems { get; init; }

	/// <summary>
	/// Indicates that the source is currently loading a subsequent page.
	/// </summary>
	/// <remarks>
	/// This is equivalent to the <see cref="MessageAxis.Progress"/> but specialized to the subsequent pages,
	/// as a ListFeed is expected to go in transient state only while loading the first page.
	/// </remarks>
	public bool IsLoadingMoreItems { get; init; }

	/// <summary>
	/// A set of tokens that allows a subscriber to track the progress of a <see cref="PageRequest"/>.
	/// </summary>
	public TokenSet<PageToken> Tokens { get; init; } = TokenSet<PageToken>.Empty;

	internal static PaginationInfo Aggregate(IReadOnlyCollection<PaginationInfo> values)
	{
		bool hasMoreItems = false, isLoadingMoreItems = false;
		foreach (var value in values)
		{
			hasMoreItems |= value.HasMoreItems;
			isLoadingMoreItems |= value.IsLoadingMoreItems;
		}

		var tokens = TokenSet<PageToken>.Aggregate(values.Select(value => value.Tokens));

		return new PaginationInfo
		{
			HasMoreItems = hasMoreItems,
			IsLoadingMoreItems = isLoadingMoreItems,
			Tokens = tokens
		};
	}
}
