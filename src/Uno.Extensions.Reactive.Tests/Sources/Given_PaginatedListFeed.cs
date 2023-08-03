using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Testing;
using static System.Linq.Enumerable;

namespace Uno.Extensions.Reactive.Tests.Sources;

[TestClass]
public class Given_PaginatedListFeed : FeedTests
{
	[TestMethod]
	public async Task When_RequestPage_Then_ItemsAdded()
	{
		var sut = ListFeed.PaginatedAsync<int>(async (page, ct) => Range((int)page.Index * 20, (int)(page.DesiredSize ?? 20)).ToImmutableList());
		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);

		var result = sut.Record(ctx);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(0, 20)))
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.HasMore))
		);

		requests.RequestMoreItems(42);
		await result.WaitForMessages(2, CT);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(0, 20)))
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(20, Range(20, 42)))
				.Current(Items.Range(62), Error.No, Progress.Final, Pagination.HasMore))
		);
	}

	[TestMethod]
	public async Task When_ByIndexAndPageIsEmpty_Then_ReportPaginationCompleted()
	{
		async ValueTask<IImmutableList<int>> GetPage(PageRequest page, CancellationToken ct)
			=> page.Index switch
			{
				0 => Range(0, 10).ToImmutableList(),
				1 => Range(10, 10).ToImmutableList(),
				2 => ImmutableList<int>.Empty,
				_ => throw new InvalidOperationException("Should not happen"),
			};

		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);
		var result = ListFeed.PaginatedAsync(GetPage).Record(ctx);

		requests.RequestMoreItems(42);
		await result.WaitForMessages(2, CT);

		requests.RequestMoreItems(42);
		await result.WaitForMessages(3, CT);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(0, 10)))
				.Current(Items.Range(10), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(10, Range(10, 10)))
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Pagination)
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.Completed))
		);
	}

	[TestMethod]
	public async Task When_ByIndexAndFirstPageIsEmpty_Then_ReportPaginationCompleted()
	{
		async ValueTask<IImmutableList<int>> GetPage(PageRequest page, CancellationToken ct)
			=> page.Index switch
			{
				0 => ImmutableList<int>.Empty,
				_ => throw new InvalidOperationException("Should not happen"),
			};

		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);
		var result = ListFeed.PaginatedAsync(GetPage).Record(ctx);

		await result.WaitForMessages(1, CT);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.NotChanged)
				.Current(Data.None, Error.No, Progress.Final, Pagination.Completed))
		);
	}

	[TestMethod]
	public async Task When_ByCursorAndGetPageHasNoNext_Then_ReportPaginationCompleted()
	{
		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);
		var result = ListFeed<int>.PaginatedByCursorAsync(new TestCursor(2), TestCursor.GetPage).Record(ctx);

		await result.WaitForMessages(1, CT);
		requests.RequestMoreItems(42);
		await result.WaitForMessages(2, CT);
		requests.RequestMoreItems(42);
		await result.WaitForMessages(3, CT);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(0, 10)))
				.Current(Items.Range(10), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(10, Range(10, 10)))
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(20, Range(20, 10)))
				.Current(Items.Range(30), Error.No, Progress.Final, Pagination.Completed))
		);
	}

	[TestMethod]
	public async Task When_ByCursorAndPageHasNoNext_Then_ReportPaginationCompleted()
	{
		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);
		var result = ListFeed<int>.PaginatedByCursorAsync(new TestCursor(0), TestCursor.GetPage).Record(ctx);

		await result.WaitForMessages(1, CT);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(0, 10)))
				.Current(Items.Range(10), Error.No, Progress.Final, Pagination.Completed))
		);
	}

	[TestMethod]
	public async Task When_ByCursorAndFirstHasNoNext_Then_ReportPaginationCompleted()
	{
		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);
		var result = ListFeed<int>.PaginatedByCursorAsync(new TestCursor(0), TestCursor.GetPage).Record(ctx);

		await result.WaitForMessages(1, CT);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(0, 10)))
				.Current(Items.Range(10), Error.No, Progress.Final, Pagination.Completed))
		);
	}

	[TestMethod]
	public async Task When_ByCursorAndFirstIsEmptyAndHasNoNext_Then_ReportPaginationCompleted()
	{
		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);
		var result = ListFeed<int>.PaginatedByCursorAsync(new TestCursor(0, PageSize: 0), TestCursor.GetPage).Record(ctx);

		await result.WaitForMessages(1, CT);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.NotChanged)
				.Current(Data.None, Error.No, Progress.Final, Pagination.Completed))
		);
	}

	[TestMethod]
	public async Task When_ByCursorAndPagesAreEmptyButHasNext_Then_ContinuePagination()
	{
		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);
		var result = ListFeed<int>.PaginatedByCursorAsync(new TestCursor(1, PageSize: 0), TestCursor.GetPage).Record(ctx);

		await result.WaitForMessages(1, CT);
		requests.RequestMoreItems(42);
		await result.WaitForMessages(2, CT);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.NotChanged)
				.Current(Data.None, Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Pagination)
				.Current(Data.None, Error.No, Progress.Final, Pagination.Completed))
		);
	}

	[TestMethod]
	public async Task When_FirstPageThrow_Then_GoInErrorAndIgnorePageRequest()
	{
		async ValueTask<IImmutableList<int>> GetPage(PageRequest page, CancellationToken ct)
			=> page.Index switch
			{
				0 => throw new TestException(),
				_ => Range(0,10).ToImmutableList()
			};

		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);
		var result = ListFeed.PaginatedAsync(GetPage).Record(ctx);

		await result.WaitForMessages(1);
		try
		{
			requests.RequestMoreItems(42);
			await result.WaitForMessages(2, 100);
		}
		catch (TimeoutException)
		{
		}

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Error & Changed.Pagination)
				.Current(Data.Undefined, typeof(TestException), Progress.Final, Pagination.Completed))
		);
	}

	[TestMethod]
	public async Task When_SubsequentPagesThrow_Then_GoInErrorAndKeepDataAndKeepListeningPageRequest()
	{
		async ValueTask<IImmutableList<int>> GetPage(PageRequest page, CancellationToken ct)
			=> page.Index switch
			{
				0 => Range(0, 10).ToImmutableList(),
				_ => throw new TestException(),
			};

		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);
		var result = ListFeed.PaginatedAsync(GetPage).Record(ctx);

		await result.WaitForMessages(1, CT);
		requests.RequestMoreItems(42);
		await result.WaitForMessages(2, CT);
		requests.RequestMoreItems(42);
		await result.WaitForMessages(3, CT);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(0, 10)))
				.Current(Items.Range(10), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Error & Changed.Pagination)
				.Current(Items.Range(10), typeof(TestException), Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Error & Changed.Pagination)
				.Current(Items.Range(10), typeof(TestException), Progress.Final, Pagination.HasMore))
		);
	}

	[TestMethod]
	public async Task When_Refresh()
	{
		async ValueTask<IImmutableList<int>> GetPage(PageRequest page, CancellationToken ct)
			=> page.Index switch
			{
				0 => Range(0, 10).ToImmutableList(),
				1 => Range(10, 10).ToImmutableList(),
				_ => throw new TestException()
			};

		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);
		var result = ListFeed.PaginatedAsync(GetPage).Record(ctx);

		await result.WaitForMessages(1, CT);
		requests.RequestMoreItems(42);
		await result.WaitForMessages(2, CT);
		requests.RequestRefresh();
		await result.WaitForMessages(3, CT);
		requests.RequestMoreItems(42);
		await result.WaitForMessages(4, CT);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(0, 10)))
				.Current(Items.Range(10), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(at: 10, Range(10, 10)))
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination & Changed.Refreshed, Items.Reset(Range(0, 20), Range(0, 10)))
				.Current(Items.Range(10), Error.No, Progress.Final, Pagination.HasMore, Refreshed.Is(1)))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(at: 10, Range(10, 10)))
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.HasMore, Refreshed.Is(1)))
		);
	}

	[TestMethod]
	public async Task When_Async_Then_FlagIsLoading()
	{
		var delay = new TaskCompletionSource<Unit>();

		void GetNext()
			=> Interlocked.Exchange(ref delay, new TaskCompletionSource<Unit>())!.SetResult(default);

		async ValueTask<IImmutableList<int>> GetPage(PageRequest page, CancellationToken ct)
		{
			await delay.Task;
			return page.Index switch
			{
				0 => Range(0, 10).ToImmutableList(),
				1 => Range(10, 10).ToImmutableList(),
				_ => throw new TestException()
			};
		}

		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);
		var result = ListFeed.PaginatedAsync(GetPage).Record(ctx);

		await result.WaitForMessages(1, CT);
		GetNext();
		await result.WaitForMessages(2, CT);
		requests.RequestMoreItems(42);
		await result.WaitForMessages(3, CT);
		GetNext();
		await result.WaitForMessages(4, CT);
		requests.RequestMoreItems(42);
		await result.WaitForMessages(5, CT);
		GetNext();
		await result.WaitForMessages(6, CT);
		requests.RequestRefresh();
		await result.WaitForMessages(7, CT);
		GetNext();
		await result.WaitForMessages(8, CT);
		requests.RequestMoreItems(42);
		await result.WaitForMessages(9, CT);
		GetNext();
		await result.WaitForMessages(10, CT);
		requests.RequestMoreItems(42);
		await result.WaitForMessages(11, CT);
		GetNext();
		await result.WaitForMessages(12, CT);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Pagination & Changed.Progress)
				.Current(Data.Undefined, Error.No, Progress.Transient, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Progress, Items.Reset(Range(0, 10)))
				.Current(Items.Range(10), Error.No, Progress.Final, Pagination.HasMore))
			// Page 1 => Get Items
			.Message(m => m
				.Changed(Changed.Pagination)
				.Current(Items.Range(10), Error.No, Progress.Final, Pagination.Loading))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(at: 10, Range(10, 10)))
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.HasMore))
			// Page 2 => Throws
			.Message(m => m
				.Changed(Changed.Pagination)
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.Loading))
			.Message(m => m
				.Changed(Changed.Error & Changed.Pagination)
				.Current(Items.Range(20), typeof(TestException), Progress.Final, Pagination.HasMore))
			// Refresh request
			.Message(m => m
				.Changed(Changed.Refreshed & Changed.Pagination & Changed.Progress)
				.Current(Items.Range(20), typeof(TestException), Progress.Transient, Pagination.HasMore, Refreshed.Is(1)))
			.Message(m => m
				.Changed(Changed.Data & Changed.Error & Changed.Progress, Items.Reset(Range(0, 20), Range(0, 10)))
				.Current(Items.Range(10), Error.No, Progress.Final, Pagination.HasMore, Refreshed.Is(1)))
			// Page 1 => Get Items
			.Message(m => m
				.Changed(Changed.Pagination)
				.Current(Items.Range(10), Error.No, Progress.Final, Pagination.Loading, Refreshed.Is(1)))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(at: 10, Range(10, 10)))
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.HasMore, Refreshed.Is(1)))
			// Page 2 => Throws
			.Message(m => m
				.Changed(Changed.Pagination)
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.Loading, Refreshed.Is(1)))
			.Message(m => m
				.Changed(Changed.Error & Changed.Pagination)
				.Current(Items.Range(20), typeof(TestException), Progress.Final, Pagination.HasMore, Refreshed.Is(1)))
		);
	}

	private record TestCursor(int RemainingPages, int PageIndex = 0, int PageSize = 10)
	{
		public TestCursor? Next => RemainingPages > 0 ? this with { RemainingPages = RemainingPages - 1, PageIndex = PageIndex + 1 } : default;

		public IImmutableList<int> Items => Range(PageIndex * PageSize, PageSize).ToImmutableList();

		public static async ValueTask<PageResult<TestCursor, int>> GetPage(TestCursor cursor, uint? desiredPageSize, CancellationToken ct)
			=> new(cursor.Items, cursor.Next);
	}
}
