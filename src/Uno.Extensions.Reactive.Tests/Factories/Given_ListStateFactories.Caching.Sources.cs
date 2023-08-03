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
public partial class Given_ListStateFactories : FeedTests
{
	[TestMethod]
	public void When_Create_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		Func<CancellationToken, IAsyncEnumerable<Message<IImmutableList<object>>>> sourceProvider1 = ct => default!;
		Func<CancellationToken, IAsyncEnumerable<Message<IImmutableList<object>>>> sourceProvider2 = ct => default!;

		var sut = ListState<object>.Create(owner1, sourceProvider1);

		var inst1_1 = ListState<object>.Create(owner1, sourceProvider1);
		var inst1_2 = ListState<object>.Create(owner1, sourceProvider2);
		var inst2_1 = ListState<object>.Create(owner2, sourceProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using sourceProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same sourceProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_CreateWithoutCT_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		Func<IAsyncEnumerable<Message<IImmutableList<object>>>> sourceProvider1 = () => default!;
		Func<IAsyncEnumerable<Message<IImmutableList<object>>>> sourceProvider2 = () => default!;

		var sut = ListState<object>.Create(owner1, sourceProvider1);

		var inst1_1 = ListState<object>.Create(owner1, sourceProvider1);
		var inst1_2 = ListState<object>.Create(owner1, sourceProvider2);
		var inst2_1 = ListState<object>.Create(owner2, sourceProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using sourceProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same sourceProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_AsyncOption_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		AsyncFunc<Option<IImmutableList<object>>> valueProvider1 = async ct => default!;
		AsyncFunc<Option<IImmutableList<object>>> valueProvider2 = async ct => default!;

		var sut = ListState<object>.Async(owner1, valueProvider1);

		var inst1_1 = ListState<object>.Async(owner1, valueProvider1);
		var inst1_2 = ListState<object>.Async(owner1, valueProvider2);
		var inst2_1 = ListState<object>.Async(owner2, valueProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using valueProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same valueProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_Async_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		AsyncFunc<IImmutableList<object>> valueProvider1 = async ct => ImmutableList<object>.Empty;
		AsyncFunc<IImmutableList<object>> valueProvider2 = async ct => ImmutableList<object>.Empty;

		var sut = ListState<object>.Async(owner1, valueProvider1);

		var inst1_1 = ListState<object>.Async(owner1, valueProvider1);
		var inst1_2 = ListState<object>.Async(owner1, valueProvider2);
		var inst2_1 = ListState<object>.Async(owner2, valueProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using valueProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same valueProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_AsyncOptionWithSignal_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		AsyncFunc<Option<IImmutableList<object>>> valueProvider1 = async ct => default!;
		AsyncFunc<Option<IImmutableList<object>>> valueProvider2 = async ct => default!;
		var signal1 = new Signal();
		var signal2 = new Signal();

		var sut = ListState<object>.Async(owner1, valueProvider1, signal1);

		var inst1_1_1 = ListState<object>.Async(owner1, valueProvider1, signal1);
		var inst1_1_2 = ListState<object>.Async(owner1, valueProvider1, signal2);
		var inst1_2_1 = ListState<object>.Async(owner1, valueProvider2, signal1);
		var inst2_1_1 = ListState<object>.Async(owner2, valueProvider1, signal1);

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
		AsyncFunc<IImmutableList<object>> valueProvider1 = async ct => default!;
		AsyncFunc<IImmutableList<object>> valueProvider2 = async ct => default!;
		var signal1 = new Signal();
		var signal2 = new Signal();

		var sut = ListState<object>.Async(owner1, valueProvider1, signal1);

		var inst1_1_1 = ListState<object>.Async(owner1, valueProvider1, signal1);
		var inst1_1_2 = ListState<object>.Async(owner1, valueProvider1, signal2);
		var inst1_2_1 = ListState<object>.Async(owner1, valueProvider2, signal1);
		var inst2_1_1 = ListState<object>.Async(owner2, valueProvider1, signal1);

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
		Func<CancellationToken, IAsyncEnumerable<Option<IImmutableList<object>>>> enumerableProvider1 = ct => default!;
		Func<CancellationToken, IAsyncEnumerable<Option<IImmutableList<object>>>> enumerableProvider2 = ct => default!;

		var sut = ListState<object>.AsyncEnumerable(owner1, enumerableProvider1);

		var inst1_1 = ListState<object>.AsyncEnumerable(owner1, enumerableProvider1);
		var inst1_2 = ListState<object>.AsyncEnumerable(owner1, enumerableProvider2);
		var inst2_1 = ListState<object>.AsyncEnumerable(owner2, enumerableProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using enumerableProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same enumerableProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_AsyncEnumerableOptionsWithoutCT_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		Func<IAsyncEnumerable<Option<IImmutableList<object>>>> enumerableProvider1 = () => default!;
		Func<IAsyncEnumerable<Option<IImmutableList<object>>>> enumerableProvider2 = () => default!;

		var sut = ListState<object>.AsyncEnumerable(owner1, enumerableProvider1);

		var inst1_1 = ListState<object>.AsyncEnumerable(owner1, enumerableProvider1);
		var inst1_2 = ListState<object>.AsyncEnumerable(owner1, enumerableProvider2);
		var inst2_1 = ListState<object>.AsyncEnumerable(owner2, enumerableProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using enumerableProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same enumerableProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_AsyncEnumerable_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		Func<CancellationToken, IAsyncEnumerable<IImmutableList<object>>> enumerableProvider1 = ct => default!;
		Func<CancellationToken, IAsyncEnumerable<IImmutableList<object>>> enumerableProvider2 = ct => default!;

		var sut = ListState<object>.AsyncEnumerable(owner1, enumerableProvider1);

		var inst1_1 = ListState<object>.AsyncEnumerable(owner1, enumerableProvider1);
		var inst1_2 = ListState<object>.AsyncEnumerable(owner1, enumerableProvider2);
		var inst2_1 = ListState<object>.AsyncEnumerable(owner2, enumerableProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using enumerableProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same enumerableProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	[TestMethod]
	public void When_AsyncEnumerableWithoutCT_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		Func<IAsyncEnumerable<IImmutableList<object>>> enumerableProvider1 = () => default!;
		Func<IAsyncEnumerable<IImmutableList<object>>> enumerableProvider2 = () => default!;

		var sut = ListState<object>.AsyncEnumerable(owner1, enumerableProvider1);

		var inst1_1 = ListState<object>.AsyncEnumerable(owner1, enumerableProvider1);
		var inst1_2 = ListState<object>.AsyncEnumerable(owner1, enumerableProvider2);
		var inst2_1 = ListState<object>.AsyncEnumerable(owner2, enumerableProvider1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using enumerableProvider");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same enumerableProvider");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}

	//[TestMethod]
	//public void When_PaginatedAsyncByCursor_Then_Cached()
	//{
	//	object owner1 = new();
	//	object owner2 = new();
	//	object firstPage1 = new();
	//	object firstPage2 = new();
	//	GetPage<object, object> getPage1 = async (_, __, ct) => default!;
	//	GetPage<object, object> getPage2 = async (_, __, ct) => default!;

	//	var inst1_1 = ListState<object>.PaginatedAsyncByCursor(owner1, firstPage1, getPage1);
	//	var inst1_2 = ListState<object>.PaginatedAsyncByCursor(owner1, firstPage1, getPage2);
	//	var inst2_1 = ListState<object>.PaginatedAsyncByCursor(owner1, firstPage2, getPage1);
	//	var inst2_2 = ListState<object>.PaginatedAsyncByCursor(owner1, firstPage2, getPage2);
	//	var inst1_1_prime = ListState<object>.PaginatedAsyncByCursor(owner1, firstPage1, getPage1);
	//	var inst1_2_prime = ListState<object>.PaginatedAsyncByCursor(owner1, firstPage1, getPage2);
	//	var inst2_1_prime = ListState<object>.PaginatedAsyncByCursor(owner1, firstPage2, getPage1);
	//	var inst2_2_prime = ListState<object>.PaginatedAsyncByCursor(owner1, firstPage2, getPage2);

	//	inst1_1_prime.Should().BeSameAs(inst1_1, "instance should have been cached using firstPage and getPage");
	//	inst1_1_prime.Should().NotBeSameAs(inst1_2, "instance should have been cached as not the exact same combination of firstPage and getPage");
	//	inst1_1_prime.Should().NotBeSameAs(inst2_1, "instance should have been cached as not the exact same combination of firstPage and getPage");
	//	inst1_1_prime.Should().NotBeSameAs(inst2_2, "instance should have been cached as not the exact same combination of firstPage and getPage");

	//	inst1_2_prime.Should().NotBeSameAs(inst1_1, "instance should have been cached as not the exact same combination of firstPage and getPage");
	//	inst1_2_prime.Should().BeSameAs(inst1_2, "instance should have been cached using firstPage and getPage");
	//	inst1_2_prime.Should().NotBeSameAs(inst2_1, "instance should have been cached as not the exact same combination of firstPage and getPage");
	//	inst1_2_prime.Should().NotBeSameAs(inst2_2, "instance should have been cached as not the exact same combination of firstPage and getPage");

	//	inst2_1_prime.Should().NotBeSameAs(inst1_1, "instance should have been cached as not the exact same combination of firstPage and getPage");
	//	inst2_1_prime.Should().NotBeSameAs(inst1_2, "instance should have been cached as not the exact same combination of firstPage and getPage");
	//	inst2_1_prime.Should().BeSameAs(inst2_1, "instance should have been cached using firstPage and getPage");
	//	inst2_1_prime.Should().NotBeSameAs(inst2_2, "instance should have been cached as not the exact same combination of firstPage and getPage");

	//	inst2_2_prime.Should().NotBeSameAs(inst1_1, "instance should have been cached as not the exact same combination of firstPage and getPage");
	//	inst2_2_prime.Should().NotBeSameAs(inst1_2, "instance should have been cached as not the exact same combination of firstPage and getPage");
	//	inst2_2_prime.Should().NotBeSameAs(inst2_1, "instance should have been cached as not the exact same combination of firstPage and getPage");
	//	inst2_2_prime.Should().BeSameAs(inst2_2, "instance should have been cached using firstPage and getPage");
	//}

	[TestMethod]
	public void When_PaginatedAsync_Then_Cached()
	{
		object owner1 = new();
		object owner2 = new();
		AsyncFunc<PageRequest, IImmutableList<object>> getPage1 = async (_, ct) => default!;
		AsyncFunc<PageRequest, IImmutableList<object>> getPage2 = async (_, ct) => default!;

		var sut = ListState<object>.PaginatedAsync(owner1, getPage1);

		var inst1_1 = ListState<object>.PaginatedAsync(owner1, getPage1);
		var inst1_2 = ListState<object>.PaginatedAsync(owner1, getPage2);
		var inst2_1 = ListState<object>.PaginatedAsync(owner2, getPage1);

		inst1_1.Should().BeSameAs(sut, "instance should have been cached on owner using getPage");
		inst1_2.Should().NotBeSameAs(sut, "instance should not have been cached as not the same getPage");
		inst2_1.Should().NotBeSameAs(sut, "instance should not have been cached as not the same owner");
	}
}
