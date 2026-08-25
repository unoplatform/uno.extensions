using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.Extensions;
using Uno.Extensions.Reactive;

namespace Uno.HotTesting.Reactive;

/// <summary>
/// Creates finite <see cref="IFeed{T}"/> instances pinned to common MVUX message states.
/// </summary>
/// <remarks>
/// Each subscription receives the configured state and then completes. This keeps loading
/// and refreshing previews pinned without leaving background work running.
/// </remarks>
public static class FeedMock
{
	/// <summary>Creates a feed pinned before its first value.</summary>
	/// <typeparam name="T">The feed value type.</typeparam>
	/// <returns>A feed whose data axis is undefined.</returns>
	public static IFeed<T> Undefined<T>()
	{
		// Feed.Create caches by delegate identity. Capturing a fresh identity keeps two mock
		// properties of the same type independent when a future factory consumes this API.
		var identity = new object();
		return Feed.Create<T>(ct => FeedMockMessageSource.Undefined<T>(identity, ct));
	}

	/// <summary>Creates a feed pinned in an indefinite loading state.</summary>
	/// <typeparam name="T">The feed value type.</typeparam>
	/// <returns>A transient feed which completes after its loading message.</returns>
	public static IFeed<T> Loading<T>()
		=> Message<T>(message => message.IsTransient(true));

	/// <summary>Creates a feed with no value.</summary>
	/// <typeparam name="T">The feed value type.</typeparam>
	/// <returns>A feed whose data axis is <see cref="Option{T}.None"/>.</returns>
	public static IFeed<T> Empty<T>()
		=> Message<T>(message => message.Data(Option<T>.None()));

	/// <summary>Creates a feed pinned to a value.</summary>
	/// <typeparam name="T">The feed value type.</typeparam>
	/// <param name="value">The value to expose.</param>
	/// <returns>A feed whose data axis contains <paramref name="value"/>.</returns>
	public static IFeed<T> Value<T>(T value)
		=> Message<T>(message => message.Data(value));

	/// <summary>Creates a feed pinned to an error.</summary>
	/// <typeparam name="T">The feed value type.</typeparam>
	/// <param name="error">The error to expose.</param>
	/// <returns>A feed whose error axis contains <paramref name="error"/>.</returns>
	public static IFeed<T> Error<T>(Exception error)
	{
		if (error is null)
		{
			throw new ArgumentNullException(nameof(error));
		}

		return Message<T>(message => message.Error(error));
	}

	/// <summary>Creates a feed pinned to stale data while a refresh is in progress.</summary>
	/// <typeparam name="T">The feed value type.</typeparam>
	/// <param name="staleValue">The stale value to expose.</param>
	/// <returns>A transient feed with data.</returns>
	/// <remarks>
	/// This models the public message axes used by views: data is present and progress is
	/// transient. The internal refresh axis can only be raised by a refreshable source feed.
	/// </remarks>
	public static IFeed<T> Refreshing<T>(T staleValue)
		=> Message<T>(message => message.Data(staleValue).IsTransient(true));

	/// <summary>Creates a feed pinned to an arbitrary message.</summary>
	/// <typeparam name="T">The feed value type.</typeparam>
	/// <param name="configure">Configures the message axes.</param>
	/// <returns>A feed which emits the configured message and completes.</returns>
	public static IFeed<T> Message<T>(Action<MessageBuilder<T>> configure)
	{
		if (configure is null)
		{
			throw new ArgumentNullException(nameof(configure));
		}

		return Feed.Create<T>(ct => FeedMockMessageSource.Single(configure, ct));
	}
}

internal static class FeedMockMessageSource
{
	public static async IAsyncEnumerable<Message<T>> Single<T>(
		Action<MessageBuilder<T>> configure,
		[EnumeratorCancellation] CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		var builder = Message<T>.Initial.With();
		configure(builder);
		yield return builder.Build();
	}

	public static async IAsyncEnumerable<Message<T>> Undefined<T>(
		object identity,
		[EnumeratorCancellation] CancellationToken ct)
	{
		GC.KeepAlive(identity);
		ct.ThrowIfCancellationRequested();

		// Initial already has Undefined data. Walking through None makes the final
		// Undefined a real data-axis change which survives MessageManager replay.
		var none = Message<T>.Initial.With().Data(Option<T>.None()).Build();
		yield return none;
		ct.ThrowIfCancellationRequested();
		yield return none.With().Data(Option<T>.Undefined());
	}
}
