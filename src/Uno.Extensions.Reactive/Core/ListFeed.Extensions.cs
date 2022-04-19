using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Provides a set of static methods to create and manipulate <see cref="IListFeed{T}"/>.
/// </summary>
public static partial class ListFeed
{
	#region Sources
	// Note: Those are helpers for which the T is set by type inference on provider.
	//		 We must have only one overload per method.

	/// <summary>
	/// Creates a custom feed from a raw <see cref="IAsyncEnumerable{T}"/> sequence of <see cref="Uno.Extensions.Reactive.Message{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the value of the resulting feed.</typeparam>
	/// <param name="sourceProvider">The provider of the message enumerable sequence.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListFeed<T> Create<T>(Func<CancellationToken, IAsyncEnumerable<Message<IImmutableList<T>>>> sourceProvider)
		=> Feed.Create(sourceProvider).AsListFeed();

	/// <summary>
	/// Creates a custom feed from an async method.
	/// </summary>
	/// <typeparam name="T">The type of the value of the resulting feed.</typeparam>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListFeed<T> Async<T>(AsyncFunc<IImmutableList<T>> valueProvider, Signal? refresh = null)
		=> Feed.Async(valueProvider, refresh).AsListFeed();

	/// <summary>
	/// Creates a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="T">The type of the data of the resulting feed.</typeparam>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListFeed<T> AsyncEnumerable<T>(Func<IAsyncEnumerable<IImmutableList<T>>> enumerableProvider)
		=> Feed.AsyncEnumerable(enumerableProvider).AsListFeed();
	#endregion

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
		return new ListFeed<TItem>(source);
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
		=> new ListFeed<TItem>(source.Select(list => list.ToImmutableList() as IImmutableList<TItem>));

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

	//public static IListState<TItem> AsListState<TItem>(
	//	this IState<IImmutableList<TItem>> source)
	//	// Note: We are not attaching the "ListFeed" as we always un-wrap them and we attach other operator on the underlying Source.
	//	=> new ListState<TItem>(source);

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

	#region Operators
	//public static IListFeed<TSource> Where<TSource>(
	//	this IListFeed<TSource> source,
	//	Predicate<TSource?> predicate)
	//	=> default!;

	//public static IListFeed<TResult> Select<TSource, TResult>(
	//	this IListFeed<TSource> source,
	//	Func<TSource?, TResult?> selector)
	//	=> default!;

	/*
	public static IFeed<TResult> SelectAsync<TSource, TResult>(
		this IFeed<TSource> source,
		AsyncFunc<TSource?, TResult?> selector)
		=> default!);
	*/
	#endregion
}
