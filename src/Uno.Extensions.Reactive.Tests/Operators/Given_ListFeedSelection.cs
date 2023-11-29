using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

[TestClass]
public partial class Given_ListFeedSelection : FeedTests
{
	[TestMethod]
	public async Task When_Single_With_InvalidInitial_And_UpdateSelectionState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => 15);
		var list = ListFeed.Async(async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection, "sut").Record();

		await sut.WaitForMessages(1);

		await selection.SetAsync(5, CT);

		await sut.Should().BeAsync(r => r
			.Message(Items.Range(10), Error.No, Progress.Final, Selection.Empty)
			.Message(m => m
				.Changed(Changed.Selection)
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(5)))
		);
	}

	[TestMethod]
	public async Task When_Single_With_InvalidInitial_And_UpdateSelectionStateInvalid_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => 15);
		var list = ListFeed.Async(async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection, "sut").Record();

		await sut.WaitForMessages(1);

		await selection.SetAsync(20, CT);

		await sut.Should().BeAsync(r => r
			.Message(Items.Range(10), Error.No, Progress.Final, Selection.Empty)
		);
	}

	[TestMethod]
	public async Task When_Single_With_ValidInitial_And_UpdateSelectionState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => 1);
		var list = ListFeed.Async(async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection, "sut").Record();

		await sut.WaitForMessages(1);

		await selection.SetAsync(5, CT);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(1)))
			.Message(m => m
				.Changed(Changed.Selection)
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(5)))
		);
	}

	[TestMethod]
	public async Task When_Single_With_ValidInitial_And_UpdateSelectionStateInvalid_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => 1);
		var list = ListFeed.Async(async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection, "sut").Record();

		await sut.WaitForMessages(1);

		await selection.SetAsync(15, CT);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(1)))
			.Message(m => m
				.Changed(Changed.Selection)
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Empty))
		);
	}

	[TestMethod]
	public async Task When_Single_With_InvalidInitial_And_UpdateListState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => 15).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await list.TrySelectAsync(5, CT)).Should().Be(true);

		await sut.Should().BeAsync(r => r
			.Message(Items.Range(10), Error.No, Progress.Final, Selection.Empty)
			.Message(m => m
				.Changed(Changed.Selection & MessageAxes.SelectionSource)
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(5)))
		);

		await selection.Should().BeAsync(r => r
			.Message(15, Error.No, Progress.Final)
			.Message(5, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Single_With_InvalidInitial_And_UpdateListStateInvalid_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => 15).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await list.TrySelectAsync(20, CT)).Should().Be(false);

		await sut.Should().BeAsync(r => r
			.Message(Items.Range(10), Error.No, Progress.Final, Selection.Empty)
		);

		await selection.Should().BeAsync(r => r
			.Message(15, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Single_With_ValidInitial_And_UpdateListState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => 1).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await list.TrySelectAsync(5, CT)).Should().Be(true);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(1)))
			.Message(m => m
				.Changed(Changed.Selection & MessageAxes.SelectionSource)
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(5)))
		);

		await selection.Should().BeAsync(r => r
			.Message(1, Error.No, Progress.Final)
			.Message(5, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Single_With_ValidInitial_And_UpdateListStateInvalid_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => 1).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await list.TrySelectAsync(15, CT)).Should().Be(false);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(1)))
		);

		await selection.Should().BeAsync(r => r
			.Message(1, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Single_With_InvalidInitial_And_UpdateSutState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => 15).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await sut.Feed.TrySelectAsync(5, CT)).Should().Be(true);

		await sut.Should().BeAsync(r => r
			.Message(Items.Range(10), Error.No, Progress.Final, Selection.Empty)
			.Message(m => m
				.Changed(Changed.Selection & MessageAxes.SelectionSource)
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(5)))
		);

		await selection.Should().BeAsync(r => r
			.Message(15, Error.No, Progress.Final)
			.Message(5, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Single_With_InvalidInitial_And_UpdateSutStateInvalid_Then_ListStateNotUpdated()
	{
		var selection = State.Value(this, () => 15).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await sut.Feed.TrySelectAsync(20, CT)).Should().Be(false);

		await sut.Should().BeAsync(r => r
			.Message(Items.Range(10), Error.No, Progress.Final, Selection.Empty)
		);

		await selection.Should().BeAsync(r => r
			.Message(15, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Single_With_ValidInitial_And_UpdateSutState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => 1).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await sut.Feed.TrySelectAsync(5, CT)).Should().Be(true);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(1)))
			.Message(m => m
				.Changed(Changed.Selection & MessageAxes.SelectionSource)
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(5)))
		);

		await selection.Should().BeAsync(r => r
			.Message(1, Error.No, Progress.Final)
			.Message(5, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Single_With_ValidInitial_And_UpdateSutStateInvalid_Then_ListStateNotUpdated()
	{
		var selection = State.Value(this, () => 1).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await sut.Feed.TrySelectAsync(15, CT)).Should().Be(false);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(1)))
		);

		await selection.Should().BeAsync(r => r
			.Message(1, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Multiple_With_InvalidInitial_And_UpdateSelectionState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => ImmutableList.Create(15) as IImmutableList<int>);
		var list = ListFeed.Async(async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection, "sut").Record();

		await sut.WaitForMessages(1);

		await selection.UpdateAsync(_ => ImmutableList.Create(5), CT);

		await sut.Should().BeAsync(r => r
			.Message(Items.Range(10), Error.No, Progress.Final, Selection.Empty)
			.Message(m => m
				.Changed(Changed.Selection)
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(5)))
		);
	}

	[TestMethod]
	public async Task When_Multiple_With_InvalidInitial_And_UpdateSelectionStateInvalid_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => ImmutableList.Create(15) as IImmutableList<int>);
		var list = ListFeed.Async(async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection, "sut").Record();

		await sut.WaitForMessages(1);

		await selection.UpdateAsync(_ => ImmutableList.Create(20), CT);

		await sut.Should().BeAsync(r => r
			.Message(Items.Range(10), Error.No, Progress.Final, Selection.Empty)
		);
	}

	[TestMethod]
	public async Task When_Multiple_With_ValidInitial_And_UpdateSelectionState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => ImmutableList.Create(1) as IImmutableList<int>).Record();
		var list = ListFeed.Async(async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		await selection.Feed.UpdateAsync(_ => ImmutableList.Create(5), CT);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(1)))
			.Message(m => m
				.Changed(Changed.Selection)
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(5)))
		);
	}

	[TestMethod]
	public async Task When_Multiple_With_ValidInitial_And_UpdateSelectionStateInvalid_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => ImmutableList.Create(1) as IImmutableList<int>).Record();
		var list = ListFeed.Async(async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		await selection.Feed.UpdateAsync(_ => ImmutableList.Create(15), CT);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(1)))
			.Message(m => m
				.Changed(Changed.Selection)
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Empty))
		);
	}

	[TestMethod]
	public async Task When_Multiple_With_InvalidInitial_And_UpdateListState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => ImmutableList.Create(15) as IImmutableList<int>).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await list.TrySelectAsync(5, CT)).Should().Be(true);

		await sut.Should().BeAsync(r => r
			.Message(Items.Range(10), Error.No, Progress.Final, Selection.Empty)
			.Message(m => m
				.Changed(Changed.Selection & MessageAxes.SelectionSource)
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(5)))
		);

		await selection.Should().BeAsync(r => r
			.Message(Items.Some(15), Error.No, Progress.Final)
			.Message(Items.Some(5), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Multiple_With_InvalidInitial_And_UpdateListStateInvalid_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => ImmutableList.Create(15) as IImmutableList<int>).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await list.TrySelectAsync(20, CT)).Should().Be(false);

		await sut.Should().BeAsync(r => r
			.Message(Items.Range(10), Error.No, Progress.Final, Selection.Empty)
		);

		await selection.Should().BeAsync(r => r
			.Message(Items.Some(15), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Multiple_With_ValidInitial_And_UpdateListState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => ImmutableList.Create(1) as IImmutableList<int>).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await list.TrySelectAsync(5, CT)).Should().Be(true);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(1)))
			.Message(m => m
				.Changed(Changed.Selection & MessageAxes.SelectionSource)
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(5)))
		);

		await selection.Should().BeAsync(r => r
			.Message(Items.Some(1), Error.No, Progress.Final)
			.Message(Items.Some(5), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Multiple_With_ValidInitial_And_UpdateListStateInvalid_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => ImmutableList.Create(1) as IImmutableList<int>).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await list.TrySelectAsync(15, CT)).Should().Be(false);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(1)))
		);

		await selection.Should().BeAsync(r => r
			.Message(Items.Some(1), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Multiple_With_InvalidInitial_And_UpdateSutState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => ImmutableList.Create(15) as IImmutableList<int>).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await sut.Feed.TrySelectAsync(5, CT)).Should().Be(true);

		await sut.Should().BeAsync(r => r
			.Message(Items.Range(10), Error.No, Progress.Final, Selection.Empty)
			.Message(m => m
				.Changed(Changed.Selection & MessageAxes.SelectionSource)
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(5)))
		);

		await selection.Should().BeAsync(r => r
			.Message(Items.Some(15), Error.No, Progress.Final)
			.Message(Items.Some(5), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Multiple_With_InvalidInitial_And_UpdateSutStateInvalid_Then_ListStateNotUpdated()
	{
		var selection = State.Value(this, () => ImmutableList.Create(15) as IImmutableList<int>).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await sut.Feed.TrySelectAsync(20, CT)).Should().Be(false);

		await sut.Should().BeAsync(r => r
			.Message(Items.Range(10), Error.No, Progress.Final, Selection.Empty)
		);

		await selection.Should().BeAsync(r => r
			.Message(Items.Some(15), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Multiple_With_ValidInitial_And_UpdateSutState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => ImmutableList.Create(1) as IImmutableList<int>).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await sut.Feed.TrySelectAsync(5, CT)).Should().Be(true);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(1)))
			.Message(m => m
				.Changed(Changed.Selection & MessageAxes.SelectionSource)
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(5)))
		);

		await selection.Should().BeAsync(r => r
			.Message(Items.Some(1), Error.No, Progress.Final)
			.Message(Items.Some(5), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Multiple_With_ValidInitial_And_UpdateSutStateInvalid_Then_ListStateNotUpdated()
	{
		var selection = State.Value(this, () => ImmutableList.Create(1) as IImmutableList<int>).Record();
		var list = ListState.Async(this, async _ => Enumerable.Range(0, 10).ToImmutableList());
		var sut = ListFeedSelection<int>.Create(list, selection.Feed, "sut").Record();

		await sut.WaitForMessages(1);

		(await sut.Feed.TrySelectAsync(15, CT)).Should().Be(false);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Range(10), Error.No, Progress.Final, Selection.Items(1)))
		);

		await selection.Should().BeAsync(r => r
			.Message(Items.Some(1), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_ProjectedSingle_With_InvalidInitial_And_UpdateSelectionState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => new MyAggregateRoot(15));
		var items = Enumerable.Range(0, 10).Select(i => new MyEntity(i)).ToImmutableList();
		var list = ListFeed.Async(async _ => items);
		var sut = list.Selection(selection, e => e.MyEntityKey).Record();

		await sut.WaitForMessages(1);

		await selection.UpdateAsync(_ => new MyAggregateRoot(5), CT);

		await sut.Should().BeAsync(r => r
			.Message(items, Error.No, Progress.Final, Selection.Empty)
			.Message(m => m
				.Changed(Changed.Selection)
				.Current(items, Error.No, Progress.Final, Selection.Items(new MyEntity(5))))
		);
	}

	[TestMethod]
	public async Task When_ProjectedSingle_With_InvalidInitial_And_UpdateSelectionStateInvalid_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => new MyAggregateRoot(15));
		var items = Enumerable.Range(0, 10).Select(i => new MyEntity(i)).ToImmutableList();
		var list = ListFeed.Async(async _ => items);
		var sut = list.Selection(selection, e => e.MyEntityKey).Record();

		await sut.WaitForMessages(1);

		await selection.UpdateAsync(_ => new MyAggregateRoot(20), CT);

		await sut.Should().BeAsync(r => r
			.Message(items, Error.No, Progress.Final, Selection.Empty)
		);
	}

	[TestMethod]
	public async Task When_ProjectedSingle_With_ValidInitial_And_UpdateSelectionState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => new MyAggregateRoot(1));
		var items = Enumerable.Range(0, 10).Select(i => new MyEntity(i)).ToImmutableList();
		var list = ListFeed.Async(async _ => items);
		var sut = list.Selection(selection, e => e.MyEntityKey).Record();

		await sut.WaitForMessages(1);

		await selection.UpdateAsync(_ => new MyAggregateRoot(5), CT);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(items, Error.No, Progress.Final, Selection.Items(new MyEntity(1))))
			.Message(m => m
				.Changed(Changed.Selection)
				.Current(items, Error.No, Progress.Final, Selection.Items(new MyEntity(5))))
		);
	}

	[TestMethod]
	public async Task When_ProjectedSingle_With_ValidInitial_And_UpdateSelectionStateInvalid_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => new MyAggregateRoot(1));
		var items = Enumerable.Range(0, 10).Select(i => new MyEntity(i)).ToImmutableList();
		var list = ListFeed.Async(async _ => items);
		var sut = list.Selection(selection, e => e.MyEntityKey).Record();

		await sut.WaitForMessages(1);

		await selection.UpdateAsync(_ => new MyAggregateRoot(15), CT);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(items, Error.No, Progress.Final, Selection.Items(new MyEntity(1))))
			.Message(m => m
				.Changed(Changed.Selection)
				.Current(items, Error.No, Progress.Final, Selection.Empty))
		);
	}

	[TestMethod]
	public async Task When_ProjectedSingle_With_InvalidInitial_And_UpdateListState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => new MyAggregateRoot(15)).Record();
		var items = Enumerable.Range(0, 10).Select(i => new MyEntity(i)).ToImmutableList();
		var list = ListState.Async(this, async _ => items);
		var sut = list.Selection(selection.Feed, e => e.MyEntityKey).Record();

		await sut.WaitForMessages(1);

		(await list.TrySelectAsync(new MyEntity(5), CT)).Should().Be(true);

		await sut.Should().BeAsync(r => r
			.Message(items, Error.No, Progress.Final, Selection.Empty)
			.Message(m => m
				.Changed(Changed.Selection & MessageAxes.SelectionSource)
				.Current(items, Error.No, Progress.Final, Selection.Items(new MyEntity(5))))
		);

		await selection.Should().BeAsync(r => r
			.Message(new MyAggregateRoot(15), Error.No, Progress.Final)
			.Message(new MyAggregateRoot(5), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_ProjectedSingle_With_InvalidInitial_And_UpdateListStateInvalid_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => new MyAggregateRoot(15)).Record();
		var items = Enumerable.Range(0, 10).Select(i => new MyEntity(i)).ToImmutableList();
		var list = ListState.Async(this, async _ => items);
		var sut = list.Selection(selection.Feed, e => e.MyEntityKey).Record();

		await sut.WaitForMessages(1);

		(await list.TrySelectAsync(new MyEntity(20), CT)).Should().Be(false);

		await sut.Should().BeAsync(r => r
			.Message(items, Error.No, Progress.Final, Selection.Empty)
		);

		await selection.Should().BeAsync(r => r
			.Message(new MyAggregateRoot(15), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_ProjectedSingle_With_ValidInitial_And_UpdateListState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => new MyAggregateRoot(1)).Record();
		var items = Enumerable.Range(0, 10).Select(i => new MyEntity(i)).ToImmutableList();
		var list = ListState.Async(this, async _ => items);
		var sut = list.Selection(selection.Feed, e => e.MyEntityKey).Record();

		await sut.WaitForMessages(1);

		(await list.TrySelectAsync(new MyEntity(5), CT)).Should().Be(true);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(items, Error.No, Progress.Final, Selection.Items(new MyEntity(1))))
			.Message(m => m
				.Changed(Changed.Selection & MessageAxes.SelectionSource)
				.Current(items, Error.No, Progress.Final, Selection.Items(new MyEntity(5))))
		);

		await selection.Should().BeAsync(r => r
			.Message(new MyAggregateRoot(1), Error.No, Progress.Final)
			.Message(new MyAggregateRoot(5), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_ProjectedSingle_With_ValidInitial_And_UpdateListStateInvalid_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => new MyAggregateRoot(1)).Record();
		var items = Enumerable.Range(0, 10).Select(i => new MyEntity(i)).ToImmutableList();
		var list = ListState.Async(this, async _ => items);
		var sut = list.Selection(selection.Feed, e => e.MyEntityKey).Record();

		await sut.WaitForMessages(1);

		(await list.TrySelectAsync(new MyEntity(15), CT)).Should().Be(false);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(items, Error.No, Progress.Final, Selection.Items(new MyEntity(1))))
		);

		await selection.Should().BeAsync(r => r
			.Message(new MyAggregateRoot(1), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_ProjectedSingle_With_InvalidInitial_And_UpdateSutState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => new MyAggregateRoot(15)).Record();
		var items = Enumerable.Range(0, 10).Select(i => new MyEntity(i)).ToImmutableList();
		var list = ListState.Async(this, async _ => items);
		var sut = list.Selection(selection.Feed, e => e.MyEntityKey).Record();

		await sut.WaitForMessages(1);

		(await sut.Feed.TrySelectAsync(new MyEntity(5), CT)).Should().Be(true);

		await sut.Should().BeAsync(r => r
			.Message(items, Error.No, Progress.Final, Selection.Empty)
			.Message(m => m
				.Changed(Changed.Selection & MessageAxes.SelectionSource)
				.Current(items, Error.No, Progress.Final, Selection.Items(new MyEntity(5))))
		);

		await selection.Should().BeAsync(r => r
			.Message(new MyAggregateRoot(15), Error.No, Progress.Final)
			.Message(new MyAggregateRoot(5), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_ProjectedSingle_With_InvalidInitial_And_UpdateSutStateInvalid_Then_ListStateNotUpdated()
	{
		var selection = State.Value(this, () => new MyAggregateRoot(15)).Record();
		var items = Enumerable.Range(0, 10).Select(i => new MyEntity(i)).ToImmutableList();
		var list = ListState.Async(this, async _ => items);
		var sut = list.Selection(selection.Feed, e => e.MyEntityKey).Record();

		await sut.WaitForMessages(1);

		(await sut.Feed.TrySelectAsync(new MyEntity(20), CT)).Should().Be(false);

		await sut.Should().BeAsync(r => r
			.Message(items, Error.No, Progress.Final, Selection.Empty)
		);

		await selection.Should().BeAsync(r => r
			.Message(new MyAggregateRoot(15), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_ProjectedSingle_With_ValidInitial_And_UpdateSutState_Then_ListStateUpdated()
	{
		var selection = State.Value(this, () => new MyAggregateRoot(1)).Record();
		var items = Enumerable.Range(0, 10).Select(i => new MyEntity(i)).ToImmutableList();
		var list = ListState.Async(this, async _ => items);
		var sut = list.Selection(selection.Feed, e => e.MyEntityKey).Record();

		await sut.WaitForMessages(1);

		(await sut.Feed.TrySelectAsync(new MyEntity(5), CT)).Should().Be(true);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(items, Error.No, Progress.Final, Selection.Items(new MyEntity(1))))
			.Message(m => m
				.Changed(Changed.Selection & MessageAxes.SelectionSource)
				.Current(items, Error.No, Progress.Final, Selection.Items(new MyEntity(5))))
		);

		await selection.Should().BeAsync(r => r
			.Message(new MyAggregateRoot(1), Error.No, Progress.Final)
			.Message(new MyAggregateRoot(5), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_ProjectedSingle_With_ValidInitial_And_UpdateSutStateInvalid_Then_ListStateNotUpdated()
	{
		var selection = State.Value(this, () => new MyAggregateRoot(1)).Record();
		var items = Enumerable.Range(0, 10).Select(i => new MyEntity(i)).ToImmutableList();
		var list = ListState.Async(this, async _ => items);
		var sut = list.Selection(selection.Feed, e => e.MyEntityKey).Record();

		await sut.WaitForMessages(1);

		(await sut.Feed.TrySelectAsync(new MyEntity(15), CT)).Should().Be(false);

		await sut.Should().BeAsync(r => r
			.Message(m => m
				.Current(items, Error.No, Progress.Final, Selection.Items(new MyEntity(1))))
		);

		await selection.Should().BeAsync(r => r
			.Message(new MyAggregateRoot(1), Error.No, Progress.Final)
		);
	}

	public partial record MyEntity(int Key) : IKeyed<int>;

	public partial record MyAggregateRoot
	{
		public MyAggregateRoot()
		{
		}

		public MyAggregateRoot(int key)
		{
			MyEntityKey = key;
		}

		public int? MyEntityKey { get; init; }
	}
}
