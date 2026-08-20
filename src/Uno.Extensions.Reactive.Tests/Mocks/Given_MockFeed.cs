using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Mocks;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Mocks;

[TestClass]
public class Given_MockFeed : FeedTests
{
	// De-risk test for spec 012 §10.2: Message<T>.Initial already carries an undefined data axis,
	// so setting Option.Undefined through a MessageBuilder registers no change. This is the reason
	// MockFeed.Undefined<T>() walks through None before pinning Undefined. If this test ever fails,
	// the framework started flagging same-value sets — simplify MockMessageSource.Undefined to a
	// single message rather than deleting this test.
	[TestMethod]
	public void When_DataSetToUndefinedOnInitial_Then_BuilderFlagsNoChange()
	{
		Message<int> message = Message<int>.Initial.With().Data(Option<int>.Undefined());

		message.Current.Data.Type.Should().Be(OptionType.Undefined);
		message.Changes.Contains(MessageAxis.Data).Should().BeFalse();
	}

	[TestMethod]
	public async Task When_Undefined_Then_DataAxisIsUndefined()
	{
		var result = MockFeed.Undefined<int>().Record();

		await result.WaitForEnd(CT);

		// Walks through None so the final Undefined registers a genuine data change (cf. spec 012 §10.2).
		result.Should().Be(r => r
			.Message(Changed.Data, Data.None, Error.No, Progress.Final)
			.Message(Changed.Data, Data.Undefined, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Undefined_ThroughState_Then_ConvergesToUndefined()
	{
		// The state pipeline (MessageManager and the replay to late subscribers) recomputes change
		// sets by value comparison — this is the path every generated CreateMock member goes through
		// (ctx.GetOrCreateState(mockFeed)). Whether the subscriber attaches before the mock messages
		// are processed (and observes None → Undefined transitions) or after (and observes a replayed
		// Undefined), the data must converge to Undefined.
		var state = Context.SourceContext.GetOrCreateState(MockFeed.Undefined<int>());

		var data = await state.DataSet(ct: CT).Where(d => d.Type == OptionType.Undefined).FirstAsync(CT);

		data.Type.Should().Be(OptionType.Undefined);
	}

	[TestMethod]
	public void When_Undefined_Then_EachCallProducesADistinctFeed()
	{
		// Two unconfigured members of the same value type on one mocked view model must not
		// share a backing state (states are keyed on the feed instance per context).
		MockFeed.Undefined<int>().Should().NotBeSameAs(MockFeed.Undefined<int>());
	}

	[TestMethod]
	public async Task When_Loading_Then_MessageIsTransient_And_FeedCompletes()
	{
		var result = MockFeed.Loading<int>().Record();

		// WaitForEnd throws on timeout, proving the feed completes even though its last message is transient.
		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient)
		);
	}

	[TestMethod]
	public async Task When_Empty_Then_DataIsNone()
	{
		var result = MockFeed.Empty<int>().Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, Data.None, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Value_Then_DataIsSome()
	{
		var result = MockFeed.Value(42).Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, 42, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Error_Then_ErrorAxisIsSet_And_DataUnset()
	{
		var error = new TestException();
		var result = MockFeed.Error<int>(error).Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Error, Data.Undefined, error, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Refreshing_Then_DataIsSome_And_IsTransient()
	{
		var result = MockFeed.Refreshing(42).Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data & Changed.Progress, 42, Error.No, Progress.Transient)
		);
	}

	[TestMethod]
	public async Task When_Message_Then_ConfiguredStateApplied()
	{
		var error = new TestException();
		var result = MockFeed.Message<int>(m => m.Data(42).Error(error).IsTransient(true)).Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data & Changed.Error & Changed.Progress, 42, error, Progress.Transient)
		);
	}

	[TestMethod]
	public async Task When_Script_Then_MessagesEmittedInOrder()
	{
		var error = new TestException();
		var result = MockFeed.Script<int>(
				(TimeSpan.Zero, m => m.Data(41)),
				(TimeSpan.Zero, m => m.Data(42)),
				(TimeSpan.Zero, m => m.Error(error)))
			.Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Changed.Data, 41, Error.No, Progress.Final)
			.Message(Changed.Data, 42, Error.No, Progress.Final)
			.Message(Changed.Error, 42, error, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Script_Cancelled_Then_EnumerationStops()
	{
		var sut = MockFeed.Script<int>(
			(TimeSpan.Zero, m => m.Data(1)),
			(TimeSpan.FromHours(1), m => m.Data(2)));

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CT);
		var sawSecondStep = false;

		// Completing at all (instead of waiting out the one-hour step) proves the token is honored.
		await foreach (var message in sut.GetSource(Context.SourceContext, cts.Token))
		{
			sawSecondStep |= message.Current.Data.SomeOrDefault() == 2;
			cts.Cancel();
		}

		sawSecondStep.Should().BeFalse();
	}

	[TestMethod]
	public async Task When_Subscribed_Twice_Then_SameStateReplayed()
	{
		var sut = MockFeed.Value(42);

		var first = sut.Record();
		await first.WaitForEnd(CT);

		var second = sut.Record();
		await second.WaitForEnd(CT);

		first.Should().Be(r => r.Message(Changed.Data, 42, Error.No, Progress.Final));
		second.Should().Be(r => r.Message(Changed.Data, 42, Error.No, Progress.Final));
	}
}
