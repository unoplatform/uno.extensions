using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Provides a set of static methods to create and manipulate <see cref="IListState{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the data.</typeparam>
public static class ListState<
	[DynamicallyAccessedMembers(ListFeed.TRequirements)]
	T
>
{
	/// <summary>
	/// Creates a custom feed from a raw <see cref="IAsyncEnumerable{T}"/> sequence of <see cref="Message{T}"/>.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="sourceProvider">The provider of the message enumerable sequence.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> Create<TOwner>(TOwner owner, Func<CancellationToken, IAsyncEnumerable<Message<IImmutableList<T>>>> sourceProvider)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, sourceProvider, static (o, sp) => S(o, new CustomFeed<IImmutableList<T>>(sp)));

	/// <summary>
	/// Creates a custom feed from a raw <see cref="IAsyncEnumerable{T}"/> sequence of <see cref="Message{T}"/>.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="sourceProvider">The provider of the message enumerable sequence.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> Create<TOwner>(TOwner owner, Func<IAsyncEnumerable<Message<IImmutableList<T>>>> sourceProvider)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, sourceProvider, static (o, sp) => S(o, new CustomFeed<IImmutableList<T>>(_ => sp())));

	/// <summary>
	/// Gets or creates an empty list state.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="name">The caller member where the state is being declared in code and which is used in the key to uniquely identify the state.</param>
	/// <param name="line">The line where the state is being declared in code and which is used in the key to uniquely identify the state.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> Empty<TOwner>(TOwner owner, [CallerMemberName] string? name = null, [CallerLineNumber] int line = -1)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(
			owner,
			(
				name ?? throw new InvalidOperationException("The name of the list state must not be null"),
				line < 0 ? throw new InvalidOperationException("The provided line number is invalid.") : line
			),
			static (o, _) => SourceContext.GetOrCreate(o).CreateListState(Option<IImmutableList<T>>.None()));

	/// <summary>
	/// Gets or creates a list state from a static initial list of items.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The provider of the initial value of the state.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> Value<TOwner>(TOwner owner, Func<IImmutableList<T>> valueProvider)
		where TOwner : class
		// Note: We force the usage of delegate so 2 properties which are doing State.Value(this, () => 42) will effectively have 2 distinct states.
		=> AttachedProperty.GetOrCreate(owner, valueProvider, static (o, v) => SourceContext.GetOrCreate(o).CreateListState(Option<IImmutableList<T>>.Some(v())));

	/// <summary>
	/// Gets or creates a list state from a static initial list of items.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The provider of the initial value of the state.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> Value<TOwner>(TOwner owner, Func<ImmutableList<T>> valueProvider)
		where TOwner : class
		// Note: We force the usage of delegate so 2 properties which are doing State.Value(this, () => 42) will effectively have 2 distinct states.
		=> AttachedProperty.GetOrCreate(owner, valueProvider, static (o, v) => SourceContext.GetOrCreate(o).CreateListState(Option<IImmutableList<T>>.Some(v())));

	/// <summary>
	/// Gets or creates a list state from a static initial list of items.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The provider of the initial value of the state.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> Value<TOwner>(TOwner owner, Func<Option<IImmutableList<T>>> valueProvider)
		where TOwner : class
		// Note: We force the usage of delegate so 2 properties which are doing State.Value(this, () => 42) will effectively have 2 distinct states.
		=> AttachedProperty.GetOrCreate(owner, valueProvider, static (o, v) => SourceContext.GetOrCreate(o).CreateListState(v()));

	/// <summary>
	/// Gets or creates a list state from a static initial list of items.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The provider of the initial value of the state.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> Value<TOwner>(TOwner owner, Func<Option<ImmutableList<T>>> valueProvider)
		where TOwner : class
		// Note: We force the usage of delegate so 2 properties which are doing State.Value(this, () => 42) will effectively have 2 distinct states.
		=> AttachedProperty.GetOrCreate(owner, valueProvider, static (o, v) => SourceContext.GetOrCreate(o).CreateListState(v().Map(l => l as IImmutableList<T>)));

	/// <summary>
	/// Creates a custom feed from an async method.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> Async<TOwner>(TOwner owner, AsyncFunc<Option<IImmutableList<T>>> valueProvider, Signal? refresh = null)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, (valueProvider, refresh), static (o, args) => S(o, new AsyncFeed<IImmutableList<T>>(args.valueProvider, args.refresh)));

	/// <summary>
	/// Creates a custom feed from an async method.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> Async<TOwner>(TOwner owner, AsyncFunc<Option<ImmutableList<T>>> valueProvider, Signal? refresh = null)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, (valueProvider, refresh), static (o, args) => S(o, new AsyncFeed<IImmutableList<T>>(async ct => (await args.valueProvider(ct).ConfigureAwait(false)).Map(l => l as IImmutableList<T>), args.refresh)));

	/// <summary>
	/// Creates a custom feed from an async method.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> Async<TOwner>(TOwner owner, AsyncFunc<IImmutableList<T>> valueProvider, Signal? refresh = null)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, (valueProvider, refresh), static (o, args) => S(o, new AsyncFeed<IImmutableList<T>>(async ct => await args.valueProvider(ct).ConfigureAwait(false), args.refresh)));

	/// <summary>
	/// Creates a custom feed from an async method.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> Async<TOwner>(TOwner owner, AsyncFunc<ImmutableList<T>> valueProvider, Signal? refresh = null)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, (valueProvider, refresh), static (o, args) => S(o, new AsyncFeed<IImmutableList<T>>(async ct => await args.valueProvider(ct).ConfigureAwait(false) as IImmutableList<T>, args.refresh)));

	/// <summary>
	/// Creates a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> AsyncEnumerable<TOwner>(TOwner owner, Func<CancellationToken, IAsyncEnumerable<Option<IImmutableList<T>>>> enumerableProvider)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, enumerableProvider, static (o, ep) => S(o, new AsyncEnumerableFeed<IImmutableList<T>>(ep)));

	/// <summary>
	/// Creates a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> AsyncEnumerable<TOwner>(TOwner owner, Func<IAsyncEnumerable<Option<IImmutableList<T>>>> enumerableProvider)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, enumerableProvider, static (o, ep) => S(o, new AsyncEnumerableFeed<IImmutableList<T>>(ep)));

	/// <summary>
	/// Creates a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static IListState<T> AsyncEnumerable(Func<IAsyncEnumerable<Option<IImmutableList<T>>>> enumerableProvider)
		=> AttachedProperty.GetOrCreate(Validate(enumerableProvider), static ep => S(ep, new AsyncEnumerableFeed<IImmutableList<T>>(ep)));

	/// <summary>
	/// Creates a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> AsyncEnumerable<TOwner>(TOwner owner, Func<CancellationToken, IAsyncEnumerable<IImmutableList<T>>> enumerableProvider)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, enumerableProvider, static (o, ep) => S(o, new AsyncEnumerableFeed<IImmutableList<T>>(ep)));

	/// <summary>
	/// Creates a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> AsyncEnumerable<TOwner>(TOwner owner, Func<IAsyncEnumerable<IImmutableList<T>>> enumerableProvider)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, enumerableProvider, static (o, ep) => S(o, new AsyncEnumerableFeed<IImmutableList<T>>(ep)));

	/// <summary>
	/// Creates a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static IListState<T> AsyncEnumerable(Func<IAsyncEnumerable<IImmutableList<T>>> enumerableProvider)
		=> AttachedProperty.GetOrCreate(Validate(enumerableProvider), static ep => S(ep, new AsyncEnumerableFeed<IImmutableList<T>>(ep)));

	/// <summary>
	/// Gets or creates a state from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="feed">The source feed of the resulting state.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListState<T> FromFeed<TOwner>(TOwner owner, IListFeed<T> feed)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, feed, static (o, f) => S(o, f));

	// WARNING: This not implemented for restrictions described in the remarks section of the AsyncPaginated
	//			While restrictions are acceptable for a paginated by index ListState, it would be invalid for custom cursors.
	///// <summary>
	///// Creates a list feed for a paginated collection.
	///// </summary>
	///// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	///// <typeparam name="TCursor">Type of the cursor that is used to identify a page to load.</typeparam>
	///// <param name="owner">The owner of the state.</param>
	///// <param name="firstPage">The cursor of the first page.</param>
	///// <param name="getPage">The async method to load a page of items.</param>
	///// <returns>A paginated list feed.</returns>
	//public static IListState<T> AsyncPaginatedByCursor<TOwner, TCursor>(TOwner owner, TCursor firstPage, GetPage<TCursor, T> getPage)
	//	where TOwner : class
	//	=> AttachedProperty.GetOrCreate(owner, (getPage, firstPage), (o, args) => S(o, new PaginatedListFeed<TCursor, T>(args.firstPage, args.getPage).AsFeed()));

	/// <summary>
	/// [OBSOLETE] Use PaginatedAsync instead.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="getPage">The async method to load a page of items.</param>
	/// <returns>A paginated list feed.</returns>
	/// <remarks>
	/// This is only a weak implementation which provides the current count of items in the ListState when a new page is requested,
	/// instead of the number of items that has been loaded so far by pagination.
	/// ** It does not ensure any tracking of entities. **
	/// This means that the only operations that this state (weakly) supports is Insert.
	/// For instance if you load a first page of 20 items, then insert one **on top**, next page request will be with CurrentCount = 21,
	/// so item which was at index 19 and now is at index 20 won't be loaded twice from the server.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if DEBUG
	[Obsolete("Use PaginatedAsync instead")]
#endif
	public static IListState<T> AsyncPaginated<TOwner>(TOwner owner, AsyncFunc<PageRequest, IImmutableList<T>> getPage)
		where TOwner : class
		=> PaginatedAsync(owner, getPage);

	/// <summary>
	/// Creates a list state for a paginated collection.
	/// WARNING, be aware of restriction described in remarks.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="getPage">The async method to load a page of items.</param>
	/// <returns>A paginated list feed.</returns>
	/// <remarks>
	/// This is only a weak implementation which provides the current count of items in the ListState when a new page is requested,
	/// instead of the number of items that has been loaded so far by pagination.
	/// ** It does not ensure any tracking of entities. **
	/// This means that the only operations that this state (weakly) supports is Insert.
	/// For instance if you load a first page of 20 items, then insert one **on top**, next page request will be with CurrentCount = 21,
	/// so item which was at index 19 and now is at index 20 won't be loaded twice from the server.
	/// </remarks>
	public static IListState<T> PaginatedAsync<TOwner>(TOwner owner, AsyncFunc<PageRequest, IImmutableList<T>> getPage)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, getPage, static (o, gp) =>
		{
			ListStateImpl<T>? state = default;
			var paginatedFeed = new PaginatedListFeed<ByIndexCursor<T>, T>(ByIndexCursor<T>.First, ByIndexCursor<T>.GetPage(GetPage));
			state = SourceContext.GetOrCreate(o).DoNotUse_GetOrCreateListState(paginatedFeed, StateUpdateKind.Persistent);

			return state;

			ValueTask<IImmutableList<T>> GetPage(PageRequest request, CancellationToken ct)
			{
				var actualCount = state?.Current.Current.Data.IsSome(out var currentItems) ?? false
					? (uint)currentItems.Count
					: request.CurrentCount;
				var actualRequest = request with { CurrentCount = actualCount };

				return gp(actualRequest, ct);
			}
		});

	private static TKey Validate<TKey>(TKey key, [CallerMemberName] string? caller = null)
		where TKey : Delegate
	{
		// TODO: We should make sure to **not** allow method group on an **external** object.
		//		 This would allow creation of State on external object (like a Service) which would be weird.
		//if (key.Target is not ISourceContextAware)
		if (key.Target is null)
		{
			throw new InvalidOperationException($"The delegate provided in the Command.{caller} must not be a static method.");
		}

		return key;
	}

	private static IListState<T> S<TOwner>(TOwner owner, IListFeed<T> feed)
		where TOwner : class
		// We make sure to use the SourceContext to create the State, so it will be disposed with the context.
		=> SourceContext.GetOrCreate(owner).GetOrCreateListState(feed);

	private static IListState<T> S<TOwner>(TOwner owner, IFeed<IImmutableList<T>> feed)
		where TOwner : class
	// We make sure to use the SourceContext to create the State, so it will be disposed with the context.
		=> SourceContext.GetOrCreate(owner).GetOrCreateListState(feed);
}
