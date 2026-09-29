using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Testing;

namespace Uno.HotTesting.Reactive.Tests;

[TestClass]
public class Given_ListFeedMock : FeedTests
{
	[TestMethod]
	public async Task When_Undefined_Then_DataAxisIsUndefined_AndFeedCompletes()
	{
		var result = ListFeedMock.Undefined<int>().Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, Data.None, Error.No, Progress.Final)
			.Message(Changed.Data, Data.Undefined, Error.No, Progress.Final));
	}

	[TestMethod]
	public async Task When_Loading_Then_ProgressIsTransient_AndFeedCompletes()
	{
		var result = ListFeedMock.Loading<int>().Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient));
	}

	[TestMethod]
	public async Task When_Empty_Then_DataIsSomeEmptyList()
	{
		var result = ListFeedMock.Empty<int>().Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, Data.Some, Error.No, Progress.Final));
		result.Single().Current.Data.SomeOrDefault().Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_ValueHasItems_Then_DataContainsItems()
	{
		var result = ListFeedMock.Value(1, 2, 3).Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, Items.Some(1, 2, 3), Error.No, Progress.Final));
	}

	[TestMethod]
	public async Task When_ValueHasNoItems_Then_DataRemainsSomeEmptyList()
	{
		var result = ListFeedMock.Value<int>().Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, Data.Some, Error.No, Progress.Final));
		result.Single().Current.Data.SomeOrDefault().Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_Error_Then_ErrorAxisIsSet()
	{
		var error = new TestException();
		var result = ListFeedMock.Error<int>(error).Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Error, Data.Undefined, error, Progress.Final));
	}

	[TestMethod]
	public async Task When_RefreshingWithItems_Then_DataAndTransientProgressAreSet()
	{
		var result = ListFeedMock.Refreshing(1, 2, 3).Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(
				Changed.Data & Changed.Progress,
				Items.Some(1, 2, 3),
				Error.No,
				Progress.Transient));
	}

	[TestMethod]
	public async Task When_RefreshingWithoutItems_Then_DataRemainsSomeEmptyList()
	{
		var result = ListFeedMock.Refreshing<int>().Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(
				Changed.Data & Changed.Progress,
				Data.Some,
				Error.No,
				Progress.Transient));
		result.Single().Current.Data.SomeOrDefault().Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_MessageSetsNone_Then_ExplicitAxisIsPreserved()
	{
		var result = ListFeedMock.Message<int>(message =>
			message.Data(Option<IImmutableList<int>>.None()))
			.Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, Data.None, Error.No, Progress.Final));
	}
}
