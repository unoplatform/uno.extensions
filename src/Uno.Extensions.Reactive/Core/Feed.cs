#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Provides a set of static methods to create and manipulate <see cref="IFeed{T}"/>.
/// </summary>
public static partial class Feed
{
	#region Sources
	// Note: Those are helpers for which the T is set by type inference on provider.
	//		 We must have only one overload per method.

	/// <summary>
	/// Gets or create a custom feed from an async method.
	/// </summary>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	internal static IFeed<T> Dynamic<T>(AsyncFunc<T?> valueProvider)
		where T : notnull
		=> AttachedProperty.GetOrCreate(valueProvider, static vp => new DynamicFeed<T>(vp));


	/// <summary>
	/// Gets or create a custom feed from a raw <see cref="IAsyncEnumerable{T}"/> sequence of <see cref="Uno.Extensions.Reactive.Message{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the value of the resulting feed.</typeparam>
	/// <param name="sourceProvider">The provider of the message enumerable sequence.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IFeed<T> Create<T>(Func<CancellationToken, IAsyncEnumerable<Message<T>>> sourceProvider)
		=> Feed<T>.Create(sourceProvider);

	/// <summary>
	/// Gets or create a custom feed from an async method.
	/// </summary>
	/// <typeparam name="T">The type of the value of the resulting feed.</typeparam>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IFeed<T> Async<T>(AsyncFunc<T> valueProvider, Signal? refresh = null)
		=> Feed<T>.Async(valueProvider, refresh);

	/// <summary>
	/// Gets or create a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="T">The type of the data of the resulting feed.</typeparam>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IFeed<T> AsyncEnumerable<T>(Func<CancellationToken, IAsyncEnumerable<T>> enumerableProvider)
		=> Feed<T>.AsyncEnumerable(enumerableProvider);
	#endregion

	#region Operators
	// Note: The operators are only dealing with values.
	//		 To deal with Message<T> or Option<T>, we will request to user to enumerate themselves the source

	/// <summary>
	/// Gets or create a feed that filters out some values of a source feed.
	/// </summary>
	/// <typeparam name="TSource">Type of the value of the feed.</typeparam>
	/// <param name="source">The source feed to filter.</param>
	/// <param name="predicate">The predicate to apply to values.</param>
	/// <returns>A feed that filters out some values of the source feed</returns>
	/// <remarks>
	/// Unlike <see cref="IEnumerable{T}"/>, <see cref="IAsyncEnumerable{T}"/> or <see cref="IObservable{T}"/>,
	/// a filtered out value from source feed **will produce a message** with its data set to None.
	/// </remarks>
	public static IFeed<TSource> Where<TSource>(
		this IFeed<TSource> source,
		Predicate<TSource> predicate)
		=> AttachedProperty.GetOrCreate(source, predicate, static (src, p) => new WhereFeed<TSource>(src, p));

	/// <summary>
	/// Gets or create a feed that projects each value of a source feed.
	/// </summary>
	/// <typeparam name="TSource">Type of the value of the source feed.</typeparam>
	/// <typeparam name="TResult">Type of the value of the resulting feed.</typeparam>
	/// <param name="source">The source feed to project.</param>
	/// <param name="selector">The projection method.</param>
	/// <returns>A feed that projects each value of the source feed.</returns>
	public static IFeed<TResult> Select<TSource, TResult>(
		this IFeed<TSource> source,
		Func<TSource, TResult> selector)
		=> AttachedProperty.GetOrCreate(source, selector, static (src, s) => new SelectFeed<TSource, TResult>(src, s));

	/// <summary>
	/// Gets or create a feed that asynchronously projects each value of a source feed.
	/// </summary>
	/// <typeparam name="TSource">Type of the value of the source feed.</typeparam>
	/// <typeparam name="TResult">Type of the value of the resulting feed.</typeparam>
	/// <param name="source">The source feed to project.</param>
	/// <param name="selector">The asynchronous projection method.</param>
	/// <returns>A feed that projects each value of the source feed.</returns>
	public static IFeed<TResult> SelectAsync<TSource, TResult>(
		this IFeed<TSource> source,
		AsyncFunc<TSource, TResult> selector)
		=> AttachedProperty.GetOrCreate(source, selector, static (src, s) => new SelectAsyncFeed<TSource, TResult>(src, s));

	public static IListFeed<TResult> SelectPaginatedAsync<TSource, TResult>(
		this IFeed<TSource> source,
		AsyncFunc<TSource, PageRequest, IImmutableList<TResult>> getPage)
		where TSource : notnull
	{
		return AttachedProperty.GetOrCreate(source, getPage, Create).AsListFeed();

		static IFeed<IImmutableList<TResult>> Create(IFeed<TSource> parameter, AsyncFunc<TSource, PageRequest, IImmutableList<TResult>> gp)
			=> new DynamicFeed<IImmutableList<TResult>>(async _ =>
			{
				FeedExecution.Current!.EnableRefresh();

				var value = await parameter;
				if (value is null)
				{
					return ImmutableList<TResult>.Empty;
				}

				var items = await FeedExecution.Current!.GetPaginated<TResult>(
					b => b
						.ByIndex()
						.GetPage(async (req, ct) => await gp(value, req, ct)));

				return items;
			});
	}
	#endregion
}
