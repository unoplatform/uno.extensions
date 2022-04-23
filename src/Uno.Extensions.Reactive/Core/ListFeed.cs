using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

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

	#region Operators
	/// <summary>
	/// Gets or create a list feed that filters out some items of a source feed.
	/// </summary>
	/// <typeparam name="TSource">Type of the items of the list feed.</typeparam>
	/// <param name="source">The source list feed to filter.</param>
	/// <param name="predicate">The predicate to apply to items.</param>
	/// <returns>A feed that filters out some items of the source feed</returns>
	/// <remarks>
	/// Unlike <see cref="IEnumerable{T}"/>, <see cref="IAsyncEnumerable{T}"/> or <see cref="IObservable{T}"/>,
	/// if all items are filtered out from source list feed,
	/// the resulting list feed  **will produce a message** with its data set to None.
	/// </remarks>
	public static IListFeed<TSource> Where<TSource>(
		this IListFeed<TSource> source,
		Predicate<TSource> predicate)
		=> AttachedProperty.GetOrCreate(source, predicate, (src, p) => new WhereListFeed<TSource>(src, p));

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
