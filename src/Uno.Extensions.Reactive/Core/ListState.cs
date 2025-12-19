using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Provides a set of static methods to create and manipulate <see cref="IListState{T}"/>.
/// </summary>
public static partial class ListState
{
	#region Sources
	// Note: Those are helpers for which the T is set by type inference on provider.
	//		 We must have only one overload per method.

	/// <summary>
	/// Gets or creates a list state from a raw <see cref="IAsyncEnumerable{T}"/> sequence of <see cref="Uno.Extensions.Reactive.Message{T}"/>.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <typeparam name="TValue">The type of the value of the resulting feed.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="sourceProvider">The provider of the message enumerable sequence.</param>
	/// <returns>A state that encapsulate the source.</returns>
	public static IListState<TValue> Create<
		TOwner,
		[DynamicallyAccessedMembers(ListFeed.TRequirements)]
		TValue
	>(TOwner owner, Func<CancellationToken, IAsyncEnumerable<Message<IImmutableList<TValue>>>> sourceProvider)
		where TOwner : class
		=> ListState<TValue>.Create(owner, sourceProvider);

	/// <summary>
	/// Gets or creates a list state from a static initial list of items.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <typeparam name="TValue">The type of the value of the resulting feed.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The provider of the initial value of the state.</param>
	/// <returns>A state that encapsulate the source.</returns>
	public static IListState<TValue> Value<
		TOwner,
		[DynamicallyAccessedMembers(ListFeed.TRequirements)]
		TValue
	>(TOwner owner, Func<IImmutableList<TValue>> valueProvider)
		where TOwner : class
	// Note: We force the usage of delegate so 2 properties which are doing State.Value(this, () => 42) will effectively have 2 distinct states.
		=> ListState<TValue>.Value(owner, valueProvider);

	/// <summary>
	/// Gets or creates a list state from a static initial list of items.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <typeparam name="TValue">The type of the value of the resulting feed.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The provider of the initial value of the state.</param>
	/// <returns>A state that encapsulate the source.</returns>
	public static IListState<TValue> Value<
		TOwner,
		[DynamicallyAccessedMembers(ListFeed.TRequirements)]
		TValue
	>(TOwner owner, Func<ImmutableList<TValue>> valueProvider)
		where TOwner : class
	// Note: We force the usage of delegate so 2 properties which are doing State.Value(this, () => 42) will effectively have 2 distinct states.
		=> ListState<TValue>.Value(owner, valueProvider);

	/// <summary>
	/// Gets or creates a list state from an async method.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <typeparam name="TValue">The type of the value of the resulting feed.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A state that encapsulate the source.</returns>
	public static IListState<TValue> Async<
		TOwner,
		[DynamicallyAccessedMembers(ListFeed.TRequirements)]
		TValue
	>(TOwner owner, AsyncFunc<IImmutableList<TValue>> valueProvider, Signal? refresh = null)
		where TOwner : class
		=> ListState<TValue>.Async(owner, valueProvider, refresh);

	/// <summary>
	/// Gets or creates a list state from an async method.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <typeparam name="TValue">The type of the value of the resulting feed.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A state that encapsulate the source.</returns>
	public static IListState<TValue> Async<
		TOwner,
		[DynamicallyAccessedMembers(ListFeed.TRequirements)]
		TValue
	>(TOwner owner, AsyncFunc<ImmutableList<TValue>> valueProvider, Signal? refresh = null)
		where TOwner : class
		=> ListState<TValue>.Async(owner, valueProvider, refresh);

	/// <summary>
	/// Gets or creates a list state from an async enumerable sequence of list of items.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <typeparam name="TValue">The type of the value of the resulting feed.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A state that encapsulate the source.</returns>
	public static IListState<TValue> AsyncEnumerable<
		TOwner,
		[DynamicallyAccessedMembers(ListFeed.TRequirements)]
		TValue
	>(TOwner owner, Func<CancellationToken, IAsyncEnumerable<IImmutableList<TValue>>> enumerableProvider)
		where TOwner : class
		=> ListState<TValue>.AsyncEnumerable(owner, enumerableProvider);

	/// <summary>
	/// Gets or creates a state from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <typeparam name="TValue">The type of the value of the resulting state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="feed">The source list feed of the resulting list state.</param>
	/// <returns>A state that encapsulate the source.</returns>
	public static IListState<TValue> FromFeed<
		TOwner,
		[DynamicallyAccessedMembers(ListFeed.TRequirements)]
		TValue
	>(TOwner owner, IListFeed<TValue> feed)
		where TOwner : class
		=> ListState<TValue>.FromFeed(owner, feed);
	#endregion
}
