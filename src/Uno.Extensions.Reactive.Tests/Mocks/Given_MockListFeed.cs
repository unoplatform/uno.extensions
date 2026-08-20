using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Mocks;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Mocks;

[TestClass]
public class Given_MockListFeed : FeedTests
{
	[TestMethod]
	public async Task When_Undefined_Then_DataAxisIsUndefined()
	{
		var result = MockListFeed.Undefined<int>().Record();

		await result.WaitForEnd(CT);

		// Walks through None so the final Undefined registers a genuine data change (cf. spec 012 §10.2).
		result.Should().Be(r => r
			.Message(Changed.Data, Data.None, Error.No, Progress.Final)
			.Message(Changed.Data, Data.Undefined, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Loading_Then_MessageIsTransient_And_FeedCompletes()
	{
		var result = MockListFeed.Loading<int>().Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient)
		);
	}

	[TestMethod]
	public async Task When_Empty_Then_DataIsNone()
	{
		var result = MockListFeed.Empty<int>().Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, Data.None, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_EmptyList_Then_DataIsSomeEmptyList()
	{
		var result = MockListFeed.EmptyList<int>().Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, Data.Some, Error.No, Progress.Final)
		);
		result.Single().Current.Data.SomeOrDefault()!.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_Value_Then_DataIsSome()
	{
		var result = MockListFeed.Value(1, 2, 3).Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, Items.Some(1, 2, 3), Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_ValueWithoutItems_Then_DataIsNone()
	{
		// Like real list feeds, an empty list is normalized to None; EmptyList is the explicit opt-out.
		var result = MockListFeed.Value<int>().Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, Data.None, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Error_Then_ErrorAxisIsSet_And_DataUnset()
	{
		var error = new TestException();
		var result = MockListFeed.Error<int>(error).Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Error, Data.Undefined, error, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Refreshing_Then_DataIsSome_And_IsTransient()
	{
		var result = MockListFeed.Refreshing(1, 2, 3).Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data & Changed.Progress, Items.Some(1, 2, 3), Error.No, Progress.Transient)
		);
	}

	[TestMethod]
	public async Task When_RefreshingWithoutItems_Then_DataIsNone_And_IsTransient()
	{
		var result = MockListFeed.Refreshing<int>().Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data & Changed.Progress, Data.None, Error.No, Progress.Transient)
		);
	}
}
