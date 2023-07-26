using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.Extensions.Edition;
using Uno.Extensions.Equality;
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
	/// Creates a custom feed from an async method.
	/// </summary>
	/// <typeparam name="T">The type of the value of the resulting feed.</typeparam>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListFeed<T> Async<T>(AsyncFunc<ImmutableList<T>> valueProvider, Signal? refresh = null)
		=> Feed.Async(valueProvider, refresh).AsListFeed();

	/// <summary>
	/// Creates a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="T">The type of the data of the resulting feed.</typeparam>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListFeed<T> AsyncEnumerable<T>(Func<CancellationToken, IAsyncEnumerable<IImmutableList<T>>> enumerableProvider)
		=> Feed.AsyncEnumerable(enumerableProvider).AsListFeed();

	/// <summary>
	/// [OBSOLETE] Use PaginatedAsync instead.
	/// </summary>
	/// <typeparam name="T">The type of the data of the resulting feed.</typeparam>
	/// <param name="getPage">The async method to load a page of items.</param>
	/// <returns>A paginated list feed.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if DEBUG
	[Obsolete("Use PaginatedAsync instead")]
#endif
	public static IListFeed<T> AsyncPaginated<T>(AsyncFunc<PageRequest, IImmutableList<T>> getPage)
		=> PaginatedAsync(getPage);

	/// <summary>
	/// Creates a list feed for a paginated collection.
	/// </summary>
	/// <typeparam name="T">The type of the data of the resulting feed.</typeparam>
	/// <param name="getPage">The async method to load a page of items.</param>
	/// <returns>A paginated list feed.</returns>
	public static IListFeed<T> PaginatedAsync<T>(AsyncFunc<PageRequest, IImmutableList<T>> getPage)
		=> ListFeed<T>.PaginatedAsync(getPage);
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
		=> AttachedProperty.GetOrCreate(source, predicate, static (src, p) => new WhereListFeed<TSource>(src, p));

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

	/// <summary>
	/// Creates a ListState from a ListFeed onto which the selected items is being synced with the provided external state.
	/// </summary>
	/// <typeparam name="TSource">Type of the items of the list feed.</typeparam>
	/// <param name="source">The source list feed.</param>
	/// <param name="selectionState">The external state from and onto which the selection of the resulting list state is going to be synced.</param>
	/// <param name="caller">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler provide this.</param>
	/// <param name="line">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler provide this.</param>
	/// <returns>A ListState from and onto which the selection is going to be synced.</returns>
	public static IListState<TSource> Selection<TSource>(
		this IListFeed<TSource> source,
		IState<IImmutableList<TSource>> selectionState,
		[CallerMemberName] string caller = "",
		[CallerLineNumber] int line = -1)
		=> AttachedProperty.GetOrCreate(
			source,
			(selectionState, caller, line),
			static (src, args) => ListFeedSelection<TSource>.Create(src, args.selectionState, $"Selection defined in {args.caller} at line {args.line}."));

	/// <summary>
	/// Creates a ListState from a ListFeed onto which the selected items is being synced with the provided external state.
	/// </summary>
	/// <typeparam name="TSource">Type of the items of the list feed.</typeparam>
	/// <param name="source">The source list feed.</param>
	/// <param name="selectionState">The external state from and onto which the selection of the resulting list state is going to be synced.</param>
	/// <param name="caller">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler provide this.</param>
	/// <param name="line">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler provide this.</param>
	/// <returns>A ListState from and onto which the selection is going to be synced.</returns>
	public static IListState<TSource> Selection<TSource>(
		this IListFeed<TSource> source,
		IState<TSource> selectionState,
		[CallerMemberName] string caller = "",
		[CallerLineNumber] int line = -1)
		=> AttachedProperty.GetOrCreate(
			source,
			(selectionState, caller, line),
			static (src, args) => ListFeedSelection<TSource>.Create(src, args.selectionState, $"Selection defined in {args.caller} at line {args.line}."));

	/// <summary>
	/// Creates a ListState from a ListFeed onto which the selected item is being synced with the provided external state using projection.
	/// </summary>
	/// <typeparam name="TSource">Type of the items of the list feed.</typeparam>
	/// <typeparam name="TSourceKey">Type of the key of items of the list feed.</typeparam>
	/// <typeparam name="TOther">Type of the entity which hold the selection.</typeparam>
	/// <param name="source">The source list feed.</param>
	/// <param name="selectionState">The external state from and onto which the selection of the resulting list state is going to be synced.</param>
	/// <param name="keySelector">The selector to get and set the key of the selected item on a <typeparamref name="TOther"/>.</param>
	/// <param name="path">
	/// The path of the file where this operator is being used.
	/// This is used to resolve the <paramref name="keySelector"/> (cf. <see cref="PropertySelector{TEntity,TValue}"/> for more info).
	/// DO NOT provide anything here, let the compiler provide this.
	/// </param>
	/// <param name="caller">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler provide this.</param>
	/// <param name="line">
	/// The line number of the file where this operator is being used.
	/// This is used to resolve the <paramref name="keySelector"/> (cf. <see cref="PropertySelector{TEntity,TValue}"/> for more info).
	/// DO NOT provide anything here, let the compiler provide this.
	/// </param>
	/// <returns>A ListState from and onto which the selection is going to be synced.</returns>
	public static IListState<TSource> Selection<TSource, TSourceKey, TOther>(
		this IListFeed<TSource> source,
		IState<TOther> selectionState,
		PropertySelector<TOther, TSourceKey?> keySelector,
		[CallerFilePath] string path = "",
		[CallerMemberName] string caller = "",
		[CallerLineNumber] int line = -1)
		where TSource : IKeyed<TSourceKey>
		where TSourceKey : notnull
		where TOther : new()
		=> AttachedProperty.GetOrCreate(
			source,
			(selectionState, keySelector, path, caller, line),
			static (src, args) => ListFeedSelection<TSource>.Create(
				src,
				args.selectionState,
				i => i.Key,
				PropertySelectors.Get(args.keySelector, nameof(keySelector), args.path, args.line),
				() => new(),
				default,
				$"Selection defined in {args.caller} at line {args.line}."));

	/// <summary>
	/// Creates a ListState from a ListFeed onto which the selected item is being synced with the provided external state using projection.
	/// </summary>
	/// <typeparam name="TSource">Type of the items of the list feed.</typeparam>
	/// <typeparam name="TSourceKey">Type of the key of items of the list feed.</typeparam>
	/// <typeparam name="TOther">Type of the entity which hold the selection.</typeparam>
	/// <param name="source">The source list feed.</param>
	/// <param name="selectionState">The external state from and onto which the selection of the resulting list state is going to be synced.</param>
	/// <param name="keySelector">The selector to get and set the key of the selected item on a <typeparamref name="TOther"/>.</param>
	/// <param name="path">
	/// The path of the file where this operator is being used.
	/// This is used to resolve the <paramref name="keySelector"/> (cf. <see cref="PropertySelector{TEntity,TValue}"/> for more info).
	/// DO NOT provide anything here, let the compiler provide this.
	/// </param>
	/// <param name="caller">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler provide this.</param>
	/// <param name="line">
	/// The line number of the file where this operator is being used.
	/// This is used to resolve the <paramref name="keySelector"/> (cf. <see cref="PropertySelector{TEntity,TValue}"/> for more info).
	/// DO NOT provide anything here, let the compiler provide this.
	/// </param>
	/// <returns>A ListState from and onto which the selection is going to be synced.</returns>
	public static IListState<TSource> Selection<TSource, TSourceKey, TOther>(
		this IListFeed<TSource> source,
		IState<TOther> selectionState,
		PropertySelector<TOther, TSourceKey?> keySelector,
		[CallerFilePath] string path = "",
		[CallerMemberName] string caller = "",
		[CallerLineNumber] int line = -1)
		where TSource : IKeyed<TSourceKey>
		where TSourceKey : struct
		where TOther : new()
		=> AttachedProperty.GetOrCreate(
			source,
			(selectionState, keySelector, path, caller, line),
			static (src, args) => ListFeedSelection<TSource>.CreateValueType(
				src,
				args.selectionState,
				i => i.Key,
				PropertySelectors.Get(args.keySelector, nameof(keySelector), args.path, args.line),
				() => new(),
				default,
				$"Selection defined in {args.caller} at line {args.line}."));
	#endregion
}
