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
	private readonly SourceContext _root;
	private Dictionary<object, IAsyncDisposable>? _states = new();
	private Dictionary<object, IAsyncDisposable>? _subscriptions = new();

	public StateStore(SourceContext root)
	{
		_root = root;
	}

	/// <inheritdoc />
	bool IStateStore.HasSubscription<TSource>(TSource source)
	{
		var subscriptions = _subscriptions;
		if (subscriptions is null)
		{
			throw new ObjectDisposedException(nameof(SourceContext));
		}

		lock (subscriptions)
		{
			return subscriptions.ContainsKey(source);
		}
	}

	/// <inheritdoc />
	public FeedSubscription<T> GetOrCreateSubscription<T>(ISignal<Message<T>> source)
	{
		var subscriptions = _subscriptions;
		if (subscriptions is null)
		{
			throw new ObjectDisposedException(nameof(SourceContext));
		}

		FeedSubscription<T> subscription;
		lock (subscriptions)
		{
			subscription = (FeedSubscription<T>)(subscriptions.TryGetValue(source, out var existing)
				? existing
				: subscriptions[source] = new FeedSubscription<T>(source, _root));
		}

		if (_subscriptions is null) // The context has been disposed while we where creating the State ...
		{
			_ = subscription.DisposeAsync();
			throw new ObjectDisposedException(nameof(SourceContext));
		}

		return subscription;
	}

	public TState GetOrCreateState<TSource, TState>(TSource source, Func<SourceContext, TSource, TState> factory)
		where TSource : class
		where TState : IState
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
				: (TState)(states[source] = factory(_root, source));
		}

		if (_states is null) // The context has been disposed while we were creating the State ...
		{
			_ = state.DisposeAsync();
			throw new ObjectDisposedException(nameof(SourceContext));
		}

		return state;
	}

	public TState CreateState<T, TState>(Option<T> initialValue, Func<SourceContext, Option<T>, TState> factory)
		where TState : IState
	{
		var states = _states;
		if (states is null)
		{
			throw new ObjectDisposedException(nameof(SourceContext));
		}

		TState state;
		lock (states)
		{
			// Note: we use the 'state' as key for the states cache as it's not expected to be retrieved,
			//		 we keep it only for dispose.
			state = factory(_root, initialValue);
			states[state] = state;
		}

		if (_states is null) // The context has been disposed while we were creating the State ...
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
		var subscriptions = Interlocked.Exchange(ref _subscriptions, null);

		if (subscriptions is { Count: > 0 })
		{
			Task disposeAsync;
			lock (subscriptions)
			{
				disposeAsync = CompositeAsyncDisposable.DisposeAll(subscriptions.Values);
			}

			await disposeAsync.ConfigureAwait(false);
		}

		if (states is { Count: >0 })
		{
			Task disposeAsync;
			lock (states)
			{
				disposeAsync = CompositeAsyncDisposable.DisposeAll(states.Values);
			}

			await disposeAsync.ConfigureAwait(false);
		}
	}
}
