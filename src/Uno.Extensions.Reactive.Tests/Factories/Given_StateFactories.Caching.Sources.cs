using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Factories;

[TestClass]
public partial class Given_StateFactories : FeedTests
{
	[TestMethod]
	public void When_Create_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		Func<CancellationToken, IAsyncEnumerable<Message<object>>> sourceProvider1 = ct => default!;
		Func<CancellationToken, IAsyncEnumerable<Message<object>>> sourceProvider2 = ct => default!;

		var sut = State<object>.Create(owner1, sourceProvider1);

		var inst1_1 = State<object>.Create(owner1, sourceProvider1);
		var inst1_2 = State<object>.Create(owner1, sourceProvider2);
		var inst2_1 = State<object>.Create(owner2, sourceProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using sourceProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same sourceProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_CreateWithoutCT_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		Func<IAsyncEnumerable<Message<object>>> sourceProvider1 = () => default!;
		Func<IAsyncEnumerable<Message<object>>> sourceProvider2 = () => default!;

		var sut = State<object>.Create(owner1, sourceProvider1);

		var inst1_1 = State<object>.Create(owner1, sourceProvider1);
		var inst1_2 = State<object>.Create(owner1, sourceProvider2);
		var inst2_1 = State<object>.Create(owner2, sourceProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using sourceProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same sourceProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_AsyncOption_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		AsyncFunc<Option<object>> valueProvider1 = async ct => default!;
		AsyncFunc<Option<object>> valueProvider2 = async ct => default!;

		var sut = State<object>.Async(owner1, valueProvider1);

		var inst1_1 = State<object>.Async(owner1, valueProvider1);
		var inst1_2 = State<object>.Async(owner1, valueProvider2);
		var inst2_1 = State<object>.Async(owner2, valueProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using valueProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same valueProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_Async_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		AsyncFunc<object> valueProvider1 = async ct => ImmutableList<object>.Empty;
		AsyncFunc<object> valueProvider2 = async ct => ImmutableList<object>.Empty;

		var sut = State<object>.Async(owner1, valueProvider1);

		var inst1_1 = State<object>.Async(owner1, valueProvider1);
		var inst1_2 = State<object>.Async(owner1, valueProvider2);
		var inst2_1 = State<object>.Async(owner2, valueProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using valueProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same valueProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_AsyncOptionWithSignal_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		AsyncFunc<Option<object>> valueProvider1 = async ct => default!;
		AsyncFunc<Option<object>> valueProvider2 = async ct => default!;
		var signal1 = new Signal();
		var signal2 = new Signal();

		var sut = State<object>.Async(owner1, valueProvider1, signal1);

		var inst1_1_1 = State<object>.Async(owner1, valueProvider1, signal1);
		var inst1_1_2 = State<object>.Async(owner1, valueProvider1, signal2);
		var inst1_2_1 = State<object>.Async(owner1, valueProvider2, signal1);
		var inst2_1_1 = State<object>.Async(owner2, valueProvider1, signal1);

		inst1_1_1.Should().BeSameAs(sut, "instance should have been cached on owner using valueProvider and signal");
		inst1_1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same signal");
		inst1_2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same valueProvider");
		inst2_1_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_AsyncWithSignal_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		AsyncFunc<object> valueProvider1 = async ct => default!;
		AsyncFunc<object> valueProvider2 = async ct => default!;
		var signal1 = new Signal();
		var signal2 = new Signal();

		var sut = State<object>.Async(owner1, valueProvider1, signal1);

		var inst1_1_1 = State<object>.Async(owner1, valueProvider1, signal1);
		var inst1_1_2 = State<object>.Async(owner1, valueProvider1, signal2);
		var inst1_2_1 = State<object>.Async(owner1, valueProvider2, signal1);
		var inst2_1_1 = State<object>.Async(owner2, valueProvider1, signal1);

		inst1_1_1.Should().BeSameAs(sut, "instance should have been cached on owner using valueProvider and signal");
		inst1_1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same signal");
		inst1_2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same valueProvider");
		inst2_1_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_AsyncEnumerableOptions_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		Func<CancellationToken, IAsyncEnumerable<Option<object>>> enumerableProvider1 = ct => default!;
		Func<CancellationToken, IAsyncEnumerable<Option<object>>> enumerableProvider2 = ct => default!;

		var sut = State<object>.AsyncEnumerable(owner1, enumerableProvider1);

		var inst1_1 = State<object>.AsyncEnumerable(owner1, enumerableProvider1);
		var inst1_2 = State<object>.AsyncEnumerable(owner1, enumerableProvider2);
		var inst2_1 = State<object>.AsyncEnumerable(owner2, enumerableProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using enumerableProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same enumerableProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_AsyncEnumerableOptionsWithoutCT_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		Func<IAsyncEnumerable<Option<object>>> enumerableProvider1 = () => default!;
		Func<IAsyncEnumerable<Option<object>>> enumerableProvider2 = () => default!;

		var sut = State<object>.AsyncEnumerable(owner1, enumerableProvider1);

		var inst1_1 = State<object>.AsyncEnumerable(owner1, enumerableProvider1);
		var inst1_2 = State<object>.AsyncEnumerable(owner1, enumerableProvider2);
		var inst2_1 = State<object>.AsyncEnumerable(owner2, enumerableProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using enumerableProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same enumerableProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_AsyncEnumerable_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		Func<CancellationToken, IAsyncEnumerable<object>> enumerableProvider1 = ct => default!;
		Func<CancellationToken, IAsyncEnumerable<object>> enumerableProvider2 = ct => default!;

		var sut = State<object>.AsyncEnumerable(owner1, enumerableProvider1);

		var inst1_1 = State<object>.AsyncEnumerable(owner1, enumerableProvider1);
		var inst1_2 = State<object>.AsyncEnumerable(owner1, enumerableProvider2);
		var inst2_1 = State<object>.AsyncEnumerable(owner2, enumerableProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using enumerableProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same enumerableProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_AsyncEnumerableWithoutCT_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		Func<IAsyncEnumerable<object>> enumerableProvider1 = () => default!;
		Func<IAsyncEnumerable<object>> enumerableProvider2 = () => default!;

		var sut = State<object>.AsyncEnumerable(owner1, enumerableProvider1);

		var inst1_1 = State<object>.AsyncEnumerable(owner1, enumerableProvider1);
		var inst1_2 = State<object>.AsyncEnumerable(owner1, enumerableProvider2);
		var inst2_1 = State<object>.AsyncEnumerable(owner2, enumerableProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using enumerableProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same enumerableProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}
}
