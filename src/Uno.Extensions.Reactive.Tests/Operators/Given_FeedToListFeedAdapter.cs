using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

[TestClass]
public partial class Given_FeedToListFeedAdapter : FeedTests
{
	[TestMethod]
	public async Task When_Null_Then_None()
	{
		var source = Feed.Async(async ct => default(IImmutableList<int>)!);
		var sut = source.AsListFeed();
		var result = sut.Record();

		await result.Should().BeAsync(r => r
			.Message(Changed.Data, Data.None, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Empty_Then_None()
	{
		var source = Feed.Async(async ct => ImmutableList<int>.Empty as IImmutableList<int>);
		var sut = source.AsListFeed();
		var result = sut.Record();

		await result.Should().BeAsync(r => r
			.Message(Changed.Data, Data.None, Error.No, Progress.Final)
		);
	}
}
