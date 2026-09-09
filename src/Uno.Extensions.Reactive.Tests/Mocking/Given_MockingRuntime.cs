using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.HotTesting.Reactive;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Mocking;

/// <summary>
/// Spec 013 step A — the mocking runtime (MockingService scope, MockFeed/MockListFeed vocabulary,
/// MockModel swap engine).
/// </summary>
[TestClass]
public class Given_MockingRuntime : FeedTests
{
	[TestMethod]
	public async Task When_MockFeed_Value_Then_EmitsValue()
	{
		var (result, _) = FeedMock.Value(42).Record();
		await result.WaitForMessages(1);
		result.Last().Current.Data.SomeOrDefault().Should().Be(42);
	}

	[TestMethod]
	public async Task When_MockListFeed_Value_Then_EmitsItems()
	{
		var (result, _) = ListFeedMock.Value(1, 2, 3).Record();
		await result.WaitForMessages(1);
		((IImmutableList<int>)result.Last().Current.Data.SomeOrDefault()!).Should().BeEquivalentTo(new[] { 1, 2, 3 });
	}

	[TestMethod]
	public async Task When_MockableFeedSwapped_ViaEngine_Then_ReEmits()
	{
		FeedTestContext ctxHolder;
		using (MockingService.Enable())
		{
			ctxHolder = new FeedTestContext();
		}

		using (ctxHolder)
		{
			ctxHolder.RestoreCurrent();

			var original = FeedMock.Value("original");
			var state = (StateImpl<string>)ctxHolder.SourceContext.GetOrCreateState(original);
			var (result, _) = state.Record();

			await result.WaitForMessages(1);
			result.Last().Current.Data.SomeOrDefault().Should().Be("original");

			MockingService.SwapFeed(ctxHolder, original, FeedMock.Value("mocked"));

			await result.WaitForMessages(2);
			result.Last().Current.Data.SomeOrDefault().Should().Be("mocked");
		}
	}

	[TestMethod]
	public void When_SwapFeed_OnNonMockableContext_Then_FailsHard()
	{
		using var ctx = new FeedTestContext();
		ctx.SourceContext.IsMockingActive.Should().BeFalse();

		var original = FeedMock.Value("x");
		_ = ctx.SourceContext.GetOrCreateState(original);

		var act = () => MockingService.SwapFeed(ctx, original, FeedMock.Value("y"));

		act.Should().Throw<InvalidOperationException>("fail-hard: a non-mockable feed cannot be swapped (D11)");
	}
}
