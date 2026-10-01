using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Testing;
using Uno.HotTesting.Reactive;

namespace Uno.Extensions.Reactive.Tests.Core;

/// <summary>
/// Spec 013 — substrate canaries for the per-context mocking gate (D12) and subscription swap (D11).
/// </summary>
[TestClass]
public class Given_MockingActivation : FeedTests
{

	[TestMethod]
	public void When_NoScope_Then_ContextNotMockable()
	{
		using var ctx = new FeedTestContext();

		ctx.SourceContext.IsMockingActive.Should().BeFalse("no MockingService.Enable() scope was opened");
	}

	[TestMethod]
	public void When_NoScope_Then_SubscriptionsCannotBeSwapped()
	{
		FeedTestContext mocking;
		using (MockingService.Enable())
		{
			mocking = new FeedTestContext();
		}
		using var live = new FeedTestContext();
		var input = Feed.Async(async ct => 1);

		mocking.SourceContext.States.GetOrCreateSubscription(input).CanHotSwap.Should().BeTrue("a mocking context can swap the source of a feed");
		live.SourceContext.States.GetOrCreateSubscription(input).CanHotSwap.Should().BeFalse("a live context never wraps a subscription (G9/R7)");

		mocking.Dispose();
	}

	[TestMethod]
	public async Task When_MockableInputSwapped_Then_DynamicFeedRecomputes()
	{
		var owner = new object();
		SourceContext ctx;
		using (MockingService.Enable())
		{
			ctx = SourceContext.GetOrCreate(owner);
		}
		using var scope = ctx.AsCurrent();
		var items = ListFeed<int>.Async(async ct => (IImmutableList<int>)ImmutableList.Create(1, 2, 3));

		var (count, _) = ctx.GetOrCreateState(Feed.Dynamic(async ct => (await items).Count)).Record();
		await count.WaitForData(3);

		MockingService.SwapListFeed(owner, items, ListFeedMock.Value(5, 6, 7, 8));

		await count.WaitForData(4);
	}

	[TestMethod]
	public void When_UnderScope_Then_ContextSubscriptionIsSwappable()
	{
		FeedTestContext ctx;
		using (MockingService.Enable())
		{
			ctx = new FeedTestContext();
		}

		using (ctx)
		{
			ctx.SourceContext.IsMockingActive.Should().BeTrue("the context was created inside an EnableMocking() scope");

			var input = Feed.Async(async ct => "v");
			ctx.SourceContext.States.GetOrCreateSubscription(input).CanHotSwap
				.Should().BeTrue("the shared subscription is the single source-replacement point");
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
	public async Task When_MockableFeedSwapped_Then_ItsStateReEmits()
	{
		var owner = new object();
		SourceContext ctx;
		using (MockingService.Enable())
		{
			ctx = SourceContext.GetOrCreate(owner);
		}

		using var scope = ctx.AsCurrent();
		var original = Feed.Async(async ct => "original");
		var state = (StateImpl<string>)ctx.GetOrCreateState(original);
		var (result, _) = state.Record();

		await result.WaitForData("original");
		MockingService.SwapFeed(owner, original, Feed.Async(async ct => "mocked"));
		await result.WaitForData("mocked");
	}

	[TestMethod]
	public async Task When_MockableStateReplaced_Then_WritesTargetReplacementState()
	{
		var owner = new object();
		SourceContext ctx;
		using (MockingService.Enable())
		{
			ctx = SourceContext.GetOrCreate(owner);
		}

		using var scope = ctx.AsCurrent();
		var current = (StateImpl<string>)ctx.CreateState(Option.Some("current"));
		var replacement = (StateImpl<string>)ctx.CreateState(Option.Some("replacement"));
		await using var currentReader = ctx.GetOrCreateSource(current).GetAsyncEnumerator(CT);
		await using var replacementReader = replacement.GetSource(ctx, CT).GetAsyncEnumerator(CT);

		(await currentReader.MoveNextAsync()).Should().BeTrue();
		currentReader.Current.Current.Data.SomeOrDefault().Should().Be("current");
		(await replacementReader.MoveNextAsync()).Should().BeTrue();
		replacementReader.Current.Current.Data.SomeOrDefault().Should().Be("replacement");

		MockingService.SwapFeed(owner, current, replacement);
		(await currentReader.MoveNextAsync()).Should().BeTrue();
		currentReader.Current.Current.Data.SomeOrDefault().Should().Be("replacement");

		await current.UpdateMessageAsync(message => message.Data("edited"), CT);
		(await replacementReader.MoveNextAsync()).Should().BeTrue();
		replacementReader.Current.Current.Data.SomeOrDefault().Should().Be("edited");
	}

	[TestMethod]
	public async Task When_StateReplacedRepeatedly_Then_OldestStateWritesToLatest()
	{
		using var context = new FeedTestContext();
		context.RestoreCurrent();
		var first = (StateImpl<string>)context.SourceContext.CreateState(Option.Some("first"));
		var second = (StateImpl<string>)context.SourceContext.CreateState(Option.Some("second"));
		var latest = (StateImpl<string>)context.SourceContext.CreateState(Option.Some("latest"));
		await using var latestReader = latest.GetSource(context.SourceContext, CT).GetAsyncEnumerator(CT);

		(await latestReader.MoveNextAsync()).Should().BeTrue();
		first.TransferUpdatesTo(second);
		second.TransferUpdatesTo(latest);

		await first.UpdateMessageAsync(message => message.Data("edited"), CT);
		(await latestReader.MoveNextAsync()).Should().BeTrue();
		latestReader.Current.Current.Data.SomeOrDefault().Should().Be("edited");
	}

}
