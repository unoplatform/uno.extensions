using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Testing;

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
		using (SourceContext.EnableMocking())
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
		using (SourceContext.EnableMocking())
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
		using (SourceContext.EnableMocking())
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
}
