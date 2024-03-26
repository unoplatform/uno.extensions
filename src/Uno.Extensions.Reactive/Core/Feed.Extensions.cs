using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

partial class Feed
{
	/// <summary>
	/// Get an awaiter to asynchronously get the next data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="feed">The feed to get data from.</param>
	/// <returns>An awaiter to asynchronously get the next data produced by the feed.</returns>
	public static ValueTaskAwaiter<T?> GetAwaiter<T>(this IFeed<T> feed)
		where T : notnull
		=> feed.Value(SourceContext.Current.Token).GetAwaiter();

	/// <summary>
	/// Get an awaiter to asynchronously get the next data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="feed">The feed to get data from.</param>
	/// <returns>An awaiter to asynchronously get the next data produced by the feed.</returns>
	public static ValueTaskAwaiter<T?> GetAwaiter<T>(this IFeed<T?> feed)
		where T : struct
		=> feed.Value(SourceContext.Current.Token).GetAwaiter();

	/// <summary>
	/// Asynchronously get the next data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="feed">The feed to get data from.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next data produced by the feed.</returns>
	public static async ValueTask<T?> Value<T>(this IFeed<T> feed, CancellationToken ct)
		where T : notnull
		=> await FeedDependency.TryGetCurrentMessage(feed).ConfigureAwait(false) switch
		{
			{ } message => message.Current.EnsureNoError().Data.SomeOrDefault(),
			null => await feed.Values(AsyncFeedValue.Default, ct).FirstOrDefaultAsync(ct).ConfigureAwait(false)
		};

	/// <summary>
	/// Asynchronously get the next data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="feed">The feed to get data from.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next data produced by the feed.</returns>
	public static async ValueTask<T?> Value<T>(this IFeed<T?> feed, CancellationToken ct)
		where T : struct
		=> await FeedDependency.TryGetCurrentMessage(feed).ConfigureAwait(false) switch
		{
			{ } message => message.Current.EnsureNoError().Data.SomeOrDefault(),
			null => await feed.Values(AsyncFeedValue.Default, ct).FirstOrDefaultAsync(ct).ConfigureAwait(false)
		};

	/// <summary>
	/// Asynchronously get the next data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="feed">The feed to get data from.</param>
	/// <param name="kind">Specify which data can be returned or not.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next acceptable data produced by the feed.</returns>
	public static async ValueTask<T?> Value<T>(this IFeed<T> feed, AsyncFeedValue kind, CancellationToken ct)
		where T : notnull
		=> await FeedDependency.TryGetCurrentMessage(feed).ConfigureAwait(false) switch
		{
			not null when kind is not AsyncFeedValue.Default => throw new NotSupportedException($"Only kind AsyncFeedValue.Default is currently supported by the dynamic feed (requested: {kind})."),
			{ } message => message.Current.EnsureNoError().Data.SomeOrDefault(),
			null => await feed.Values(kind, ct).FirstOrDefaultAsync(ct).ConfigureAwait(false)
		};

	/// <summary>
	/// Asynchronously get the next data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="feed">The feed to get data from.</param>
	/// <param name="kind">Specify which data can be returned or not.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next acceptable data produced by the feed.</returns>
	public static async ValueTask<T?> Value<T>(this IFeed<T?> feed, AsyncFeedValue kind, CancellationToken ct)
		where T : struct
		=> await FeedDependency.TryGetCurrentMessage(feed).ConfigureAwait(false) switch
		{
			not null when kind is not AsyncFeedValue.Default => throw new NotSupportedException($"Only kind AsyncFeedValue.Default is currently supported by the dynamic feed (requested: {kind})."),
			{ } message => message.Current.EnsureNoError().Data.SomeOrDefault(),
			null => await feed.Values(kind, ct).FirstOrDefaultAsync(ct).ConfigureAwait(false)
		};

	/// <summary>
	/// Gets an asynchronous enumerable sequence of all data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="feed">The feed to get data from.</param>
	/// <param name="kind">Specify which data can be returned or not.</param>
	/// <param name="ct">A cancellation to cancel the async enumeration.</param>
	/// <returns>An async enumeration sequence of all acceptable data produced by a feed.</returns>
	public static IAsyncEnumerable<T?> Values<T>(this IFeed<T> feed, AsyncFeedValue kind = AsyncFeedValue.AllowError, CancellationToken ct = default)
		where T : notnull
		=> feed.Options(kind, ct).Select(opt => opt.SomeOrDefault());

	/// <summary>
	/// Gets an asynchronous enumerable sequence of all data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="feed">The feed to get data from.</param>
	/// <param name="kind">Specify which data can be returned or not.</param>
	/// <param name="ct">A cancellation to cancel the async enumeration.</param>
	/// <returns>An async enumeration sequence of all acceptable data produced by a feed.</returns>
	public static IAsyncEnumerable<T?> Values<T>(this IFeed<T?> feed, AsyncFeedValue kind = AsyncFeedValue.AllowError, CancellationToken ct = default)
		where T : struct
		=> feed.Options(kind, ct).Select(opt => opt.SomeOrDefault());

	/// <summary>
	/// Asynchronously get the next data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="feed">The feed to get data from.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next data produced by the feed.</returns>
	public static async ValueTask<Option<T>> Option<T>(this IFeed<T> feed, CancellationToken ct)
		=> await FeedDependency.TryGetCurrentMessage(feed).ConfigureAwait(false) switch
		{
			{ } message => message.Current.EnsureNoError().Data,
			null => await feed.Options(AsyncFeedValue.Default, ct).FirstOrDefaultAsync(Uno.Extensions.Option<T>.Undefined(), ct).ConfigureAwait(false)
		};

	/// <summary>
	/// Asynchronously get the next data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="feed">The feed to get data from.</param>
	/// <param name="kind">Specify which data can be returned or not.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next acceptable data produced by the feed.</returns>
	public static async ValueTask<Option<T>> Option<T>(this IFeed<T> feed, AsyncFeedValue kind, CancellationToken ct)
		=> await FeedDependency.TryGetCurrentMessage(feed).ConfigureAwait(false) switch
		{
			not null when kind is not AsyncFeedValue.Default => throw new NotSupportedException($"Only kind AsyncFeedValue.Default is currently supported by the dynamic feed (requested: {kind})."),
			{ } message => message.Current.EnsureNoError().Data,
			null => await feed.Options(kind, ct).FirstOrDefaultAsync(Uno.Extensions.Option<T>.Undefined(), ct).ConfigureAwait(false)
		};

	/// <summary>
	/// Gets an asynchronous enumerable sequence of all data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="feed">The feed to get data from.</param>
	/// <param name="kind">Specify which data can be returned or not.</param>
	/// <param name="ct">A cancellation to cancel the async enumeration.</param>
	/// <returns>An async enumeration sequence of all acceptable data produced by a feed.</returns>
	public static async IAsyncEnumerable<Option<T>> Options<T>(this IFeed<T> feed, AsyncFeedValue kind = AsyncFeedValue.AllowError, [EnumeratorCancellation] CancellationToken ct = default)
	{
		using var enumCt = CancellationTokenSource.CreateLinkedTokenSource(ct);
		try
		{
			var dataHasChanged = true;
			await foreach (var message in SourceContext.Current.GetOrCreateSource(feed).WithCancellation(enumCt.Token).ConfigureAwait(false))
			{
				var current = message.Current;
				dataHasChanged |= message.Changes.Contains(MessageAxis.Data);

				// Note: We check flags first to make sure to not touch values that are not needed for FeedDependency.

				if (!kind.HasFlag(AsyncFeedValue.AllowTransient) && current.IsTransient)
				{
					continue;
				}

				if (!kind.HasFlag(AsyncFeedValue.AllowError) && current.Error is { } error)
				{
					ExceptionDispatchInfo.Capture(error).Throw();
				}

				if (dataHasChanged)
				{
					yield return current.Data;
					dataHasChanged = false;
				}
			}
		}
		finally
		{
			enumCt.Cancel();
		}
	}

	/// <summary>
	/// Asynchronously get the next message produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="feed">The feed to get message from.</param>
	/// <returns>A ValueTask to asynchronously get the next message produced by the feed.</returns>
	public static async ValueTask<Message<T>> Message<T>(this IFeed<T> feed)
		=> await FeedDependency.TryGetCurrentMessage(feed).ConfigureAwait(false)
			?? await feed.Messages().FirstAsync(SourceContext.Current.Token).ConfigureAwait(false);

	/// <summary>
	/// Asynchronously get the next message produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="feed">The feed to get message from.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next message produced by the feed.</returns>
	public static async ValueTask<Message<T>> Message<T>(this IFeed<T> feed, CancellationToken ct)
		=> await FeedDependency.TryGetCurrentMessage(feed).ConfigureAwait(false)
			?? await feed.Messages().FirstAsync(ct).ConfigureAwait(false);

	/// <summary>
	/// Gets an asynchronous enumerable sequence of all messages produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="feed">The feed to get messages from.</param>
	/// <returns>An async enumeration sequence of all acceptable messages produced by a feed.</returns>
	public static IAsyncEnumerable<Message<T>> Messages<T>(this IFeed<T> feed)
		=> SourceContext.Current.GetOrCreateSource(feed);

	internal static IMessageEntry<T> EnsureNoError<T>(this IMessageEntry<T> entry)
	{
		if (entry.Error is { } error)
		{
			ExceptionDispatchInfo.Capture(error).Throw();
		}

		return entry;
	}
}
