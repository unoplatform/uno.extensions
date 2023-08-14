using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Testing;
using static System.Linq.Enumerable;

namespace Uno.Extensions.Reactive.Tests.Sources;

[TestClass]
public class Given_DynamicFeed : FeedTests
{
	[TestMethod]
	public void When_DelegateSync_Then_LoadSync()
	{
		var sut = new DynamicFeed<int>(async _ => 42).Record();

		sut.Should().Be(b => b
			.Message(42, Error.No, Progress.Final));
	}

	[TestMethod]
	public async Task When_DelegateSlightlyAsync_Then_NoProgress()
	{
		async ValueTask<int> Load(CancellationToken ct)
		{
			await Task.Yield();

			return 42;
		}

		var sut = new DynamicFeed<int>(Load).Record();

		await sut.Should().BeAsync(b => b
			.Message(42, Error.No, Progress.Final));
	}

	[TestMethod]
	public async Task When_AwaitFeed_And_FeedUpdated_Then_ReloadDependent()
	{
		var myFeed = State<int>.Value(this, () => 42);

		async ValueTask<int> Load(CancellationToken ct)
		{
			var myValue = await myFeed;

			return myValue * 100;
		}

		var result = new DynamicFeed<int>(Load).Record();

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data)
				.Current(4200, Error.No, Progress.Final))
		);

		await myFeed.Set(43, CT);

		await result.Should().BeAsync(b => b
			.Message(m => m
				.Changed(Changed.Data)
				.Current(4200, Error.No, Progress.Final))
			.Message(m => m
				.Changed(Changed.Data)
				.Current(4300, Error.No, Progress.Final))
		);
	}

	[TestMethod]
	public async Task When_AwaitFeed_And_FeedUpdatedMutipleTimes_Then_ReloadDependent()
	{
		var myFeed = State<int>.Value(this, () => 42);

		async ValueTask<int> Load(CancellationToken ct)
		{
			var myValue = await myFeed;

			return myValue * 100;
		}

		var result = new DynamicFeed<int>(Load).Record();

		await myFeed.Set(43, CT);
		await myFeed.Set(44, CT);
		await myFeed.Set(45, CT);

		await result.Should().BeAsync(b => b
			.Message(m => m
				.Changed(Changed.Data)
				.Current(4200, Error.No, Progress.Final))
			.Message(m => m
				.Changed(Changed.Data)
				.Current(4300, Error.No, Progress.Final))
			.Message(m => m
				.Changed(Changed.Data)
				.Current(4400, Error.No, Progress.Final))
			.Message(m => m
				.Changed(Changed.Data)
				.Current(4500, Error.No, Progress.Final))
		);
	}

	[TestMethod]
	[Ignore("Failing build - https://github.com/unoplatform/uno.extensions/issues/1753")]
	public async Task When_AwaitFeedMultipleTime_Then_GetSameInstance()
	{
		object initial = new(), updated = new();
		var myFeed = State<object>.Value(this, () => initial);

		async ValueTask<bool> Load(CancellationToken ct)
		{
			var myValue1 = await myFeed;

			await myFeed.Update(_ => updated, ct);
			await Task.Delay(10, ct);

			var myValue2 = await myFeed;

			return object.ReferenceEquals(myValue1, myValue2) && object.ReferenceEquals(myValue1, updated);
		}

		var result = new DynamicFeed<bool>(Load).Record();

		await result.Should().BeAsync(b => b
			.Message(m => m
				.Changed(Changed.Progress)
				.Current(Data.Undefined, Error.No, Progress.Transient))
			.Message(m => m
				.Changed(Changed.Data & Changed.Progress)
				.Current(true, Error.No, Progress.Final))
		);
	}

	[TestMethod]
	[Ignore("Failing build - https://github.com/unoplatform/uno.extensions/issues/1753")]
	public async Task When_UpdateAwaitedFeed_Then_CancelAndReExecute()
	{
		int before = 0, after = 0;
		object initial = new(), updated = new();
		var myFeed = State<object>.Value(this, () => initial);

		async ValueTask<object?> Load(CancellationToken ct)
		{
			before++;
			await myFeed;

			await myFeed.Update(_ => updated, ct);
			await Task.Delay(10, ct); // Here is the cancellable point

			after++;
			await myFeed;

			return new object();
		}

		var result = new DynamicFeed<object>(Load).Record();

		await result.Should().BeAsync(b => b
			.Message(m => m
				.Changed(Changed.Progress)
				.Current(Data.Undefined, Error.No, Progress.Transient))
			.Message(m => m
				.Changed(Changed.Data & Changed.Progress)
				.Current(Data.Some, Error.No, Progress.Final))
		);

		before.Should().Be(2);
		after.Should().Be(1);
	}

	[TestMethod]
	public async Task When_Await2Feeds_And_AnyUpdated_Then_Reload()
	{
		var myFeed1 = State<int>.Value(this, () => 42);
		var myFeed2 = State<int>.Value(this, () => 42);

		async ValueTask<int> Load(CancellationToken ct)
		{
			var myValue1 = await myFeed1;
			var myValue2 = await myFeed2;

			return myValue1 * myValue2;
		}

		var result = new DynamicFeed<int>(Load).Record();

		await myFeed1.Set(43, CT);
		await myFeed2.Set(43, CT);

		await result.Should().BeAsync(b => b
			.Message(m => m
				.Changed(Changed.Data)
				.Current(42 * 42, Error.No, Progress.Final))
			.Message(m => m
				.Changed(Changed.Data)
				.Current(43 * 42, Error.No, Progress.Final))
			.Message(m => m
				.Changed(Changed.Data)
				.Current(43 * 43, Error.No, Progress.Final))
		);
	}

	[TestMethod]
	public async Task When_AwaitFeed_And_UpdateUntouchedAxis_Then_AxisPropagatedWithoutReload()
	{
		var myFeed = State<int>.Value(this, () => 42);
		var myAxis = new MessageAxis<string>("test_axis", string.Concat);

		var loadCount = 0;
		async ValueTask<int> Load(CancellationToken ct)
		{
			loadCount++;

			var myValue = await myFeed;

			return myValue * 100;
		}

		var result = new DynamicFeed<int>(Load).Record();

		await myFeed.UpdateMessage(msg => msg.Set(myAxis, "hello world"), CT);

		await result.Should().BeAsync(b => b
			.Message(m => m
				.Changed(Changed.Data)
				.Current(4200, Error.No, Progress.Final))
			.Message(m => m
				.Changed(myAxis)
				.Current(4200, Error.No, Progress.Final, Axis.Set(myAxis, "hello world")))
		);
		loadCount.Should().Be(1);
	}

	[TestMethod]
	public async Task When_AwaitFeedWithError_And_Catch_Then_NoError()
	{
		var myFeed = State<int>.Value(this, () => 42);

		async ValueTask<int> Load(CancellationToken ct)
		{
			try
			{
				return await myFeed;
			}
			catch (TestException)
			{
				return -42;
			}
		}

		var result = new DynamicFeed<int>(Load).Record();

		await myFeed.UpdateMessage(msg => msg.Error(new TestException()), CT);
		await myFeed.UpdateMessage(msg => msg.Error(null).Data(43), CT);

		await result.Should().BeAsync(b => b
			.Message(m => m
				.Changed(Changed.Data)
				.Current(42, Error.No, Progress.Final))
			.Message(m => m
				.Changed(Changed.Data & Changed.Error) // <<- Known issue, the error should not be propagated since it has been touched / handled in the Load method
				.Current(-42, typeof(TestException), Progress.Final)) // <<- Known issue, the error should not be propagated since it has been touched / handled in the Load method
			.Message(m => m
				.Changed(Changed.Data & Changed.Error) // <<- Known issue, the error should not be propagated since it has been touched / handled in the Load method
				.Current(43, Error.No, Progress.Final))
		);
	}


	[TestMethod]
	public async Task When_Paginated_And_PageRequested_Then_Reload_And_ItemsAdded()
	{
		async ValueTask<IImmutableList<int>?> Load(CancellationToken ct)
		{
			var items = await FeedExecution.Current!.GetPaginated<int>(b => b.ByIndex().GetPage(LoadPage));

			return items;
		}

		async ValueTask<IImmutableList<int>> LoadPage(PageRequest page, CancellationToken ct)
			=> Range((int)page.Index * 20, (int)(page.DesiredSize ?? 20)).ToImmutableList();

		var sut = new DynamicFeed<IImmutableList<int>?>(Load);
		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);

		var result = sut.Record(ctx);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination)
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.HasMore))
		);

		requests.RequestMoreItems(42);

		await result.Should().BeAsync(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination)
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination)
				.Current(Items.Range(62), Error.No, Progress.Final, Pagination.HasMore))
		);

		Console.WriteLine(result.ToString());
	}

	[TestMethod]
	public async Task When_PaginatedAsListFeed_And_PageRequested_Then_Reload_And_ItemsAddedWithCollectionChanges()
	{
		async ValueTask<IImmutableList<int>?> Load(CancellationToken ct)
		{
			var items = await FeedExecution.Current!.GetPaginated<int>(b => b.ByIndex().GetPage(LoadPage));

			return items;
		}

		async ValueTask<IImmutableList<int>> LoadPage(PageRequest page, CancellationToken ct)
			=> Range((int)page.Index * 20, (int)(page.DesiredSize ?? 20)).ToImmutableList();

		var sut = new DynamicFeed<IImmutableList<int>>(Load).AsListFeed();
		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);

		var result = sut.Record(ctx);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(0, 20)))
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.HasMore))
		);

		requests.RequestMoreItems(42);

		await result.Should().BeAsync(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(0, 20)))
				.Current(Items.Range(20), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(20, Range(20, 42)))
				.Current(Items.Range(62), Error.No, Progress.Final, Pagination.HasMore))
		);
	}

	[TestMethod]
	public async Task When_PaginatedWithFeedParameterAsListFeed_And_PageRequested_Then_Reload_And_ItemsAddedWithCollectionChanges()
	{
		var myFeed = State<uint>.Value(this, () => 42);
		async ValueTask<IImmutableList<int>?> Load(CancellationToken ct)
		{
			var myValue = await myFeed;
			var items = await FeedExecution.Current!.GetPaginated<int>(b => b.ByIndex(myValue).GetPage(LoadPage));

			return items;
		}

		async ValueTask<IImmutableList<int>> LoadPage(PageRequest page, CancellationToken ct)
			=> Range((int)page.Index * 20, (int)(page.DesiredSize ?? 20)).ToImmutableList();

		var sut = new DynamicFeed<IImmutableList<int>>(Load).AsListFeed();
		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);

		var result = sut.Record(ctx);

		requests.RequestMoreItems(42);
		await myFeed.Set(43, CT);
		requests.RequestMoreItems(42);

		await result.Should().BeAsync(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(42 * 20, 20)))
				.Current(Items.Some(Range(42 * 20, 20)), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(at: 20, Range(42 * 20 + 20, 42)))
				.Current(Items.Some(Range(42 * 20, 20 + 42)), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data, Items.Remove(at: 0, Range(42 * 20, 20)) & Items.Remove(at: 20, Range(43 * 20 + 20, 22)))
				.Current(Items.Some(Range(43 * 20, 20)), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(at: 20, Range(43 * 20 + 20, 42)))
				.Current(Items.Some(Range(43 * 20, 20 + 42)), Error.No, Progress.Final, Pagination.HasMore))
		);
	}


	[TestMethod]
	public async Task When_PaginatedWithFeedParameterAsListFeed_And_PageIsAsync_And_PageRequest_Then_Reload_And_ItemsAddedWithCollectionChanges()
	{
		var myFeed = State<uint>.Value(this, () => 42);
		var pageReady = new TaskCompletionSource();
		async ValueTask<IImmutableList<int>?> Load(CancellationToken ct)
		{
			var myValue = await myFeed;
			var items = await FeedExecution.Current!.GetPaginated<int>(b => b.ByIndex(myValue).GetPage(LoadPage));

			return items;
		}

		async ValueTask<IImmutableList<int>> LoadPage(PageRequest page, CancellationToken ct)
		{
			await pageReady.Task;
			return Range((int)page.Index * 20, (int)(page.DesiredSize ?? 20)).ToImmutableList();
		}

		var sut = new DynamicFeed<IImmutableList<int>>(Load).AsListFeed();
		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);

		var result = sut.Record(ctx);

		await result.WaitForMessages(1);
		Interlocked.Exchange(ref pageReady, new ()).SetResult();

		await result.WaitForMessages(2);
		requests.RequestMoreItems(42);

		await result.WaitForMessages(3);
		Interlocked.Exchange(ref pageReady, new()).SetResult();

		await result.WaitForMessages(4);
		await myFeed.Set(43, CT);

		await result.WaitForMessages(5);
		Interlocked.Exchange(ref pageReady, new()).SetResult();

		await result.WaitForMessages(6);
		requests.RequestMoreItems(42);

		await result.WaitForMessages(7);
		Interlocked.Exchange(ref pageReady, new()).SetResult();

		await result.WaitForMessages(8);

		await result.Should().BeAsync(b => b
			// Initial load
			.Message(m => m
				.Changed(Changed.Progress)
				.Current(Data.Undefined, Error.No, Progress.Transient))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination & Changed.Progress, Items.Reset(Range(42 * 20, 20)))
				.Current(Items.Some(Range(42 * 20, 20)), Error.No, Progress.Final, Pagination.HasMore))

			// Load more (42)
			.Message(m => m
				.Changed(Changed.Pagination)
				.Current(Items.Some(Range(42 * 20, 20)), Error.No, Progress.Final, Pagination.Loading))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(at: 20, Range(42 * 20 + 20, 42)))
				.Current(Items.Some(Range(42 * 20, 20 + 42)), Error.No, Progress.Final, Pagination.HasMore))

			// Parameter change (43)
			.Message(m => m
				.Changed(Changed.Progress)
				.Current(Items.Some(Range(42 * 20, 20 + 42)), Error.No, Progress.Transient, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Progress, Items.Remove(at: 0, Range(42 * 20, 20)) & Items.Remove(at: 20, Range(43 * 20 + 20, 22)))
				.Current(Items.Some(Range(43 * 20, 20)), Error.No, Progress.Final, Pagination.HasMore))

			// Load more (42)
			.Message(m => m
				.Changed(Changed.Pagination)
				.Current(Items.Some(Range(43 * 20, 20)), Error.No, Progress.Final, Pagination.Loading))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(at: 20, Range(43 * 20 + 20, 42)))
				.Current(Items.Some(Range(43 * 20, 20 + 42)), Error.No, Progress.Final, Pagination.HasMore))
		);
	}

	[TestMethod]
	[Ignore]
	public async Task When_PaginatedWithFeedParameterAsListFeed_And_PageIsAsync_And_PageRequest_And_RefreshRequested_Then_Reload_And_ItemsAddedWithCollectionChanges()
	{
		var myFeed = State<uint>.Value(this, () => 42);
		var pageReady = new TaskCompletionSource();
		async ValueTask<IImmutableList<int>?> Load(CancellationToken ct)
		{
			FeedExecution.Current!.EnableRefresh();

			var myValue = await myFeed;
			var items = await FeedExecution.Current!.GetPaginated<int>(b => b.ByIndex(myValue).GetPage(LoadPage));

			return items;
		}

		async ValueTask<IImmutableList<int>> LoadPage(PageRequest page, CancellationToken ct)
		{
			await pageReady.Task;
			return Range((int)page.Index * 20, (int)(page.DesiredSize ?? 20)).ToImmutableList();
		}

		var sut = new DynamicFeed<IImmutableList<int>>(Load).AsListFeed();
		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);

		var result = sut.Record(ctx);

		await result.WaitForMessages(1);
		Interlocked.Exchange(ref pageReady, new()).SetResult();

		await result.WaitForMessages(2);
		requests.RequestMoreItems(42);

		await result.WaitForMessages(3);
		Interlocked.Exchange(ref pageReady, new()).SetResult();

		await result.WaitForMessages(4);
		await myFeed.Set(43, CT);

		await result.WaitForMessages(5);
		Interlocked.Exchange(ref pageReady, new()).SetResult();

		await result.WaitForMessages(6);
		requests.RequestMoreItems(42);

		await result.WaitForMessages(7);
		Interlocked.Exchange(ref pageReady, new()).SetResult();

		await result.WaitForMessages(8);
		requests.RequestRefresh();

		await result.WaitForMessages(9);
		Interlocked.Exchange(ref pageReady, new()).SetResult();

		await result.WaitForMessages(10);
		requests.RequestMoreItems(42);

		await result.WaitForMessages(11);
		Interlocked.Exchange(ref pageReady, new()).SetResult();

		await result.WaitForMessages(12);

		await result.Should().BeAsync(b => b
			// Initial load
			.Message(m => m
				.Changed(Changed.Progress)
				.Current(Data.Undefined, Error.No, Progress.Transient))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination & Changed.Progress, Items.Reset(Range(42 * 20, 20)))
				.Current(Items.Some(Range(42 * 20, 20)), Error.No, Progress.Final, Pagination.HasMore))

			// Load more (42)
			.Message(m => m
				.Changed(Changed.Pagination)
				.Current(Items.Some(Range(42 * 20, 20)), Error.No, Progress.Final, Pagination.Loading))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(at: 20, Range(42 * 20 + 20, 42)))
				.Current(Items.Some(Range(42 * 20, 20 + 42)), Error.No, Progress.Final, Pagination.HasMore))

			// Parameter change (43)
			.Message(m => m
				.Changed(Changed.Progress)
				.Current(Items.Some(Range(42 * 20, 20 + 42)), Error.No, Progress.Transient, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Progress, Items.Remove(at: 0, Range(42 * 20, 20)) & Items.Remove(at: 20, Range(43 * 20 + 20, 22)))
				.Current(Items.Some(Range(43 * 20, 20)), Error.No, Progress.Final, Pagination.HasMore))

			// Load more (42)
			.Message(m => m
				.Changed(Changed.Pagination)
				.Current(Items.Some(Range(43 * 20, 20)), Error.No, Progress.Final, Pagination.Loading))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(at: 20, Range(43 * 20 + 20, 42)))
				.Current(Items.Some(Range(43 * 20, 20 + 42)), Error.No, Progress.Final, Pagination.HasMore))

			// Refresh
			.Message(m => m
				.Changed(Changed.Progress)
				.Current(Items.Some(Range(43 * 20, 20 + 42)), Error.No, Progress.Transient, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Progress, Items.Remove(at: 20, Range(43 * 20 + 20, 42)))
				.Current(Items.Some(Range(43 * 20, 20)), Error.No, Progress.Final, Pagination.HasMore))

			// Load more (42)
			.Message(m => m
				.Changed(Changed.Pagination)
				.Current(Items.Some(Range(43 * 20, 20)), Error.No, Progress.Final, Pagination.Loading))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(at: 20, Range(43 * 20 + 20, 42)))
				.Current(Items.Some(Range(43 * 20, 20 + 42)), Error.No, Progress.Final, Pagination.HasMore))
		);
	}

	//[TestMethod]
	//public async Task When_Paginated_Then_ReloadWhenPageRequested()
	//{
	//	async ValueTask<MyEntity> Load(CancellationToken ct)
	//	{
	//		var items = await FeedAsyncExecution.Current!.GetPaginated<int>(b => b.ByIndex().GetPage(LoadPage));

	//		return new MyEntity(items, 100);
	//	}

	//	async ValueTask<IImmutableList<int>> LoadPage(PageRequest page, CancellationToken ct)
	//		=> Range((int)page.Index * 20, (int)(page.DesiredSize ?? 20)).ToImmutableList();

	//	var sut = new DynamicFeed<MyEntity>(Load);
	//	var requests = new RequestSource();
	//	var ctx = Context.SourceContext.CreateChild(requests);

	//	var result = sut.Record(ctx);

	//	result.Should().Be(b => b
	//		.Message(m => m
	//			.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(0, 20)))
	//			.Current(Items.Range(20), Error.No, Progress.Final, Pagination.HasMore))
	//	);

	//	requests.RequestMoreItems(42);
	//	await result.WaitForMessages(2, CT);

	//	//result.Should().Be(b => b
	//	//	.Message(m => m
	//	//		.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(0, 20)))
	//	//		.Current(Items.Range(20), Error.No, Progress.Final, Pagination.HasMore))
	//	//	.Message(m => m
	//	//		.Changed(Changed.Data & Changed.Pagination, Items.Add(20, Range(20, 42)))
	//	//		.Current(Items.Range(62), Error.No, Progress.Final, Pagination.HasMore))
	//	//);

	//	Console.WriteLine(result.ToString());
	//}

	//private record MyEntity(IImmutableList<int> Items, uint TotalItems);

	[TestMethod]
	public async Task When_NoDependency_Then_Complete()
	{
		var sut = new DynamicFeed<int>(async _ => 42).Record();

		await sut.WaitForEnd();
	}

	[TestMethod]
	public async Task When_RemoveLastDependency_Then_Complete()
	{
		FeedSession? session = default;
		var dependency = new TestDependency();
		var sut = new DynamicFeed<int>(async _ =>
		{
			session = FeedExecution.Current!.Session;
			session.RegisterDependency(dependency);

			return 42;
		}).Record();

		session!.Should().NotBeNull();
		try
		{
			await sut.WaitForEnd(100);
		}
		catch (TimeoutException)
		{
		}

		session!.UnRegisterDependency(dependency);

		await sut.WaitForEnd();
	}

	private class TestDependency : IDependency
	{
		/// <inheritdoc />
		public async ValueTask OnExecuting(FeedExecution execution, CancellationToken ct)
		{
		}

		/// <inheritdoc />
		public async ValueTask OnExecuted(FeedExecution execution, FeedExecutionResult result, CancellationToken ct)
		{
		}
	}
}
