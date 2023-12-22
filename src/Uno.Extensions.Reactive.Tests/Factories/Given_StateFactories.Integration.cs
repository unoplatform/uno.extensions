using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Factories;

partial class Given_StateFactories
{
	[TestMethod]
	public async Task When_CreateStateFromFeed_Then_UpdatingDoesNotImpactFeed()
	{
		var feed = Feed.Async(async _ => 42);
		var state = State.FromFeed(this, feed);

		var feedResult = feed.Record();
		var stateResult = state.Record();

		await state.SetAsync(43, CT);

		// We assert the state first to wait for the second message
		await stateResult.Should().BeAsync(r => r
			.Message(42, Error.No, Progress.Final)
			.Message(43, Error.No, Progress.Final));

		try
		{
			await feedResult.WaitForMessages(2, 100);
		}
		catch (TimeoutException) { }

		feedResult.Should().Be(r => r
			.Message(42, Error.No, Progress.Final));
	}
}
