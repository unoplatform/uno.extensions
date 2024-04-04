using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Messaging;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Messaging;

[TestClass]
public class Given_Messaging : FeedTests
{
	[TestMethod]
	public async Task When_Created_Then_StateNotUpdated()
	{
		var messenger = new WeakReferenceMessenger();
		var state = State<MyEntity>.Empty(this);

		messenger.Observe(state, i => i.Key);

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Created, new(42)));

		var result = await state.Data(CT);
		result.Should().Be(Option<MyEntity>.None());
	}

	[TestMethod]
	public async Task When_Updated_Then_StateUpdated()
	{
		var messenger = new WeakReferenceMessenger();
		var state = State.Value(this, () => new MyEntity(42));

		messenger.Observe(state, i => i.Key);

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Updated, new(42, 1)));

		var result = await state;
		result.Should().BeEquivalentTo(new MyEntity(42, 1));
	}

	[TestMethod]
	public async Task When_Deleted_Then_StateUpdated()
	{
		var messenger = new WeakReferenceMessenger();
		var state = State.Value(this, () => new MyEntity(42));

		messenger.Observe(state, i => i.Key);

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Deleted, new(42, 1)));

		var result = await state.Data(CT);
		result.Should().Be(Option<MyEntity>.None());
	}

	[TestMethod]
	public async Task When_Created_Then_ListStateUpdated()
	{
		var messenger = new WeakReferenceMessenger();
		var state = ListState<MyEntity>.Empty(this);

		messenger.Observe(state, i => i.Key);

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Created, new(42)));

		var result = await state;
		result.Should().BeEquivalentTo(Items(42));
	}

	[TestMethod]
	public async Task When_Updated_Then_ListStateUpdated()
	{
		var messenger = new WeakReferenceMessenger();
		var state = ListState<MyEntity>.Value(this, () => Items(42));

		messenger.Observe(state, i => i.Key);

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Updated, new(42, 1)));

		var result = await state;
		result.Should().BeEquivalentTo(Items((42, 1)));
	}

	[TestMethod]
	public async Task When_Deleted_Then_ListStateUpdated()
	{
		var messenger = new WeakReferenceMessenger();
		var state = ListState<MyEntity>.Value(this, () => Items(42));

		messenger.Observe(state, i => i.Key);

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Deleted, new(42, 1)));

		var result = await state;
		result.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_CreatedUsingOther_Then_StateNotUpdated()
	{
		var messenger = new WeakReferenceMessenger();
		var other = State.Value(this, () => 0);
		var state = State<MyEntity>.Empty(this);

		messenger.Observe(state, other, (o, e) => true, i => i.Key);

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Created, new(42)));

		var result = await state.Data(CT);
		result.Should().Be(Option<MyEntity>.None());
	}

	[TestMethod]
	public async Task When_UpdatedUsingOther_Then_StateUpdatedIfMatchOther()
	{
		var messenger = new WeakReferenceMessenger();
		var other = State.Value(this, () => 1);
		var state = State.Value(this, () => new MyEntity(42));

		messenger.Observe(state, other, (o, e) => e.Version == o, i => i.Key);

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Updated, new(42, 1)));
		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Updated, new(42, 2)));

		var result = await state;
		result.Should().BeEquivalentTo(new MyEntity(42, 1));
	}

	[TestMethod]
	public async Task When_DeletedUsingOther_Then_StateUpdatedIfMatchOther()
	{
		var messenger = new WeakReferenceMessenger();
		var other = State.Value(this, () => 2);
		var state = State.Value(this, () => new MyEntity(42));

		messenger.Observe(state, other, (o, e) => e.Version == o, i => i.Key);

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Deleted, new(42, 1)));
		var intermediate = await state;
		intermediate.Should().BeEquivalentTo(new MyEntity(42));

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Deleted, new(42, 2)));
		var result = await state.Data(CT);
		result.Should().Be(Option<MyEntity>.None());
	}

	[TestMethod]
	public async Task When_CreatedUsingOther_Then_ListStateUpdatedIfMatchOther()
	{
		var messenger = new WeakReferenceMessenger();
		var other = State.Value(this, () => 1);
		var state = ListState<MyEntity>.Empty(this);

		messenger.Observe(state, other, (o, e) => e.Version == o, i => i.Key);

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Created, new(42, 1)));
		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Created, new(43, 2)));

		var result = await state;
		result.Should().BeEquivalentTo(Items((42, 1)));
	}

	[TestMethod]
	public async Task When_UpdatedUsingOther_Then_ListStateUpdatedIfMatchOther()
	{
		var messenger = new WeakReferenceMessenger();
		var other = State.Value(this, () => 1);
		var state = ListState<MyEntity>.Value(this, () => Items((42, 0), (43, 1)));

		messenger.Observe(state, other, (o, e) => e.Version == o, i => i.Key);

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Updated, new(42, 1)));
		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Updated, new(43, 2)));

		var result = await state;
		result.Should().BeEquivalentTo(Items((42, 1), (43, 1)));
	}

	[TestMethod]
	public async Task When_DeletedUsingOther_Then_ListStateUpdatedIfMatchOther()
	{
		var messenger = new WeakReferenceMessenger();
		var other = State.Value(this, () => 1);
		var state = ListState<MyEntity>.Value(this, () => Items((42, 0), (43, 1)));

		messenger.Observe(state, other, (o, e) => e.Version == o, i => i.Key);

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Deleted, new(42, 1)));
		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Deleted, new(43, 2)));

		var result = await state;
		result.Should().BeEquivalentTo(Items((43, 1)));
	}

	[TestMethod]
	public async Task When_DisposedAndUpdated_Then_StateNotUpdated()
	{
		var messenger = new WeakReferenceMessenger();
		var state = State.Value(this, () => new MyEntity(42));

		messenger.Observe(state, i => i.Key).Dispose();

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Updated, new(42, 1)));

		var result = await state;
		result.Should().BeEquivalentTo(new MyEntity(42));
	}

	[TestMethod]
	public async Task When_DisposedAndUpdated_Then_ListStateNotUpdated()
	{
		var messenger = new WeakReferenceMessenger();
		var state = ListState<MyEntity>.Value(this, () => Items(42));

		messenger.Observe(state, i => i.Key).Dispose();

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Updated, new(42, 1)));

		var result = await state;
		result.Should().BeEquivalentTo(Items(42));
	}

	[TestMethod]
	public async Task When_DisposedAndUpdatedUsingOther_Then_StateNotUpdated()
	{
		var messenger = new WeakReferenceMessenger();
		var other = State.Value(this, () => 1);
		var state = State.Value(this, () => new MyEntity(42));

		messenger.Observe(state, other, (o, e) => e.Version == o, i => i.Key).Dispose();

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Updated, new(42, 1)));

		var result = await state;
		result.Should().BeEquivalentTo(new MyEntity(42));
	}

	[TestMethod]
	public async Task When_DisposedAndUpdatedUsingOther_Then_ListStateNotUpdated()
	{
		var messenger = new WeakReferenceMessenger();
		var other = State.Value(this, () => 1);
		var state = ListState<MyEntity>.Value(this, () => Items((42, 0), (43, 1)));

		messenger.Observe(state, other, (o, e) => e.Version == o, i => i.Key).Dispose();

		messenger.Send(new EntityMessage<MyEntity>(EntityChange.Updated, new(42, 1)));

		var result = await state;
		result.Should().BeEquivalentTo(Items((42, 0), (43, 1)));
	}

	private static IImmutableList<MyEntity> Items(params int[] items)
		=> items.Select(i => new MyEntity(i)).ToImmutableList();

	private static IImmutableList<MyEntity> Items(params (int key, int version)[] items)
		=> items.Select(i => new MyEntity(i.key, i.version)).ToImmutableList();

	[ImplicitKeys(IsEnabled = false)]
	private record MyEntity(int Key, int Version = 0);
}
