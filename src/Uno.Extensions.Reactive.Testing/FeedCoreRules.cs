using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Uno.Extensions.Reactive.Testing;

/// <summary>
/// Helper class to ensure that a custom implementation of feed follows the core rules of the feeds engine.
/// </summary>
public class FeedCoreRules
{
	public static FeedUnderTest<T> WhenFeed<T>(IFeed<T> sut, params ISignal<IMessage>[] dependencies)
		=> new(dependencies.ToImmutableArray(), sut);

	public static SutBuilder<TFeed> Using<TFeed>(TFeed dependency)
		where TFeed : ISignal<IMessage>
		=> new(dependency);

	public record struct SutBuilder<TFeed>(TFeed Dependency)
		where TFeed : ISignal<IMessage>
	{
		public FeedUnderTest<T> WhenFeed<T>(Func<TFeed, IFeed<T>> sutFactory) => new(ImmutableArray.Create<ISignal<IMessage>>(Dependency), sutFactory(Dependency));
		public ListFeedUnderTest<T> WhenListFeed<T>(Func<TFeed, IListFeed<T>> sutFactory) => new(ImmutableArray.Create<ISignal<IMessage>>(Dependency), sutFactory(Dependency));
	}

	public record struct FeedUnderTest<T>(ImmutableArray<ISignal<IMessage>> Dependencies, IFeed<T> Value)
	{
		public async Task Then(AsyncAction<FeedUnderTest<T>> assert, CancellationToken ct) => await assert(this, ct);

		public Task Then_CompilesToCoreRules(CancellationToken ct) => Then(When_Feed_Then_CompliesToCoreRules, ct);
	}

	public record struct ListFeedUnderTest<T>(ImmutableArray<ISignal<IMessage>> Dependencies, IListFeed<T> Value)
	{
		public async Task Then(AsyncAction<ListFeedUnderTest<T>> assert, CancellationToken ct) => await assert(this, ct);

		public Task Then_CompilesToCoreRules(CancellationToken ct) => Then(When_ListFeed_Then_CompliesToCoreRules, ct);
	}

	/// <summary>
	/// Validate that a feed compiles to the core rules.
	/// </summary>
	/// <param name="sut">The feed under test.</param>
	/// <param name="ct">Cancellation token to cancel async operation.</param>
	/// <returns>Async validation operation.</returns>
	public static async ValueTask When_Feed_Then_CompliesToCoreRules<TValue>(FeedUnderTest<TValue> sut, CancellationToken ct)
	{
		await When_Subscribe_Then_GetFirstMessageFastEnough(sut.Value, ct);

		using var scope = new AssertionScope("initial message content");
		await When_Subscribe_Then_FirstMessageHasDataOrIsTransient(sut.Value, ct);
		await When_SubscribeMultipleTimeWithSameContext_Then_GetSameResult(sut.Dependencies, sut.Value, ct);
	}

	/// <summary>
	/// Validate that a feed compiles to the core rules.
	/// </summary>
	/// <param name="sut">The feed under test.</param>
	/// <param name="ct">Cancellation token to cancel async operation.</param>
	/// <returns>Async validation operation.</returns>
	public static async ValueTask When_ListFeed_Then_CompliesToCoreRules<TValue>(ListFeedUnderTest<TValue> sut, CancellationToken ct)
	{
		await When_Subscribe_Then_GetFirstMessageFastEnough(sut.Value, ct);

		using var scope = new AssertionScope("initial message content");
		await When_Subscribe_Then_FirstMessageHasDataOrIsTransient(sut.Value, ct);
		await When_SubscribeMultipleTimeWithSameContext_Then_GetSameResult(sut.Dependencies, sut.Value, ct);
	}


	internal static async Task When_Subscribe_Then_GetFirstMessageFastEnough<TFeed>(TFeed feed, CancellationToken ct)
		where TFeed : ISignal<IMessage>
	{
		using var scope = new AssertionScope(nameof(When_Subscribe_Then_GetFirstMessageFastEnough));
		using var ctx = new FeedTestContext();

		var timeout = Task.Delay(300, ct);
		var firstMessage = feed.GetSource(ctx, ct).FirstAsync(ct).AsTask();

		await Task.WhenAny(timeout, firstMessage);

		firstMessage.Status.Should().Be(
			TaskStatus.RanToCompletion,
			"feeds must provide an initial value in a reasonable amount of time (i.e. few Ticks!) in order to properly initialize the dependency graph and the UI."
			+ "This initial message could either have the None or Some data, or could be just an Undefined data flagged as transient.");
	}

	internal static async Task When_Subscribe_Then_FirstMessageHasDataOrIsTransient<TFeed>(TFeed feed, CancellationToken ct)
		where TFeed : ISignal<IMessage>
	{
		using var scope = new AssertionScope(nameof(When_Subscribe_Then_FirstMessageHasDataOrIsTransient));
		using var ctx = new FeedTestContext();

		var firstMessage = await feed.GetSource(ctx, ct).FirstAsync(ct);

		firstMessage.Current[MessageAxis.Data].IsSet.Should().BeTrue("data axis always be set (use Option.Undefined if no info available yet)");
		if (firstMessage.Current.Data.IsUndefined())
		{
			firstMessage.Current.IsTransient.Should().BeTrue(
				"if the first message does not contains any data, then it has to be transient (and an non transient data -Some or None- has to be published as soon as possible");
		}
	}

	private static async Task When_SubscribeMultipleTimeWithSameContext_Then_GetSameResult<TFeed>(
		ImmutableArray<ISignal<IMessage>> dependencies,
		TFeed feed,
		CancellationToken ct)
		where TFeed : ISignal<IMessage>
	{
		using var scope = new AssertionScope(nameof(When_SubscribeMultipleTimeWithSameContext_Then_GetSameResult));
		using var ctx = new FeedTestContext();

		using (new AssertionScope("initial state"))
		{
			foreach (var dependency in dependencies)
			{
				ctx.SourceContext.States.HasSubscription(dependency).Should().BeFalse("dependencies should have been used yet to run test properly");
			}
		}

		await feed.GetSource(ctx, ct).FirstAsync(ct);

		foreach (var dependency in dependencies)
		{
			ctx.SourceContext.States.HasSubscription(dependency).Should().BeTrue("all dependencies should have been consumed using Context.GetOrCreateSource(dependency).");
		}
	}
}
