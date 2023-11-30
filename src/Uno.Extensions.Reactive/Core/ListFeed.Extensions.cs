using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Sources;
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
	public static async ValueTask<IImmutableList<T>> Value<T>(this IListFeed<T> listFeed, CancellationToken ct)
		=> await FeedDependency.TryGetCurrentMessage(listFeed).ConfigureAwait(false) switch
		{
			{ } message => message.Current.EnsureNoError().Data.SomeOrDefault(ImmutableList<T>.Empty),
			null => await listFeed.Values(AsyncFeedValue.Default, ct).FirstOrDefaultAsync(ImmutableList<T>.Empty, ct).ConfigureAwait(false)
		};

	/// <summary>
	/// Asynchronously get the next collection of items produced by a list feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get data from.</param>
	/// <param name="kind">Specify which data can be returned or not.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next acceptable data produced by the feed.</returns>
	public static async ValueTask<IImmutableList<T>> Value<T>(this IListFeed<T> listFeed, AsyncFeedValue kind, CancellationToken ct)
		=> await FeedDependency.TryGetCurrentMessage(listFeed).ConfigureAwait(false) switch
		{
			not null when kind is not AsyncFeedValue.Default => throw new NotSupportedException($"Only kind AsyncFeedValue.Default is currently supported by the dynamic feed (requested: {kind})."),
			{ } message => message.Current.EnsureNoError().Data.SomeOrDefault(ImmutableList<T>.Empty),
			null => await listFeed.Values(kind, ct).FirstOrDefaultAsync(ImmutableList<T>.Empty, ct).ConfigureAwait(false)
		};

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
	public static async ValueTask<Option<IImmutableList<T>>> Option<T>(this IListFeed<T> listFeed, CancellationToken ct)
		=> await FeedDependency.TryGetCurrentMessage(listFeed).ConfigureAwait(false) switch
		{
			{ } message => message.Current.EnsureNoError().Data,
			null => await listFeed.Options(AsyncFeedValue.Default, ct).FirstOrDefaultAsync(Extensions.Option<IImmutableList<T>>.Undefined(), ct).ConfigureAwait(false)
		};

	/// <summary>
	/// Asynchronously get the next collection of items produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get data from.</param>
	/// <param name="kind">Specify which data can be returned or not.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next acceptable data produced by the feed.</returns>
	public static async ValueTask<Option<IImmutableList<T>>> Option<T>(this IListFeed<T> listFeed, AsyncFeedValue kind, CancellationToken ct)
		=> await FeedDependency.TryGetCurrentMessage(listFeed).ConfigureAwait(false) switch
		{
			not null when kind is not AsyncFeedValue.Default => throw new NotSupportedException($"Only kind AsyncFeedValue.Default is currently supported by the dynamic feed (requested: {kind})."),
			{ } message => message.Current.EnsureNoError().Data,
			null => await listFeed.Options(kind, ct).FirstOrDefaultAsync(Extensions.Option<IImmutableList<T>>.Undefined(), ct).ConfigureAwait(false)
		};

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
		var dataHasChanged = true;
		await foreach (var message in SourceContext.Current.GetOrCreateSource(listFeed).WithCancellation(ct).ConfigureAwait(false))
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

	/// <summary>
	/// Asynchronously gets the next message produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get message from.</param>
	/// <returns>A ValueTask to asynchronously get the next message produced by the feed.</returns>
	public static async ValueTask<Message<IImmutableList<T>>> Message<T>(this IListFeed<T> listFeed)
		=> await FeedDependency.TryGetCurrentMessage(listFeed).ConfigureAwait(false)
			?? await listFeed.Messages().FirstAsync(SourceContext.Current.Token).ConfigureAwait(false);

	/// <summary>
	/// Asynchronously get the next message produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get message from.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next message produced by the feed.</returns>
	public static async ValueTask<Message<IImmutableList<T>>> Message<T>(this IListFeed<T> listFeed, CancellationToken ct)
		=> await FeedDependency.TryGetCurrentMessage(listFeed).ConfigureAwait(false)
			?? await listFeed.Messages().FirstAsync(ct).ConfigureAwait(false);

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
		=> AttachedProperty.GetOrCreate(source, typeof(TItem), (s, _) => new FeedToListFeedAdapter<ImmutableList<TItem>, TItem>(s, list => list));

	/// <summary>
	/// Wraps a feed of list into a <see cref="IListFeed{T}"/>.
	/// </summary>
	/// <typeparam name="TCollection">Type of the items collection.</typeparam>
	/// <typeparam name="TItem">Type of items in the list.</typeparam>
	/// <param name="source">The source list stream to wrap.</param>
	/// <returns>A <see cref="IListFeed{T}"/> that wraps the given source data stream.</returns>
	public static IListFeed<TItem> AsListFeed<TCollection, TItem>(this IFeed<TCollection> source)
		where TCollection : IImmutableList<TItem>
		=> AttachedProperty.GetOrCreate(source, typeof(TItem), (s, _) => new FeedToListFeedAdapter<TCollection, TItem>(s, list => list));

	/// <summary>
	/// Converts a feed of list into a <see cref="IListFeed{T}"/>.
	/// </summary>
	/// <typeparam name="TCollection">Type of the items collection.</typeparam>
	/// <typeparam name="TItem">Type of items in the list.</typeparam>
	/// <param name="source">The source list stream to wrap.</param>
	/// <returns>A <see cref="IListFeed{T}"/> that wraps the given source data stream.</returns>
	/// <remarks>
	/// With this extension, the lists from the source feed might be enumerated more than once.
	/// Use with caution.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static IListFeed<TItem> ToListFeed<TCollection, TItem>(this IFeed<TCollection> source)
		where TCollection : IEnumerable<TItem>
		=> AttachedProperty.GetOrCreate(source, typeof(TItem), (s, _) => new FeedToListFeedAdapter<TCollection, TItem>(s, list => list?.ToImmutableList() ?? ImmutableList<TItem>.Empty));

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

	/// <summary>
	/// Gets the selected items of a list feed, or an empty collection if none.
	/// </summary>
	/// <typeparam name="T">Type of the items of the list feed.</typeparam>
	/// <param name="source">The source list feed to get selected items for.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>The selected items, or an empty collection if none.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static async ValueTask<IImmutableList<T>> GetSelectedItems<T>(this IListFeed<T> source, CancellationToken ct)
		=> (await source.Message(ct).ConfigureAwait(false)).Current.GetSelectedItems();

	/// <summary>
	/// Gets the selected item of a list feed, or null if none.
	/// </summary>
	/// <typeparam name="T">Type of the items of the list feed.</typeparam>
	/// <param name="source">The source list feed to get selected items for.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>The selected item, or null if none.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static async ValueTask<T?> GetSelectedItem<T>(this IListFeed<T> source, CancellationToken ct)
		where T : notnull
		=> (await source.Message(ct).ConfigureAwait(false)).Current.GetSelectedItem();
}
