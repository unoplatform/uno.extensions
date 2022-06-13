using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive;

internal record PaginationInfo
{
	public bool HasMoreItems { get; init; }

	public bool IsLoadingMoreItems { get; init; }

	public TokenCollection<PageToken> Tokens { get; init; } = TokenCollection<PageToken>.Empty;

	internal static PaginationInfo Aggregate(IReadOnlyCollection<PaginationInfo> values)
	{
		bool hasMoreItems = false, isLoadingMoreItems = false;
		foreach (var value in values)
		{
			hasMoreItems |= value.HasMoreItems;
			isLoadingMoreItems |= value.IsLoadingMoreItems;
		}

		var tokens = TokenCollection<PageToken>.Aggregate(values.Select(value => value.Tokens));

		return new PaginationInfo
		{
			HasMoreItems = hasMoreItems,
			IsLoadingMoreItems = isLoadingMoreItems,
			Tokens = tokens
		};
	}
}


internal class RefreshAxis : MessageAxis<TokenCollection<RefreshToken>>
{
	public static RefreshAxis Instance { get; } = new();

	private RefreshAxis()
		: base(MessageAxes.Refresh, TokenCollection<RefreshToken>.Aggregate)
	{
	}

	/// <inheritdoc />
	public override MessageAxisValue ToMessageValue(TokenCollection<RefreshToken>? value)
		=> value?.Tokens is {Count:1} singleToken && singleToken[0].SequenceId == 0
			? MessageAxisValue.Unset
			: base.ToMessageValue(value);
}
