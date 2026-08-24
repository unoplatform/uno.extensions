using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Testing;

namespace Uno.HotTesting.Reactive.Tests;

[TestClass]
public class Given_FeedMock : FeedTests
{
	[TestMethod]
	public async Task When_Undefined_Then_DataAxisIsUndefined_AndFeedCompletes()
	{
		var result = FeedMock.Undefined<int>().Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, Data.None, Error.No, Progress.Final)
			.Message(Changed.Data, Data.Undefined, Error.No, Progress.Final));
	}

	[TestMethod]
	public void When_UndefinedTwice_Then_FeedsAreIndependent()
		=> FeedMock.Undefined<int>().Should().NotBeSameAs(FeedMock.Undefined<int>());

	[TestMethod]
	public async Task When_Loading_Then_ProgressIsTransient_AndFeedCompletes()
	{
		var result = FeedMock.Loading<int>().Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient));
	}

	[TestMethod]
	public async Task When_Empty_Then_DataIsNone()
	{
		var result = FeedMock.Empty<int>().Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, Data.None, Error.No, Progress.Final));
	}

	[TestMethod]
	public async Task When_Value_Then_DataIsSome()
	{
		var result = FeedMock.Value(42).Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, 42, Error.No, Progress.Final));
	}

	[TestMethod]
	public async Task When_Error_Then_ErrorAxisIsSet()
	{
		var error = new TestException();
		var result = FeedMock.Error<int>(error).Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Error, Data.Undefined, error, Progress.Final));
	}

	[TestMethod]
	public async Task When_Refreshing_Then_DataAndTransientProgressAreSet()
	{
		var result = FeedMock.Refreshing(42).Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data & Changed.Progress, 42, Error.No, Progress.Transient));
	}

	[TestMethod]
	public async Task When_Message_Then_AllConfiguredAxesArePreserved()
	{
		var error = new TestException();
		var result = FeedMock.Message<int>(message => message
			.Data(42)
			.Error(error)
			.IsTransient(true))
			.Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(
				Changed.Data & Changed.Error & Changed.Progress,
				42,
				error,
				Progress.Transient));
	}

	[TestMethod]
	public async Task When_SubscribedAgain_Then_PinnedStateIsReplayedAndCompletes()
	{
		var feed = FeedMock.Loading<int>();
		var first = feed.Record();
		await first.WaitForEnd(CT);

		var second = feed.Record();
		await second.WaitForEnd(CT);

		first.Should().Be(r => r
			.Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient));
		second.Should().Be(r => r
			.Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient));
	}

	[TestMethod]
	public async Task When_UndefinedPassesThroughState_Then_ReplayConvergesToUndefined()
	{
		var state = Context.SourceContext.GetOrCreateState(FeedMock.Undefined<int>());

		var data = await state.DataSet(ct: CT)
			.Where(value => value.Type == OptionType.Undefined)
			.FirstAsync(CT);

		data.Type.Should().Be(OptionType.Undefined);
	}
}
