using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Core;

internal class StateStore : IStateStore
{
	private readonly SourceContext _owner;
	private Dictionary<object, IAsyncDisposable>? _states = new();

	public StateStore(SourceContext owner)
	{
		_owner = owner;
	}

	public TState GetOrCreateState<TSource, TState>(TSource source, Func<SourceContext, TSource, TState> factory)
		where TSource : class
		where TState : IStateImpl, IAsyncDisposable
	{
		var states = _states;
		if (states is null)
		{
			throw new ObjectDisposedException(nameof(SourceContext));
		}

		if (source is TState state && state.Context.States == this)
		{
			return state;
		}

		lock (states)
		{
			state = states.TryGetValue(source, out var existing)
				? (TState)existing
				: (TState)(states[source] = factory(_owner, source));
		}

		if (_states is null) // The context has been disposed while we where creating the State ...
		{
			_ = state.DisposeAsync();
			throw new ObjectDisposedException(nameof(SourceContext));
		}

		return state;
	}

	public TState CreateState<T, TState>(Option<T> initialValue, Func<SourceContext, Option<T>, TState> factory)
		where TState : IStateImpl, IAsyncDisposable
	{
		var states = _states;
		if (states is null)
		{
			throw new ObjectDisposedException(nameof(SourceContext));
		}

		TState state;
		lock (states)
		{
			// Note: we use the **boxed** initialValue as key for the states cache,
			//		 but it's only to have a key, it's not expected to be retrieved,
			//		 we keep it only for dispose.
			states[initialValue] = state = factory(_owner, initialValue);
		}

		if (_states is null) // The context has been disposed while we where creating the State ...
		{
			_ = state.DisposeAsync();
			throw new ObjectDisposedException(nameof(SourceContext));
		}

		return state;
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		var states = Interlocked.Exchange(ref _states, null);
		if (states is null or { Count: 0 })
		{
			return; // already disposed
		}

		Task disposeAsync;
		lock (states)
		{
			disposeAsync = CompositeAsyncDisposable.DisposeAll(states.Values);
		}

		await disposeAsync.ConfigureAwait(false);
	}
}
