using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Factories;

[TestClass]
public partial class Given_ListFeedFactories : FeedTests
{
	[TestMethod]
	public void When_Create_Then_Cached()
	{
		Func<CancellationToken, IAsyncEnumerable<Message<IImmutableList<object>>>> sourceProvider1 = ct => default!;
		Func<CancellationToken, IAsyncEnumerable<Message<IImmutableList<object>>>> sourceProvider2 = ct => default!;

		var inst1 = ListFeed<object>.Create(sourceProvider1);
		var inst2 = ListFeed<object>.Create(sourceProvider2);
		var inst1_prime = ListFeed<object>.Create(sourceProvider1);
		var inst2_prime = ListFeed<object>.Create(sourceProvider2);

		inst1_prime.Should().BeSameAs(inst1, "instance should have been cached using sourceProvider");
		inst1_prime.Should().NotBeSameAs(inst2, "instance should have been cached as not the exact same sourceProvider");

		inst2_prime.Should().NotBeSameAs(inst1, "instance should have been cached as not the exact same sourceProvider");
		inst2_prime.Should().BeSameAs(inst2, "instance should have been cached using sourceProvider");
	}

	[TestMethod]
	public void When_CreateWithoutCT_Then_Cached()
	{
		Func<IAsyncEnumerable<Message<IImmutableList<object>>>> sourceProvider1 = () => default!;
		Func<IAsyncEnumerable<Message<IImmutableList<object>>>> sourceProvider2 = () => default!;

		var inst1 = ListFeed<object>.Create(sourceProvider1);
		var inst2 = ListFeed<object>.Create(sourceProvider2);
		var inst1_prime = ListFeed<object>.Create(sourceProvider1);
		var inst2_prime = ListFeed<object>.Create(sourceProvider2);

		inst1_prime.Should().BeSameAs(inst1, "instance should have been cached using sourceProvider");
		inst1_prime.Should().NotBeSameAs(inst2, "instance should have been cached as not the exact same sourceProvider");

		inst2_prime.Should().NotBeSameAs(inst1, "instance should have been cached as not the exact same sourceProvider");
		inst2_prime.Should().BeSameAs(inst2, "instance should have been cached using sourceProvider");
	}

	[TestMethod]
	public void When_AsyncOption_Then_Cached()
	{
		AsyncFunc<Option<IImmutableList<object>>> valueProvider1 = async ct => default!;
		AsyncFunc<Option<IImmutableList<object>>> valueProvider2 = async ct => default!;

		var inst1 = ListFeed<object>.Async(valueProvider1);
		var inst2 = ListFeed<object>.Async(valueProvider2);
		var inst1_prime = ListFeed<object>.Async(valueProvider1);
		var inst2_prime = ListFeed<object>.Async(valueProvider2);

		inst1_prime.Should().BeSameAs(inst1, "instance should have been cached using valueProvider");
		inst1_prime.Should().NotBeSameAs(inst2, "instance should have been cached as not the exact same valueProvider");

		inst2_prime.Should().NotBeSameAs(inst1, "instance should have been cached as not the exact same valueProvider");
		inst2_prime.Should().BeSameAs(inst2, "instance should have been cached using valueProvider");
	}

	[TestMethod]
	public void When_Async_Then_Cached()
	{
		AsyncFunc<IImmutableList<object>> valueProvider1 = async ct => ImmutableList<object>.Empty;
		AsyncFunc<IImmutableList<object>> valueProvider2 = async ct => ImmutableList<object>.Empty;

		var inst1 = ListFeed<object>.Async(valueProvider1);
		var inst2 = ListFeed<object>.Async(valueProvider2);
		var inst1_prime = ListFeed<object>.Async(valueProvider1);
		var inst2_prime = ListFeed<object>.Async(valueProvider2);

		inst1_prime.Should().BeSameAs(inst1, "instance should have been cached using valueProvider");
		inst1_prime.Should().NotBeSameAs(inst2, "instance should have been cached as not the exact same valueProvider");

		inst2_prime.Should().NotBeSameAs(inst1, "instance should have been cached as not the exact same valueProvider");
		inst2_prime.Should().BeSameAs(inst2, "instance should have been cached using valueProvider");
	}

	[TestMethod]
	public void When_AsyncOptionWithSignal_Then_Cached()
	{
		AsyncFunc<Option<IImmutableList<object>>> valueProvider1 = async ct => default!;
		AsyncFunc<Option<IImmutableList<object>>> valueProvider2 = async ct => default!;
		var signal1 = new Signal();
		var signal2 = new Signal();

		var inst1_1 = ListFeed<object>.Async(valueProvider1, signal1);
		var inst1_2 = ListFeed<object>.Async(valueProvider1, signal2);
		var inst2_1 = ListFeed<object>.Async(valueProvider2, signal1);
		var inst2_2 = ListFeed<object>.Async(valueProvider2, signal2);
		var inst1_1_prime = ListFeed<object>.Async(valueProvider1, signal1);
		var inst1_2_prime = ListFeed<object>.Async(valueProvider1, signal2);
		var inst2_1_prime = ListFeed<object>.Async(valueProvider2, signal1);
		var inst2_2_prime = ListFeed<object>.Async(valueProvider2, signal2);

		inst1_1_prime.Should().BeSameAs(inst1_1, "instance should have been cached using valueProvider and signal");
		inst1_1_prime.Should().NotBeSameAs(inst1_2, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst1_1_prime.Should().NotBeSameAs(inst2_1, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst1_1_prime.Should().NotBeSameAs(inst2_2, "instance should have been cached as not the exact same combination of valueProvider and signal");

		inst1_2_prime.Should().NotBeSameAs(inst1_1, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst1_2_prime.Should().BeSameAs(inst1_2, "instance should have been cached using valueProvider and signal");
		inst1_2_prime.Should().NotBeSameAs(inst2_1, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst1_2_prime.Should().NotBeSameAs(inst2_2, "instance should have been cached as not the exact same combination of valueProvider and signal");

		inst2_1_prime.Should().NotBeSameAs(inst1_1, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst2_1_prime.Should().NotBeSameAs(inst1_2, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst2_1_prime.Should().BeSameAs(inst2_1, "instance should have been cached using valueProvider and signal");
		inst2_1_prime.Should().NotBeSameAs(inst2_2, "instance should have been cached as not the exact same combination of valueProvider and signal");

		inst2_2_prime.Should().NotBeSameAs(inst1_1, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst2_2_prime.Should().NotBeSameAs(inst1_2, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst2_2_prime.Should().NotBeSameAs(inst2_1, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst2_2_prime.Should().BeSameAs(inst2_2, "instance should have been cached using valueProvider and signal");
	}

	[TestMethod]
	public void When_AsyncWithSignal_Then_Cached()
	{
		AsyncFunc<IImmutableList<object>> valueProvider1 = async ct => default!;
		AsyncFunc<IImmutableList<object>> valueProvider2 = async ct => default!;
		var signal1 = new Signal();
		var signal2 = new Signal();

		var inst1_1 = ListFeed<object>.Async(valueProvider1, signal1);
		var inst1_2 = ListFeed<object>.Async(valueProvider1, signal2);
		var inst2_1 = ListFeed<object>.Async(valueProvider2, signal1);
		var inst2_2 = ListFeed<object>.Async(valueProvider2, signal2);
		var inst1_1_prime = ListFeed<object>.Async(valueProvider1, signal1);
		var inst1_2_prime = ListFeed<object>.Async(valueProvider1, signal2);
		var inst2_1_prime = ListFeed<object>.Async(valueProvider2, signal1);
		var inst2_2_prime = ListFeed<object>.Async(valueProvider2, signal2);

		inst1_1_prime.Should().BeSameAs(inst1_1, "instance should have been cached using valueProvider and signal");
		inst1_1_prime.Should().NotBeSameAs(inst1_2, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst1_1_prime.Should().NotBeSameAs(inst2_1, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst1_1_prime.Should().NotBeSameAs(inst2_2, "instance should have been cached as not the exact same combination of valueProvider and signal");

		inst1_2_prime.Should().NotBeSameAs(inst1_1, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst1_2_prime.Should().BeSameAs(inst1_2, "instance should have been cached using valueProvider and signal");
		inst1_2_prime.Should().NotBeSameAs(inst2_1, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst1_2_prime.Should().NotBeSameAs(inst2_2, "instance should have been cached as not the exact same combination of valueProvider and signal");

		inst2_1_prime.Should().NotBeSameAs(inst1_1, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst2_1_prime.Should().NotBeSameAs(inst1_2, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst2_1_prime.Should().BeSameAs(inst2_1, "instance should have been cached using valueProvider and signal");
		inst2_1_prime.Should().NotBeSameAs(inst2_2, "instance should have been cached as not the exact same combination of valueProvider and signal");

		inst2_2_prime.Should().NotBeSameAs(inst1_1, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst2_2_prime.Should().NotBeSameAs(inst1_2, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst2_2_prime.Should().NotBeSameAs(inst2_1, "instance should have been cached as not the exact same combination of valueProvider and signal");
		inst2_2_prime.Should().BeSameAs(inst2_2, "instance should have been cached using valueProvider and signal");
	}

	[TestMethod]
	public void When_AsyncEnumerableOptions_Then_Cached()
	{
		Func<IAsyncEnumerable<Option<IImmutableList<object>>>> enumerableProvider1 = () => default!;
		Func<IAsyncEnumerable<Option<IImmutableList<object>>>> enumerableProvider2 = () => default!;

		var inst1 = ListFeed<object>.AsyncEnumerable(enumerableProvider1);
		var inst2 = ListFeed<object>.AsyncEnumerable(enumerableProvider2);
		var inst1_prime = ListFeed<object>.AsyncEnumerable(enumerableProvider1);
		var inst2_prime = ListFeed<object>.AsyncEnumerable(enumerableProvider2);

		inst1_prime.Should().BeSameAs(inst1, "instance should have been cached using enumerableProvider");
		inst1_prime.Should().NotBeSameAs(inst2, "instance should have been cached as not the exact same enumerableProvider");

		inst2_prime.Should().NotBeSameAs(inst1, "instance should have been cached as not the exact same enumerableProvider");
		inst2_prime.Should().BeSameAs(inst2, "instance should have been cached using enumerableProvider");
	}

	[TestMethod]
	public void When_AsyncEnumerable_Then_Cached()
	{
		Func<IAsyncEnumerable<IImmutableList<object>>> enumerableProvider1 = () => default!;
		Func<IAsyncEnumerable<IImmutableList<object>>> enumerableProvider2 = () => default!;

		var inst1 = ListFeed<object>.AsyncEnumerable(enumerableProvider1);
		var inst2 = ListFeed<object>.AsyncEnumerable(enumerableProvider2);
		var inst1_prime = ListFeed<object>.AsyncEnumerable(enumerableProvider1);
		var inst2_prime = ListFeed<object>.AsyncEnumerable(enumerableProvider2);

		inst1_prime.Should().BeSameAs(inst1, "instance should have been cached using enumerableProvider");
		inst1_prime.Should().NotBeSameAs(inst2, "instance should have been cached as not the exact same enumerableProvider");

		inst2_prime.Should().NotBeSameAs(inst1, "instance should have been cached as not the exact same enumerableProvider");
		inst2_prime.Should().BeSameAs(inst2, "instance should have been cached using enumerableProvider");
	}

	[TestMethod]
	public void When_PaginatedByCursorAsync_Then_Cached()
	{
		object firstPage1 = new();
		object firstPage2 = new();
		GetPage<object, object> getPage1 = async (_, __, ct) => default!;
		GetPage<object, object> getPage2 = async (_, __, ct) => default!;

		var inst1_1 = ListFeed<object>.PaginatedByCursorAsync(firstPage1, getPage1);
		var inst1_2 = ListFeed<object>.PaginatedByCursorAsync(firstPage1, getPage2);
		var inst2_1 = ListFeed<object>.PaginatedByCursorAsync(firstPage2, getPage1);
		var inst2_2 = ListFeed<object>.PaginatedByCursorAsync(firstPage2, getPage2);
		var inst1_1_prime = ListFeed<object>.PaginatedByCursorAsync(firstPage1, getPage1);
		var inst1_2_prime = ListFeed<object>.PaginatedByCursorAsync(firstPage1, getPage2);
		var inst2_1_prime = ListFeed<object>.PaginatedByCursorAsync(firstPage2, getPage1);
		var inst2_2_prime = ListFeed<object>.PaginatedByCursorAsync(firstPage2, getPage2);

		inst1_1_prime.Should().BeSameAs(inst1_1, "instance should have been cached using firstPage and getPage");
		inst1_1_prime.Should().NotBeSameAs(inst1_2, "instance should have been cached as not the exact same combination of firstPage and getPage");
		inst1_1_prime.Should().NotBeSameAs(inst2_1, "instance should have been cached as not the exact same combination of firstPage and getPage");
		inst1_1_prime.Should().NotBeSameAs(inst2_2, "instance should have been cached as not the exact same combination of firstPage and getPage");

		inst1_2_prime.Should().NotBeSameAs(inst1_1, "instance should have been cached as not the exact same combination of firstPage and getPage");
		inst1_2_prime.Should().BeSameAs(inst1_2, "instance should have been cached using firstPage and getPage");
		inst1_2_prime.Should().NotBeSameAs(inst2_1, "instance should have been cached as not the exact same combination of firstPage and getPage");
		inst1_2_prime.Should().NotBeSameAs(inst2_2, "instance should have been cached as not the exact same combination of firstPage and getPage");

		inst2_1_prime.Should().NotBeSameAs(inst1_1, "instance should have been cached as not the exact same combination of firstPage and getPage");
		inst2_1_prime.Should().NotBeSameAs(inst1_2, "instance should have been cached as not the exact same combination of firstPage and getPage");
		inst2_1_prime.Should().BeSameAs(inst2_1, "instance should have been cached using firstPage and getPage");
		inst2_1_prime.Should().NotBeSameAs(inst2_2, "instance should have been cached as not the exact same combination of firstPage and getPage");

		inst2_2_prime.Should().NotBeSameAs(inst1_1, "instance should have been cached as not the exact same combination of firstPage and getPage");
		inst2_2_prime.Should().NotBeSameAs(inst1_2, "instance should have been cached as not the exact same combination of firstPage and getPage");
		inst2_2_prime.Should().NotBeSameAs(inst2_1, "instance should have been cached as not the exact same combination of firstPage and getPage");
		inst2_2_prime.Should().BeSameAs(inst2_2, "instance should have been cached using firstPage and getPage");
	}

	[TestMethod]
	public void When_PaginatedAsync_Then_Cached()
	{
		AsyncFunc<PageRequest, IImmutableList<object>> getPage1 = async (_, ct) => default!;
		AsyncFunc<PageRequest, IImmutableList<object>> getPage2 = async (_, ct) => default!;

		var inst1 = ListFeed<object>.PaginatedAsync(getPage1);
		var inst2 = ListFeed<object>.PaginatedAsync(getPage2);
		var inst1_prime = ListFeed<object>.PaginatedAsync(getPage1);
		var inst2_prime = ListFeed<object>.PaginatedAsync(getPage2);

		inst1_prime.Should().BeSameAs(inst1, "instance should have been cached using getPage");
		inst1_prime.Should().NotBeSameAs(inst2, "instance should have been cached as not the exact same getPage");

		inst2_prime.Should().NotBeSameAs(inst1, "instance should have been cached as not the exact same getPage");
		inst2_prime.Should().BeSameAs(inst2, "instance should have been cached using getPage");
	}
}
