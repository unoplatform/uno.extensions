using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Uno.Extensions.Reactive.Testing;

public class Pagination : AxisConstraint
{
	private readonly bool _isLoading;
	private readonly bool _hasMoreItems;

	public static Pagination HasMore { get; } = new(isLoading: false, hasMoreItems: true);

	public static Pagination Loading { get; } = new(isLoading: true, hasMoreItems: true); // We cannot be loading if does not have more items!

	public static Pagination Completed { get; } = new(isLoading: false, hasMoreItems: false);

	/// <inheritdoc />
	public override MessageAxis ConstrainedAxis => MessageAxis.Pagination;

	public Pagination(bool isLoading, bool hasMoreItems)
	{
		_isLoading = isLoading;
		_hasMoreItems = hasMoreItems;
	}

	/// <inheritdoc />m
	public override void Assert(IMessageEntry actual)
	{
		var pageInfo = actual.GetPaginationInfo();

		pageInfo.Should().NotBeNull();

		using (AssertionScope.Current.ForContext("IsLoadingMoreItems"))
		{
			pageInfo?.IsLoadingMoreItems.Should().Be(_isLoading);
		}

		using (AssertionScope.Current.ForContext("HasMoreItems"))
		{
			pageInfo?.HasMoreItems.Should().Be(_hasMoreItems);
		}
	}

}
