using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Generator;

/// <summary>
/// Verifies that a partial record whose name ends with "Model" can be used as an
/// <see cref="IListFeed{T}"/> item type without causing a duplicate hintName crash
/// in the FeedsGenerator. The record name matches the ImplicitBindables "Model$"
/// pattern, so the generator treats it as both a model AND a feed item type.
/// Unique hintName suffixes (.Reactive, .Bindings, .ViewModel/.Bindable) prevent collisions.
/// </summary>
[TestClass]
public partial class Given_PartialRecordEndingWithModel_Then_NoHintNameCollision : FeedUITests
{
	// If these tests compile and run, the generator did not crash on duplicate hintNames.

	[TestMethod]
	public async Task HostModel_GeneratesViewModel()
	{
		// The host model that uses IListFeed<T> of a record ending with "Model" should generate its ViewModel.
		await using var vm = new HintNameCollision_HostViewModel();

		Assert.IsNotNull(vm);
	}

	[TestMethod]
	public async Task HostModel_ListFeedProperty_IsAccessible()
	{
		await using var vm = new HintNameCollision_HostViewModel();

		// The Items property backed by IListFeed<HintNameCollision_ItemModel> should be generated.
		Assert.IsNotNull(vm.Items);
		vm.Items.Should().BeAssignableTo<IListState<HintNameCollision_ItemModel>>();
	}

	[TestMethod]
	public void ItemRecord_EndingWithModel_DoesNotPreventGeneration()
	{
		// The partial record ending with "Model" compiles successfully.
		// If the generator had a hintName collision, the entire FeedsGenerator would have crashed
		// and NO types (including HostViewModel) would have been generated.
		var item = new HintNameCollision_ItemModel("key1", "value1");

		Assert.AreEqual("key1", item.ItemId);
		Assert.AreEqual("value1", item.Value);
	}

	// --- Test types ---

	/// <summary>
	/// A model that references a partial record ending with "Model" in a ListFeed.
	/// </summary>
	public partial class HintNameCollision_HostModel
	{
		public IListFeed<HintNameCollision_ItemModel> Items => ListFeed.Async(async ct =>
			ImmutableList.Create(new HintNameCollision_ItemModel("a", "1"), new HintNameCollision_ItemModel("b", "2")));
	}

	/// <summary>
	/// A partial record whose name ends with "Model" — matches the ImplicitBindables "Model$" pattern.
	/// Used as an IListFeed item type in <see cref="HintNameCollision_HostModel"/>.
	/// ReactiveBindable(false) opts out of model treatment: without it the generator would produce
	/// two classes with the same name but different base types (one from the model path, one from
	/// the feed item record path).
	/// </summary>
	[ReactiveBindable(false)]
	public partial record HintNameCollision_ItemModel(string ItemId, string Value);
}
