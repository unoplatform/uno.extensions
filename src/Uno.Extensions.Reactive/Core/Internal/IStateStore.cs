using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// A cache of <see cref="IState{T}"/> and <see cref="FeedSubscription{T}"/> used by a <see cref="SourceContext"/>.
/// </summary>
/// <remarks>
/// This is the class responsible to that hold the "state" (the generic term, i.e. a persistent value) of the subscriptions made by the owner on feeds.
/// </remarks>
internal interface IStateStore : IAsyncDisposable
{
	/// <summary>
	/// For test purposes only - indicates if there is an active subscription for the given source
	/// </summary>
	/// <typeparam name="TSource">Type of the source feed.</typeparam>
	/// <param name="source">The source feed.</param>
	/// <returns>Tue if a states has been created for the given source, false otherwise.</returns>
	internal bool HasSubscription<TSource>(TSource source)
		where TSource : class;

	/// <summary>
	/// Get or create a <see cref="FeedSubscription{T}"/> for a given feed.
	/// </summary>
	/// <typeparam name="TSource">Type of the source feed.</typeparam>
	/// <typeparam name="TValue">Type of the values of the <paramref name="source"/>.</typeparam>
	/// <param name="source">The source feed.</param>
	/// <returns>The subscription of the given feed</returns>
	/// <exception cref="ObjectDisposedException">This store has been disposed.</exception>
	FeedSubscription<TValue> GetOrCreateSubscription<TSource, TValue>(TSource source)
		where TSource : class, ISignal<Message<TValue>>;

	/// <summary>
	/// Get or create a <see cref="IState{T}"/> for a given feed.
	/// </summary>
	/// <typeparam name="TSource">Type of the source feed.</typeparam>
	/// <typeparam name="TState">The requested type of state.</typeparam>
	/// <param name="source">The source feed.</param>
	/// <param name="factory">Factory to build the state is not present yet in the cache.</param>
	/// <returns>The state wrapping the given feed</returns>
	/// <exception cref="ObjectDisposedException">This store has been disposed.</exception>
	/// <remarks>
	/// If the the returned state makes any subscription to a feed,
	/// it's expected that it will share that subscription with other subscribers of the current context (i.e. it uses the <see cref="GetOrCreateSubscription{TSource,TValue}"/>).
	/// </remarks>
	TState GetOrCreateState<TSource, TState>(TSource source, Func<SourceContext, TSource, TState> factory)
		where TSource : class
		where TState : IState;

	/// <summary>
	/// Create a <see cref="IState{T}"/> for a given value.
	/// </summary>
	/// <typeparam name="T">Type of the value of items.</typeparam>
	/// <typeparam name="TState">The requested type of state.</typeparam>
	/// <param name="initialValue">The initial value of the state.</param>
	/// <param name="factory">Factory to build the state.</param>
	/// <returns>A new state initialized with given initial value</returns>
	/// <exception cref="ObjectDisposedException">This store has been disposed.</exception>
	TState CreateState<T, TState>(Option<T> initialValue, Func<SourceContext, Option<T>, TState> factory)
		where TState : IState;
}
