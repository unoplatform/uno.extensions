using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
	private static async Task<IImmutableList<T>?> CurrentItems<T>(SourceContext ctx, IListFeed<T> feed)
	{
		var (result, _) = ctx.GetOrCreateListState(feed).Record();
		for (var i = 0; i < 50; i++)
		{
			if (result.Count > 0 && result.Last().Current.Data.IsSome(out var v))
			{
				return (IImmutableList<T>)v!;
			}
			await Task.Delay(20);
		}
		return result.Count > 0 && result.Last().Current.Data.IsSome(out var last) ? (IImmutableList<T>)last! : null;
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

	[TestMethod]
	public async Task When_ModelHasSeveralCtorParametersAndOverloads_Then_CreateCompilesAndInputFlows()
	{
		// MenuModel takes a service and a navigator, and offers a second constructor of the same arity. This file
		// compiling is the proof that Create passes a typed default for each parameter (a bare `default!` leaves
		// the call ambiguous, CS0121); the assertion checks that the mocked input flows without any dependency.
		var vm = MenuViewModelMock.Create(new MenuModelMock { Items = ListFeedMock.Value("a", "b") });
		using var _ = SourceContext.GetOrCreate(vm.Model).AsCurrent();

		var items = await CurrentItems(SourceContext.GetOrCreate(vm.Model), vm.Model.Items);
		items.Should().BeEquivalentTo(new[] { "a", "b" });
	}

	[TestMethod]
	public async Task When_InputMocked_Then_DerivedFeedComputesOverTheMock()
	{
		var vm = MenuViewModelMock.Create(new MenuModelMock { Items = ListFeedMock.Value("a", "b") });
		var ctx = SourceContext.GetOrCreate(vm.Model);
		using var scope = ctx.AsCurrent();

		var (count, _) = ctx.GetOrCreateState(vm.Model.ItemsCount).Record();

		await count.WaitForData(2); // ItemsCount is left unset: the real Select runs over the mocked Items
		count.Should().NotContain(message => message.Current.Error != null, "the real, null-injected Items never runs");
	}

	[TestMethod]
	public async Task When_InputReSwapped_Then_DerivedFeedRecomputes()
	{
		var vm = MenuViewModelMock.Create(new MenuModelMock { Items = ListFeedMock.Value("a", "b") });
		var ctx = SourceContext.GetOrCreate(vm.Model);
		using var scope = ctx.AsCurrent();

		var (count, _) = ctx.GetOrCreateState(vm.Model.ItemsCount).Record();
		await count.WaitForData(2);

		vm.SetMock(MenuModelMock.Empty with { Items = ListFeedMock.Value("a", "b", "c") });

		await count.WaitForData(3);
	}

	[TestMethod]
	public async Task When_DerivedMemberMocked_Then_TheOverrideWins()
	{
		var vm = MenuViewModelMock.Create(new MenuModelMock { Items = ListFeedMock.Value("a", "b"), ItemsCount = FeedMock.Value(42) });
		var ctx = SourceContext.GetOrCreate(vm.Model);
		using var scope = ctx.AsCurrent();

		var (count, _) = ctx.GetOrCreateState(vm.Model.ItemsCount).Record();

		await count.WaitForData(42);
	}

	[TestMethod]
	public async Task When_ListInputMocked_Then_ListWhereComputesOverTheMock()
	{
		var vm = MenuViewModelMock.Create(new MenuModelMock { Items = ListFeedMock.Value("a", "bb", "ccc") });
		var ctx = SourceContext.GetOrCreate(vm.Model);
		using var scope = ctx.AsCurrent();

		var (longItems, _) = ctx.GetOrCreateListState(vm.Model.LongItems).Record();

		await longItems.WaitForData(items => items.SequenceEqual(new[] { "bb", "ccc" }));
	}

	[TestMethod]
	public async Task When_ScalarInputMocked_Then_ChainedDerivationsComputeOverTheMock()
	{
		var vm = PriceViewModelMock.Create(new PriceModelMock { Price = FeedMock.Value(10m) });
		var ctx = SourceContext.GetOrCreate(vm.Model);
		using var scope = ctx.AsCurrent();

		var (rounded, _) = ctx.GetOrCreateState(vm.Model.Rounded).Record();

		await rounded.WaitForData(9m); // two Select levels over the mocked Price
		rounded.Should().NotContain(message => message.Current.Error != null, "the real, null-injected Price never runs");
	}

	[TestMethod]
	public async Task When_ScalarInputReSwapped_Then_ChainedDerivationsRecompute()
	{
		var vm = PriceViewModelMock.Create(new PriceModelMock { Price = FeedMock.Value(10m) });
		var ctx = SourceContext.GetOrCreate(vm.Model);
		using var scope = ctx.AsCurrent();

		var (rounded, _) = ctx.GetOrCreateState(vm.Model.Rounded).Record();
		await rounded.WaitForData(9m);

		vm.SetMock(PriceModelMock.Empty with { Price = FeedMock.Value(20m) });

		await rounded.WaitForData(18m);
	}

	[TestMethod]
	public async Task When_SetMockPassesTheOriginalFeed_Then_TheRealInputRuns()
	{
		var vm = MenuViewModelMock.Create(new MenuModelMock { Items = ListFeedMock.Value("a", "b") });
		var ctx = SourceContext.GetOrCreate(vm.Model);
		using var scope = ctx.AsCurrent();

		var (count, _) = ctx.GetOrCreateState(vm.Model.ItemsCount).Record();
		await count.WaitForData(2);

		var before = count.Count;
		vm.SetMock(MenuModelMock.Empty with { Items = vm.Model.Items });

		await count.WaitForMessages(before + 1);
		count.Last().Current.Error.Should().BeOfType<NullReferenceException>("the original Items runs again, over its null-injected service");
	}

	[TestMethod]
	public async Task When_InputStateIsEdited_Then_DerivedFeedIgnoresTheEdit()
	{
		var vm = MenuViewModelMock.Create(new MenuModelMock { Items = ListFeedMock.Value("a", "b") });
		var ctx = SourceContext.GetOrCreate(vm.Model);
		using var scope = ctx.AsCurrent();

		var (count, _) = ctx.GetOrCreateState(vm.Model.ItemsCount).Record();
		await count.WaitForData(2);

		await ctx.GetOrCreateState(vm.Model.Items.AsFeed()).UpdateAsync(_ => ImmutableList.Create("edited"));
		vm.SetMock(MenuModelMock.Empty with { Items = ListFeedMock.Value("a", "b", "c") });
		await count.WaitForData(3);

		count.Should().NotContain(
			message => message.Current.Data.SomeOrDefault() == 1,
			"an edit of the state stays local to it, as in a live app, while the swap reaches the derived feed");
	}

	[TestMethod]
	public void When_ModelHasDerivedAndIndependentMembers_Then_MockExposesOnlyTheDerivedOneAsOptional()
	{
		// MenuModel also declares a derived feed (ItemsCount) and an independent state (Filter): the record requires
		// the input, offers the derived feed as an optional override and leaves the independent state out.
		var mock = typeof(MenuModelMock);

		IsRequired(mock.GetProperty(nameof(MenuModel.Items))).Should().BeTrue();
		IsRequired(mock.GetProperty(nameof(MenuModel.ItemsCount))).Should().BeFalse();
		IsRequired(mock.GetProperty(nameof(MenuModel.LongItems))).Should().BeFalse();
		mock.GetProperty(nameof(MenuModel.Filter)).Should().BeNull();
	}

	[TestMethod]
	public void When_ModelIsAPositionalRecord_Then_ItsServiceDependentFeedIsRequired()
	{
		// The service reaches the feed through the record's synthesized property, not through a
		// constructor-body assignment: the classifier must still see it as a service dependency.
		IsRequired(typeof(BasketModelMock).GetProperty(nameof(BasketModel.Lines))).Should().BeTrue();
	}

	private static bool IsRequired(PropertyInfo? property)
	{
		property.Should().NotBeNull();
		return property!.IsDefined(typeof(RequiredMemberAttribute), inherit: false);
	}
}
