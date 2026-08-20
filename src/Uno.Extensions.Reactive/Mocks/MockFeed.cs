using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive.Mocks;

/// <summary>
/// Provides factories of <see cref="IFeed{T}"/> pinned to a fixed message state, for previews and UI tests of visual states.
/// </summary>
/// <remarks>
/// A mocked feed emits its configured state once and then completes, which pins the subscriber
/// (e.g. a <c>FeedView</c>) in that state without any pending work left behind.
/// </remarks>
public static class MockFeed
{
	/// <summary>
	/// Gets a feed pinned in the undefined state, i.e. as if the source had not produced any data yet.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <returns>A feed pinned in the undefined state.</returns>
	// Not Feed.Create: it caches per delegate and the method-group conversion is compiler-cached, which
	// would make every Undefined<T>() of a given T the same feed instance — and two unconfigured members
	// of the same type on one mocked view model would then share a single backing state.
	public static IFeed<T> Undefined<T>()
		=> new CustomFeed<T>(MockMessageSource.Undefined<T>);

	/// <summary>
	/// Gets a feed pinned in the loading state, indefinitely.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <returns>A feed pinned in the loading state.</returns>
	public static IFeed<T> Loading<T>()
		=> Message<T>(m => m.IsTransient(true));

	/// <summary>
	/// Gets a feed pinned in the empty state (<see cref="Option{T}.None"/>), i.e. the "no result" state.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <returns>A feed pinned in the empty state.</returns>
	public static IFeed<T> Empty<T>()
		=> Message<T>(m => m.Data(Option<T>.None()));

	/// <summary>
	/// Gets a feed pinned with the given value.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="value">The value of the feed.</param>
	/// <returns>A feed pinned with the given value.</returns>
	public static IFeed<T> Value<T>(T value)
		=> Message<T>(m => m.Data(value));

	/// <summary>
	/// Gets a feed pinned in the error state.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="error">The error of the feed.</param>
	/// <returns>A feed pinned in the error state.</returns>
	public static IFeed<T> Error<T>(Exception error)
	{
		error = error ?? throw new ArgumentNullException(nameof(error));

		return Message<T>(m => m.Error(error));
	}

	/// <summary>
	/// Gets a feed pinned with stale data while refreshing, indefinitely.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="staleValue">The stale value exposed while refreshing.</param>
	/// <returns>A feed pinned in the refreshing state.</returns>
	/// <remarks>
	/// This is visually faithful (data present with an indeterminate progress) but does not set
	/// the internal refresh axis, which cannot be produced outside of a refreshable source feed.
	/// </remarks>
	public static IFeed<T> Refreshing<T>(T staleValue)
		=> Message<T>(m => m.Data(staleValue).IsTransient(true));

	/// <summary>
	/// Gets a feed pinned to an arbitrary message.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="configure">The delegate to configure the single message produced by the feed.</param>
	/// <returns>A feed pinned to the configured message.</returns>
	public static IFeed<T> Message<T>(Action<MessageBuilder<T>> configure)
	{
		configure = configure ?? throw new ArgumentNullException(nameof(configure));

		return Feed.Create<T>(ct => MockMessageSource.Single(configure, ct));
	}

	/// <summary>
	/// Gets a feed that walks a scripted sequence of states, then completes.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="steps">The sequence of steps, each applied to the previous message after the given delay.</param>
	/// <returns>A feed that walks the scripted sequence of states.</returns>
	public static IFeed<T> Script<T>(params (TimeSpan after, Action<MessageBuilder<T>> step)[] steps)
	{
		steps = steps ?? throw new ArgumentNullException(nameof(steps));

		return Feed.Create<T>(ct => MockMessageSource.Script(steps, ct));
	}
}

/// <summary>
/// The message sequences behind <see cref="MockFeed"/> (kept out of that class so its
/// <c>Message</c> factory method does not shadow the <see cref="Message{T}"/> type).
/// </summary>
internal static class MockMessageSource
{
	public static async IAsyncEnumerable<Message<T>> Single<T>(Action<MessageBuilder<T>> configure, [EnumeratorCancellation] CancellationToken ct)
	{
		var builder = Message<T>.Initial.With();
		configure(builder);
		yield return builder.Build();
	}

	public static async IAsyncEnumerable<Message<T>> Undefined<T>([EnumeratorCancellation] CancellationToken ct)
	{
		// Message<T>.Initial already carries an undefined data axis, so a single message setting
		// Option.Undefined registers no change (spec 012 §10.2) — and even an explicitly-flagged change
		// is recomputed away by the state pipeline (MessageManager compares values), which is the path
		// every generated CreateMock member goes through. Walk through None so the final Undefined is a
		// genuine data change that survives every subscription path.
		var none = Message<T>.Initial.With().Data(Option<T>.None()).Build();

		yield return none;
		yield return none.With().Data(Option<T>.Undefined());
	}

	public static async IAsyncEnumerable<Message<T>> Script<T>((TimeSpan after, Action<MessageBuilder<T>> step)[] steps, [EnumeratorCancellation] CancellationToken ct)
	{
		var current = Message<T>.Initial;
		foreach (var (after, step) in steps)
		{
			if (after > TimeSpan.Zero)
			{
				await Task.Delay(after, ct).ConfigureAwait(false);
			}

			var builder = current.With();
			step(builder);
			yield return current = builder.Build();
		}
	}
}
