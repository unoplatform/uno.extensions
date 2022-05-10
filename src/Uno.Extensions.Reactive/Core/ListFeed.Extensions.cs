using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Provides a set of static methods to create and manipulate <see cref="IListFeed{T}"/>.
/// </summary>
public static partial class ListFeed
{
	/// <summary>
	/// Get an awaiter to asynchronously get the next data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get data from.</param>
	/// <returns>An awaiter to asynchronously get the next data produced by the feed.</returns>
	public static ValueTaskAwaiter<IImmutableList<T>> GetAwaiter<T>(this IListFeed<T> listFeed)
		=> listFeed.Value(SourceContext.Current.Token).GetAwaiter();

	/// <summary>
	/// Asynchronously gets the next collection of items produced by a list feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The feed to get data from.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next data produced by the feed.</returns>
	public static ValueTask<IImmutableList<T>> Value<T>(this IListFeed<T> listFeed, CancellationToken ct)
		=> listFeed.Values(AsyncFeedValue.Default, ct).FirstOrDefaultAsync(ImmutableList<T>.Empty, ct);

	/// <summary>
	/// Asynchronously get the next collection of items produced by a list feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get data from.</param>
	/// <param name="kind">Specify which data can be returned or not.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next acceptable data produced by the feed.</returns>
	public static ValueTask<IImmutableList<T>> Value<T>(this IListFeed<T> listFeed, AsyncFeedValue kind, CancellationToken ct)
		=> listFeed.Values(kind, ct).FirstOrDefaultAsync(ImmutableList<T>.Empty, ct);

	/// <summary>
	/// Gets an asynchronous enumerable sequence of all collection of items produced by a list feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get data from.</param>
	/// <param name="kind">Specify which data can be returned or not.</param>
	/// <param name="ct">A cancellation to cancel the async enumeration.</param>
	/// <returns>An async enumeration sequence of all acceptable data produced by a feed.</returns>
	public static IAsyncEnumerable<IImmutableList<T>> Values<T>(this IListFeed<T> listFeed, AsyncFeedValue kind = AsyncFeedValue.AllowError, CancellationToken ct = default)
		=> listFeed.Options(kind, ct).Select(opt => opt.SomeOrDefault(ImmutableList<T>.Empty));

	/// <summary>
	/// Asynchronously gets the next collection of items produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The feed to get data from.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next data produced by the feed.</returns>
	public static ValueTask<Option<IImmutableList<T>>> Option<T>(this IListFeed<T> listFeed, CancellationToken ct)
		=> listFeed.Options(AsyncFeedValue.Default, ct).FirstOrDefaultAsync(Extensions.Option<IImmutableList<T>>.Undefined(), ct);

	/// <summary>
	/// Asynchronously get the next collection of items produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get data from.</param>
	/// <param name="kind">Specify which data can be returned or not.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next acceptable data produced by the feed.</returns>
	public static ValueTask<Option<IImmutableList<T>>> Option<T>(this IListFeed<T> listFeed, AsyncFeedValue kind, CancellationToken ct)
		=> listFeed.Options(kind, ct).FirstOrDefaultAsync(Extensions.Option<IImmutableList<T>>.Undefined(), ct);

	/// <summary>
	/// Gets an asynchronous enumerable sequence of all collection of items produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get data from.</param>
	/// <param name="kind">Specify which data can be returned or not.</param>
	/// <param name="ct">A cancellation to cancel the async enumeration.</param>
	/// <returns>An async enumeration sequence of all acceptable data produced by a feed.</returns>
	public static async IAsyncEnumerable<Option<IImmutableList<T>>> Options<T>(this IListFeed<T> listFeed, AsyncFeedValue kind = AsyncFeedValue.AllowError, [EnumeratorCancellation] CancellationToken ct = default)
	{
		await foreach (var message in SourceContext.Current.GetOrCreateSource(listFeed).WithCancellation(ct).ConfigureAwait(false))
		{
			var current = message.Current;

			if (current.IsTransient && !kind.HasFlag(AsyncFeedValue.AllowTransient))
			{
				continue;
			}

			if (current.Error is { } error && !kind.HasFlag(AsyncFeedValue.AllowError))
			{
				ExceptionDispatchInfo.Capture(error).Throw();
			}

			yield return current.Data;
		}
	}

	/// <summary>
	/// Asynchronously gets the next message produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get message from.</param>
	/// <returns>A ValueTask to asynchronously get the next message produced by the feed.</returns>
	public static ValueTask<Message<IImmutableList<T>>> Message<T>(this IListFeed<T> listFeed)
		=> listFeed.Messages().FirstAsync(SourceContext.Current.Token);

	/// <summary>
	/// Asynchronously get the next message produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get message from.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next message produced by the feed.</returns>
	public static ValueTask<Message<IImmutableList<T>>> Message<T>(this IListFeed<T> listFeed, CancellationToken ct)
		=> listFeed.Messages().FirstAsync(ct);

	/// <summary>
	/// Gets an asynchronous enumerable sequence of all messages produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get messages from.</param>
	/// <returns>An async enumeration sequence of all acceptable messages produced by a feed.</returns>
	public static IAsyncEnumerable<Message<IImmutableList<T>>> Messages<T>(this IListFeed<T> listFeed)
		=> SourceContext.Current.GetOrCreateSource(listFeed);

	#region Convertions
	/// <summary>
	/// Wraps a feed of list into a <see cref="IListFeed{T}"/>.
	/// </summary>
	/// <typeparam name="TItem">Type of items in the list.</typeparam>
	/// <param name="source">The source list stream to wrap.</param>
	/// <returns>A <see cref="IListFeed{T}"/> that wraps the given source data stream of list.</returns>
	public static IListFeed<TItem> AsListFeed<TItem>(this IFeed<IImmutableList<TItem>> source)
		=> source is ListFeedToFeedAdapter<TItem> adapter
			? adapter.Source
			: AttachedProperty.GetOrCreate(source, typeof(TItem), (s, _) => new FeedToListFeedAdapter<TItem>(s));

	/// <summary>
	/// Wraps a feed of list into a <see cref="IListFeed{T}"/>.
	/// </summary>
	/// <typeparam name="TItem">Type of items in the list.</typeparam>
	/// <param name="source">The source list stream to wrap.</param>
	/// <returns>A <see cref="IListFeed{T}"/> that wraps the given source data stream of list.</returns>
	public static IListFeed<TItem> AsListFeed<TItem>(this IFeed<ImmutableList<TItem>> source)
		=> source.Select(list => list as IImmutableList<TItem>).AsListFeed();

	/// <summary>
	/// Wraps a feed of list into a <see cref="IListFeed{T}"/>.
	/// </summary>
	/// <typeparam name="TCollection">Type of the items collection.</typeparam>
	/// <typeparam name="TItem">Type of items in the list.</typeparam>
	/// <param name="source">The source list stream to wrap.</param>
	/// <returns>A <see cref="IListFeed{T}"/> that wraps the given source data stream.</returns>
	public static IListFeed<TItem> AsListFeed<TCollection, TItem>(
		this IFeed<TCollection> source)
		where TCollection : IImmutableList<TItem>
		// Note: We are not attaching the "ListFeed" as we always un-wrap them and we attach other operator on the underlying Source.
		=> source.Select(list => list.ToImmutableList() as IImmutableList<TItem>).AsListFeed();

	/// <summary>
	/// Unwraps a <see cref="IListFeed{T}"/> to get the source feed of list.
	/// </summary>
	/// <typeparam name="TItem">Type of items in the list.</typeparam>
	/// <param name="source">The source list stream to wrap.</param>
	/// <returns>The source data stream of list of the given <see cref="IListFeed{T}"/>.</returns>
	public static IFeed<IImmutableList<TItem>> AsFeed<TItem>(
		this IListFeed<TItem> source)
		// Note: DO NOT unwrap FeedToListFeedAdapter, as it adds some behavior
		=> AttachedProperty.GetOrCreate(source, typeof(TItem), (s, _) => new ListFeedToFeedAdapter<TItem>(s));
	#endregion
}
