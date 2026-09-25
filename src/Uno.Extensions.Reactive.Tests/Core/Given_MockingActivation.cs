using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Testing;
using Uno.HotTesting.Reactive;

namespace Uno.Extensions.Reactive.Tests.Core;

/// <summary>
/// Spec 013 — substrate canaries for the per-context mocking gate (D12) and reflection swap (D11).
/// </summary>
[TestClass]
public class Given_MockingActivation : FeedTests
{
	private static HotSwapFeed<T>? GetHotSwap<T>(StateImpl<T> state)
		=> (HotSwapFeed<T>?)typeof(StateImpl<T>)
			.GetField("_hotSwap", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(state);

	[TestMethod]
	public void When_NoScope_Then_ContextNotMockable_And_NoWrap()
	{
		using var ctx = new FeedTestContext();

		ctx.SourceContext.IsMockingActive.Should().BeFalse("no MockingService.Enable() scope was opened");

		var state = new StateImpl<string>(ctx.SourceContext, Option<string>.Some("v"));
		GetHotSwap(state).Should().BeNull("a live-app context must never inject a HotSwapFeed indirection (G9/R7)");
	}

	[TestMethod]
	public void When_UnderScope_Then_ContextMockable_And_Wrapped()
	{
		FeedTestContext ctx;
		using (MockingService.Enable())
		{
			ctx = new FeedTestContext();
		}

		using (ctx)
		{
			ctx.SourceContext.IsMockingActive.Should().BeTrue("the context was created inside an EnableMocking() scope");

			var state = new StateImpl<string>(ctx.SourceContext, Option<string>.Some("v"));
			GetHotSwap(state).Should().NotBeNull("a mocking context wraps every state's source so it can be swapped");
		}
	}

	[TestMethod]
	public void When_ScopeDisposed_Then_AlreadyCreatedContextStaysMockable_ButNewOnesDont()
	{
		FeedTestContext inside;
		using (MockingService.Enable())
		{
			inside = new FeedTestContext();
		}
		using var outside = new FeedTestContext();

		inside.SourceContext.IsMockingActive.Should().BeTrue("contexts created inside a scope stay mockable for their own lifetime");
		outside.SourceContext.IsMockingActive.Should().BeFalse("after disposal, new contexts are no longer mockable");

		inside.Dispose();
	}

	[TestMethod]
	public async Task When_MockableStateSwapped_Then_ReEmits()
	{
		FeedTestContext ctxHolder;
		using (MockingService.Enable())
		{
			ctxHolder = new FeedTestContext();
		}

		using (ctxHolder)
		{
			ctxHolder.RestoreCurrent();

			var original = Feed.Async(async ct => "original");
			var state = (StateImpl<string>)ctxHolder.SourceContext.GetOrCreateState(original);

			// Sanity: a mocking context wraps the state source so it can be swapped.
			GetHotSwap(state).Should().NotBeNull();

			var (result, _) = state.Record();

			await result.WaitForMessages(1);
			result.Last().Current.Data.SomeOrDefault().Should().Be("original");

			// Reflection swap (D11): the state exposes IHotSwapState<T>, like hot reload.
			((IHotSwapState<string>)state).HotSwap(Feed.Async(async ct => "mocked"));

			await result.WaitForMessages(2);
			result.Last().Current.Data.SomeOrDefault().Should().Be("mocked",
				"the swapped source must re-emit through the same cached wrapper");
		}
	}

	[TestMethod]
	public async Task When_MockableInputSwapped_Then_SelectRecomputes()
	{
		using var test = CreateMockingContext();
		var input = Feed.Async(async ct => 1);
		var (selected, _) = test.SourceContext.GetOrCreateState(input.Select(value => value * 10)).Record();
		await selected.WaitForData(10);

		SwapSource(test, input, Feed.Async(async ct => 2));

		await selected.WaitForData(20);
	}

	[TestMethod]
	public async Task When_MockableInputSwapped_Then_DynamicRecomputes()
	{
		using var test = CreateMockingContext();
		var input = Feed.Async(async ct => 1);
		var (dynamic, _) = test.SourceContext.GetOrCreateState(Feed.Dynamic(async ct => await input * 100)).Record();
		await dynamic.WaitForData(100);

		SwapSource(test, input, Feed.Async(async ct => 2));

		await dynamic.WaitForData(200);
	}

	[TestMethod]
	public async Task When_MockableListInputSwapped_Then_ListWhereRecomputes()
	{
		using var test = CreateMockingContext();
		var items = ListFeed<int>.Async(async ct => (IImmutableList<int>)ImmutableList.Create(1, 2, 3));
		var (large, _) = test.SourceContext.GetOrCreateListState(items.Where(item => item >= 2)).Record();
		await large.WaitForData(list => list.Count == 2);

		SwapSource(test, items.AsFeed(), ListFeed<int>.Async(async ct => (IImmutableList<int>)ImmutableList.Create(5, 6, 7, 8)).AsFeed());

		await large.WaitForData(list => list.Count == 4);
	}

	[TestMethod]
	public async Task When_MockableListInputSwapped_Then_DynamicAwaitingTheListRecomputes()
	{
		using var test = CreateMockingContext();
		var items = ListFeed<int>.Async(async ct => (IImmutableList<int>)ImmutableList.Create(1, 2, 3));
		var (count, _) = test.SourceContext.GetOrCreateState(Feed.Dynamic(async ct => (await items).Count)).Record();
		await count.WaitForData(3);

		SwapSource(test, items.AsFeed(), ListFeed<int>.Async(async ct => (IImmutableList<int>)ImmutableList.Create(5, 6, 7, 8)).AsFeed());

		await count.WaitForData(4);
	}

	[TestMethod]
	public async Task When_MockableInputStateIsEdited_Then_DerivedFeedIgnoresTheEdit()
	{
		using var test = CreateMockingContext();
		var input = Feed.Async(async ct => 1);
		var inputState = test.SourceContext.GetOrCreateState(input);
		var (selected, _) = test.SourceContext.GetOrCreateState(input.Select(value => value * 10)).Record();
		await selected.WaitForData(10);

		await inputState.UpdateAsync(_ => 5);
		SwapSource(test, input, Feed.Async(async ct => 2));
		await selected.WaitForData(20);

		selected.Should().NotContain(
			message => message.Current.Data.SomeOrDefault() == 50,
			"an edit of the state stays local to it, as in a live app, while the swap reaches the derived feed");
	}

	[TestMethod]
	public async Task When_NoScope_Then_DerivedFeedCreatesNoSwapLayer()
	{
		using var test = new FeedTestContext();
		var input = Feed.Async(async ct => 1);
		var (selected, _) = test.SourceContext.GetOrCreateState(input.Select(value => value * 10)).Record();
		await selected.WaitForData(10);

		GetSwapLayers(test.SourceContext).Should().BeNull("a live-app context never observes a feed through a swap layer (G9/R7)");
	}

	private static FeedTestContext CreateMockingContext()
	{
		FeedTestContext context;
		using (MockingService.Enable())
		{
			context = new FeedTestContext();
		}

		context.RestoreCurrent();
		return context;
	}

	private static void SwapSource<T>(FeedTestContext test, IFeed<T> feed, IFeed<T> replacement)
		=> ((IHotSwapState<T>)test.SourceContext.GetOrCreateState(feed)).HotSwap(replacement);

	private static object? GetSwapLayers(SourceContext context)
		=> typeof(StateStore)
			.GetField("_swapLayers", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(context.States);
}
