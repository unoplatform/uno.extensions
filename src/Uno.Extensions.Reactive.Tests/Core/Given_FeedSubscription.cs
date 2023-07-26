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
		async IAsyncEnumerable<int> Source([EnumeratorCancellation] CancellationToken ct = default)
		{
			yield return 1;

			await Task.Yield(); // Make sure to run async, so listener will receive 1 message.

			yield return 2;
			yield return 3;
		}
		var src = Feed<int>.AsyncEnumerable(Source);
		var sut = new FeedSubscription<int>(src, Context.SourceContext);

		var sub1Message = await sut.GetMessages(Context.SourceContext, CT).FirstAsync(CT);

		await Task.Delay(10); // Make sure to run async, so listener will receive next messages.

		var sub2Message = await sut.GetMessages(Context.SourceContext, CT).FirstAsync(CT);

		sub1Message.Current.Data.SomeOrDefault().Should().Be(1, "We added a delay before the second value");
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
