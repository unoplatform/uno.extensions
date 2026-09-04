using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.HotTesting.Reactive;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive.Tests.MockingApp;

namespace Uno.Extensions.Reactive.Tests.Mocking;

/// <summary>
/// Spec 013 — end-to-end: the consumer generator's {Model}Mock / {Vm}Mock.Create / SetMock drive a real
/// VM (real Model, null-injected service) through mocked feed states. The activation scope lives inside
/// Create, so user code never opens it. Observation goes through the cached list-state (the same state
/// the bindable VM subscribes to), which the swap targets — not a fresh subscription to the raw feed.
/// </summary>
[TestClass]
public class Given_GeneratedMock : FeedUITests
{
	private static async Task<IImmutableList<int>?> CurrentItems(SourceContext ctx, IListFeed<int> feed)
	{
		var (result, _) = ctx.GetOrCreateListState(feed).Record();
		for (var i = 0; i < 50; i++)
		{
			if (result.Count > 0 && result.Last().Current.Data.IsSome(out var v))
			{
				return (IImmutableList<int>)v!;
			}
			await Task.Delay(20);
		}
		return result.Count > 0 && result.Last().Current.Data.IsSome(out var last) ? (IImmutableList<int>)last! : null;
	}

	[TestMethod]
	public async Task When_CreateWithMock_Then_FeedEmitsMockedValues()
	{
		// No MockingService.Enable() here — Create opens the scope internally.
		var vm = RecipeViewModelMock.Create(new RecipeModelMock { Steps = ListFeedMock.Value(1, 2, 3) });
		using var _ = SourceContext.GetOrCreate(vm.Model).AsCurrent();

		var items = await CurrentItems(SourceContext.GetOrCreate(vm.Model), vm.Model.Steps);
		items.Should().BeEquivalentTo(new[] { 1, 2, 3 });
	}

	[TestMethod]
	public async Task When_SetMockReSwaps_Then_ReEmitsLive()
	{
		var vm = RecipeViewModelMock.Create(new RecipeModelMock { Steps = ListFeedMock.Value(1) });
		using var _ = SourceContext.GetOrCreate(vm.Model).AsCurrent();

		(await CurrentItems(SourceContext.GetOrCreate(vm.Model), vm.Model.Steps))
			.Should().BeEquivalentTo(new[] { 1 });

		// Live re-swap — still works after Create's scope has closed (the context stays mockable).
		vm.SetMock(RecipeModelMock.Empty with { Steps = ListFeedMock.Value(7, 8) });

		(await CurrentItems(SourceContext.GetOrCreate(vm.Model), vm.Model.Steps))
			.Should().BeEquivalentTo(new[] { 7, 8 });
	}

	[TestMethod]
	public async Task When_CreateDefault_Then_InputsAreEmpty()
	{
		var vm = RecipeViewModelMock.Create(); // = RecipeModelMock.Empty → Steps = None
		using var _ = SourceContext.GetOrCreate(vm.Model).AsCurrent();

		var items = await CurrentItems(SourceContext.GetOrCreate(vm.Model), vm.Model.Steps);
		items.Should().BeNull("Empty pins the input to None");
	}

	[TestMethod]
	public async Task When_CatalogEntry_Then_PinnedState()
	{
		// Tier-3 sample: a named catalog entry (a partial of the generated RecipeViewModelMock).
		var vm = RecipeViewModelMock.Basic;
		using var _ = SourceContext.GetOrCreate(vm.Model).AsCurrent();

		var items = await CurrentItems(SourceContext.GetOrCreate(vm.Model), vm.Model.Steps);
		items.Should().BeEquivalentTo(new[] { 1, 2, 3 });
	}
}
