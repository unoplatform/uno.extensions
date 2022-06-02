using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// A cache of <see cref="IState{T}"/> used by a <see cref="SourceContext"/>.
/// </summary>
internal interface IStateStore : IAsyncDisposable
{
	/// <summary>
	/// Get or create a <see cref="IState{T}"/> for a given feed.
	/// </summary>
	/// <typeparam name="TSource">Type of the source feed.</typeparam>
	/// <typeparam name="TState">The requested type of state.</typeparam>
	/// <param name="source">The source feed.</param>
	/// <param name="factory">Factory to build the state is not present yet in the cache.</param>
	/// <returns>The state wrapping the given feed</returns>
	/// <exception cref="ObjectDisposedException">This store has been disposed.</exception>
	TState GetOrCreateState<TSource, TState>(TSource source, Func<SourceContext, TSource, TState> factory)
		where TSource : class
		where TState : IStateImpl, IAsyncDisposable;

	/// <summary>
	/// Create a <see cref="IState{T}"/> for a given value.
	/// </summary>
	/// <typeparam name="T">Type of the value of items.</typeparam>
	/// <param name="initialValue">The initial value of the state</param>
	/// <returns>The list state wrapping the given list feed</returns>
	/// <exception cref="ObjectDisposedException">This store has been disposed.</exception>
	IState<T> CreateState<T>(Option<T> initialValue);
}
