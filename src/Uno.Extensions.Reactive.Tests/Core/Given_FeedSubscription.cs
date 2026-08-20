using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Core;

[TestClass]
public class Given_FeedSubscription : FeedTests
{
	[TestMethod]
	public async Task When_SubscribeTwice_Then_SourceSubscribedOnlyOnce()
	{
		var subscribe = 0;
		var src = Feed.Async(async ct => ++subscribe);
		var sut = new FeedSubscription<int>(src, Context.SourceContext);

		await sut.GetMessages(Context.SourceContext, CT).GetAsyncEnumerator(CT).MoveNextAsync();
		await sut.GetMessages(Context.SourceContext, CT).GetAsyncEnumerator(CT).MoveNextAsync();

		subscribe.Should().Be(1);
	}

	[TestMethod]
	public async Task When_SubscribeTwice_Then_Replay()
	{
		// Both halves of this test are about *which* value a subscriber sees, so the source must be
		// held at a known position rather than merely nudged with a delay. The original version used
		// `await Task.Yield()` to give the first subscriber a chance to observe 1 and `Task.Delay(10)`
		// to let 2 and 3 flow; a loaded CI agent lost the first race regularly, and the subscriber then
		// read the latest value instead ("expected 1, but found 3").
		var firstValueObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var allValuesProduced = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		async IAsyncEnumerable<int> Source([EnumeratorCancellation] CancellationToken ct = default)
		{
			yield return 1;

			await firstValueObserved.Task; // Not a delay: 2 cannot exist until 1 has been read.

			yield return 2;
			yield return 3;

			// Reached only once the consumer has pulled past 3, so 3 is already published by then.
			allValuesProduced.SetResult();
		}
		var src = Feed<int>.AsyncEnumerable(Source);
		var sut = new FeedSubscription<int>(src, Context.SourceContext);

		var sub1Message = await sut.GetMessages(Context.SourceContext, CT).FirstAsync(CT);

		firstValueObserved.SetResult();
		await allValuesProduced.Task;

		var sub2Message = await sut.GetMessages(Context.SourceContext, CT).FirstAsync(CT);

		sub1Message.Current.Data.SomeOrDefault().Should().Be(1, "the source was held until the first value had been read");
		sub2Message.Current.Data.SomeOrDefault().Should().Be(3, "the subscription should have stay active");
	}

	[TestMethod]
	public async Task When_SubscribeOnLiveFeed_The_FirstMessageIsRebasedOnInitial()
	{
		var myAxis = new MessageAxis<object>("testAxis", _ => new object());
		var myAxisValue = new object();
		async IAsyncEnumerable<Message<int>> Source([EnumeratorCancellation] CancellationToken ct = default)
		{
			var msg = Message<int>.Initial;
			yield return msg = msg.With().Data(1).Set(myAxis, myAxisValue);
			yield return msg = msg.With().Data(2);
			yield return msg = msg.With().Data(3);
		}
		var src = Feed<int>.Create(Source);
		var sut = new FeedSubscription<int>(src, Context.SourceContext);

		_ = await sut.GetMessages(Context.SourceContext, CT).FirstAsync(CT);
		var sub2Message = await sut.GetMessages(Context.SourceContext, CT).FirstAsync(CT);

		sub2Message.Should().Be(m => m
			.Previous(Data.Undefined, Error.No, Progress.Final, Axis.NotSet(myAxis)) // initial
			.Changed(Changed.Data & myAxis)
			.Current(3, Error.No, Progress.Final, Axis.Set(myAxis, myAxisValue)));
	}

	[TestMethod]
	public async Task When_Subscribe_Then_FlowSubContext()
	{
		Context.ResignCurrent();

		var ctx = new FeedTestContext();
		var src = Feed.Async(async ct =>
		{
			Assert.AreNotSame(ctx.SourceContext, SourceContext.Current, "We should have a sub context to allow request flowing");
			Assert.AreEqual(ctx.SourceContext.RootId, SourceContext.Current.RootId, "but the sub context should be based on the subscription context");

			return 42;
		});
		var sut = new FeedSubscription<int>(src, ctx);

		var result = await sut.GetMessages(ctx, CT).FirstAsync(CT);

		result.Should().Be(Error.No);
	}

	[TestMethod]
	public async Task When_Subscribe_Then_FlowRequestSubscriptionContext()
	{
		Context.ResignCurrent();

		var myRequest = new MyTestRequest();
		var receivedRequest = new List<MyTestRequest>();
		var requestSource = new RequestSource();
		var rootCtx = new FeedTestContext().SourceContext;
		var ctx = rootCtx.CreateChild(requestSource);
		var src = Feed.Async(async ct =>
		{
			SourceContext.Current.Requests<MyTestRequest>(receivedRequest.Add, CT);
			return 42;
		});
		var sut = new FeedSubscription<int>(src, rootCtx);
		_ = await sut.GetMessages(ctx, CT).FirstAsync(CT);

		requestSource.Send(myRequest);

		receivedRequest.Should().BeEquivalentTo(new[] { myRequest });
	}

	[TestMethod]
	public async Task When_Subscribe_Then_FlowRequestFromSubsequentSubscriptionContext()
	{
		Context.ResignCurrent();

		var myRequest = new MyTestRequest();
		var receivedRequest = new List<MyTestRequest>();
		var requestSource = new RequestSource();
		var rootCtx = new FeedTestContext().SourceContext;
		var ctx = rootCtx.CreateChild(requestSource);
		var src = Feed.Async(async ct =>
		{
			SourceContext.Current.Requests<MyTestRequest>(receivedRequest.Add, CT);
			return 42;
		});
		var sut = new FeedSubscription<int>(src, rootCtx);
		_ = await sut.GetMessages(rootCtx.CreateChild(new RequestSource()), CT).FirstAsync(CT);
		_ = await sut.GetMessages(ctx, CT).FirstAsync(CT);

		requestSource.Send(myRequest);

		receivedRequest.Should().BeEquivalentTo(new[] { myRequest });
	}

	private record MyTestRequest : IContextRequest;
}
