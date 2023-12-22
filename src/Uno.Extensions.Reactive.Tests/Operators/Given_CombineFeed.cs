using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

[TestClass]
public class Given_CombineFeed : FeedTests
{
	[TestMethod]
	public async Task When_Combine2()
	{
		var feed1 = new StateImpl<int>(Context, Option<int>.Undefined());
		var feed2 = new StateImpl<int>(Context, Option<int>.Undefined());

		var sut = Feed.Combine(feed1, feed2).Record();

		await feed1.UpdateMessageAsync(msg => msg.Data(42), CT);
		await feed2.UpdateMessageAsync(msg => msg.Data(43), CT);

		sut.Should().Be(r => r
			.Message(Changed.None, Data.Undefined, Error.No, Progress.Final)
			.Message(Changed.Data, (42, 43), Error.No, Progress.Final)
		);
	}
}
