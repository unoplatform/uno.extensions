using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

[TestClass]
public partial class Given_WhereListFeed : FeedTests
{
	[TestMethod]
	public async Task When_AllFilteredOut_Then_None()
	{
		var source = ListState<int>.Async(this, async ct => ImmutableList.Create(42) as IImmutableList<int>);
		var sut = source.Where(i => i is not 42);
		var result = sut.Record();

		await result.WaitForMessages(1);

		result.Should().Be(r => r
			.Message(Changed.Data, Data.None, Error.No, Progress.Final)
		);
	}
}
