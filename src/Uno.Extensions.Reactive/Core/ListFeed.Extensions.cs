using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
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
	public static ValueTaskAwaiter<Option<IImmutableList<T>>> GetAwaiter<T>(this IListFeed<T> listFeed)
		=> listFeed.AsFeed().GetAwaiter();

	/// <summary>
	/// Asynchronously get the next data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The feed to get data from.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next data produced by the feed.</returns>
	public static ValueTask<Option<IImmutableList<T>>> Value<T>(this IListFeed<T> listFeed, CancellationToken ct)
		=> listFeed.AsFeed().Value(ct);

	/// <summary>
	/// Asynchronously get the next data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get data from.</param>
	/// <param name="kind">Specify which data can be returned or not.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next acceptable data produced by the feed.</returns>
	public static ValueTask<Option<IImmutableList<T>>> Value<T>(this IListFeed<T> listFeed, AsyncFeedValue kind, CancellationToken ct)
		=> listFeed.AsFeed().Value(kind, ct);

	/// <summary>
	/// Gets an asynchronous enumerable sequence of all data produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get data from.</param>
	/// <param name="kind">Specify which data can be returned or not.</param>
	/// <param name="ct">A cancellation to cancel the async enumeration.</param>
	/// <returns>An async enumeration sequence of all acceptable data produced by a feed.</returns>
	public static IAsyncEnumerable<Option<IImmutableList<T>>> Values<T>(this IListFeed<T> listFeed, AsyncFeedValue kind = AsyncFeedValue.AllowError, [EnumeratorCancellation] CancellationToken ct = default)
		=> listFeed.AsFeed().Values(kind, ct);

	/// <summary>
	/// Asynchronously get the next message produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get message from.</param>
	/// <returns>A ValueTask to asynchronously get the next message produced by the feed.</returns>
	public static ValueTask<Message<IImmutableList<T>>> Message<T>(this IListFeed<T> listFeed)
		=> listFeed.AsFeed().Message();

	/// <summary>
	/// Asynchronously get the next message produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get message from.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to asynchronously get the next message produced by the feed.</returns>
	public static ValueTask<Message<IImmutableList<T>>> Message<T>(this IListFeed<T> listFeed, CancellationToken ct)
		=> listFeed.AsFeed().Message(ct);

	/// <summary>
	/// Gets an asynchronous enumerable sequence of all messages produced by a feed.
	/// </summary>
	/// <typeparam name="T">The type of the value of the feed.</typeparam>
	/// <param name="listFeed">The list feed to get messages from.</param>
	/// <returns>An async enumeration sequence of all acceptable messages produced by a feed.</returns>
	public static IAsyncEnumerable<Message<IImmutableList<T>>> Messages<T>(this IListFeed<T> listFeed)
		=> listFeed.AsFeed().Messages();

	#region Convertions
	/// <summary>
	/// Wraps a feed of list into a <see cref="IListFeed{T}"/>.
	/// </summary>
	/// <typeparam name="TItem">Type of items in the list.</typeparam>
	/// <param name="source">The source list stream to wrap.</param>
	/// <returns>A <see cref="IListFeed{T}"/> that wraps the given source data stream of list.</returns>
	public static IListFeed<TItem> AsListFeed<TItem>(this IFeed<IImmutableList<TItem>> source)
	{
		// Note: We are not attaching the "ListFeed" as we always un-wrap them and we attach other operator on the underlying Source.
		return new ListFeedImpl<TItem>(source);
	}

	/// <summary>
	/// Wraps a feed of list into a <see cref="IListFeed{T}"/>.
	/// </summary>
	/// <typeparam name="TItem">Type of items in the list.</typeparam>
	/// <param name="source">The source list stream to wrap.</param>
	/// <returns>A <see cref="IListFeed{T}"/> that wraps the given source data stream of list.</returns>
	public static IListFeed<TItem> AsListFeed<TItem>(this IFeed<ImmutableList<TItem>> source)
	{
		// Note: We are not attaching the "ListFeed" as we always un-wrap them and we attach other operator on the underlying Source.
		return new ListFeedImpl<TItem>(source.Select(list => list as IImmutableList<TItem>));
	}

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
		=> new ListFeedImpl<TItem>(source.Select(list => list.ToImmutableList() as IImmutableList<TItem>));

	/// <summary>
	/// Unwraps a <see cref="IListFeed{T}"/> to get the source feed of list.
	/// </summary>
	/// <typeparam name="TItem">Type of items in the list.</typeparam>
	/// <param name="source">The source list stream to wrap.</param>
	/// <returns>The source data stream of list of the given <see cref="IListFeed{T}"/>.</returns>
	public static IFeed<IImmutableList<TItem>> AsFeed<TItem>(
		this IListFeed<TItem> source)
		=> source is IListFeedWrapper<TItem> wrapper
			? wrapper.Source
			// The IListFeed is a direct implementation (external). It's the only case where we attach something to the IListFeed.
			// All subsequent operators are going to be attached to the newly created wrap Feed (i.e. not ListFeed)
			: AttachedProperty.GetOrCreate(source, typeof(TItem), WrapListFeed);

	/// <summary>
	/// Wraps a feed of list into a <see cref="IListState{T}"/>.
	/// </summary>
	/// <typeparam name="TItem">Type of items in the list.</typeparam>
	/// <param name="source">The source list state to wrap.</param>
	/// <returns>A <see cref="IListFeed{T}"/> that wraps the given source data stream of list.</returns>
	public static IListState<TItem> AsListState<TItem>(
		this IState<IImmutableList<TItem>> source)
		// Note: We are not attaching the "ListFeed" as we always un-wrap them and we attach other operator on the underlying Source.
		=> new ListState<TItem>(source);

	//public static IState<IImmutableList<TItem>> AsState<TItem>(
	//	this IListState<TItem> source)
	//	=> source is IListFeedWrapper<TItem> wrapper
	//		? wrapper.Source
	//		// The IListFeed is a direct implementation (external). It's the only case where we attach something to the IListFeed.
	//		// All subsequent operators are going to be attached to the newly created wrap Feed (i.e. not ListFeed)
	//		: AttachedProperty.GetOrCreate(source, typeof(TItem), WrapListFeed);

	private static IFeed<IImmutableList<TItem>> WrapListFeed<TItem>(IListFeed<TItem> listFeed, Type _)
	{
		// TODO Uno
		return null!;
	}
	#endregion
}
