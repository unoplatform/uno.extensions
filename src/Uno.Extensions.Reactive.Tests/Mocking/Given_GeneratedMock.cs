using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Mocking;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive.Tests.MockingApp;

namespace Uno.Extensions.Reactive.Tests.Mocking;

/// <summary>
/// Spec 013 step B — end-to-end: the consumer generator's {Model}Mock / Create / SetModel drive a real
/// VM (real Model, null-injected service) through mocked feed states via the reflection swap engine.
/// Observation goes through the cached list-state (same state the bindable VM subscribes to), which the
/// swap targets — not a fresh subscription to the raw feed.
/// </summary>
[TestClass]
public class Given_GeneratedMock : FeedUITests
{
	private static async Task<IImmutableList<int>?> CurrentItems(SourceContext ctx, IListFeed<int> feed)
	{
		var (result, _) = ctx.GetOrCreateListState(feed).Record();
		// Wait until a defined (Some) message is observed.
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
		using (MockingService.Enable())
		{
			var vm = RecipeModelMockExtensions.Create(MockListFeed.Value(1, 2, 3));
			using var _ = SourceContext.GetOrCreate(vm.Model).AsCurrent();

			var items = await CurrentItems(SourceContext.GetOrCreate(vm.Model), vm.Model.Steps);
			items.Should().BeEquivalentTo(new[] { 1, 2, 3 });
		}
	}

	[TestMethod]
	public async Task When_SetModelReSwaps_Then_ReEmitsLive()
	{
		using (MockingService.Enable())
		{
			var vm = RecipeModelMockExtensions.Create(MockListFeed.Value(1));
			using var _ = SourceContext.GetOrCreate(vm.Model).AsCurrent();

			(await CurrentItems(SourceContext.GetOrCreate(vm.Model), vm.Model.Steps))
				.Should().BeEquivalentTo(new[] { 1 });

			vm.SetModel(new RecipeModelMock { Steps = MockListFeed.Value(7, 8) });

			(await CurrentItems(SourceContext.GetOrCreate(vm.Model), vm.Model.Steps))
				.Should().BeEquivalentTo(new[] { 7, 8 });
		}
	}

	[TestMethod]
	public async Task When_CreateDefault_Then_InputsAreEmpty()
	{
		using (MockingService.Enable())
		{
			var vm = RecipeModelMockExtensions.Create(); // Empty → Steps = None
			using var _ = SourceContext.GetOrCreate(vm.Model).AsCurrent();

			var items = await CurrentItems(SourceContext.GetOrCreate(vm.Model), vm.Model.Steps);
			items.Should().BeNull("Empty pins the input to None");
		}
	}

	[TestMethod]
	public void When_CommandOverridden_Then_VmCommandInvokesMock()
	{
		using (MockingService.Enable())
		{
			var executed = false;
			var vm = RecipeModelMockExtensions.Create(new RecipeModelMock
			{
				Steps = MockListFeed.Value(1),
				Save = MockCommand.Callback(_ => executed = true),
			});

			vm.Save.Should().NotBeNull();
			vm.Save.CanExecute(null).Should().BeTrue();
			vm.Save.Execute(null);
			executed.Should().BeTrue("SetModel routed the mock command through __Mock_SetCommand");
		}
	}
}
