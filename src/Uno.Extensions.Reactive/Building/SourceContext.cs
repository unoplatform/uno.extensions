using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Utils;
using Uno.Extensions.Reactive.Utils.Logging;

namespace Uno.Extensions.Reactive;

[EditorBrowsable(EditorBrowsableState.Advanced)]
public interface ISourceContextAware
{
}

public sealed class SourceContext : IAsyncDisposable
{
	private static readonly SourceContext _none = new(isNone: true);
	private static readonly AsyncLocal<SourceContext> _current = new();
	private static readonly ConditionalWeakTable<object, SourceContext> _contexts = new();

	public static SourceContext Current => _current.Value ?? _none;

	[Pure]
	public static SourceContext? Find(object? owner)
	{
		if (owner is null)
		{
			return default;
		}

		_contexts.TryGetValue(owner, out var ctx);
		return ctx;
	}

	public static SourceContext GetOrCreate(object owner)
	{
		owner = owner ?? throw new ArgumentNullException(nameof(owner));

		if (_contexts.TryGetValue(owner, out var ctx))
		{
			return ctx;
		}

		lock (_contexts)
		{
			ctx = new SourceContext();
			if (owner is not ISourceContextAware)
			{
				ConditionalDisposable.Link(owner, ctx);
			}

			_contexts.Add(owner, ctx);
		}

		return ctx;
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static void Set(object owner, SourceContext ctx)
	{
		if (_contexts.TryGetValue(owner, out _))
		{
			throw new InvalidOperationException($"'{owner}' already has a context attached.");
		}

		_contexts.Add(owner, ctx);
	}

	private readonly bool _isNone;
	private readonly CancellationTokenSource? _ct;
	private Dictionary<object, IAsyncDisposable>? _states;

	private SourceContext(bool isNone = false)
	{
		_isNone = isNone;

		if (!_isNone)
		{
			_states = new();
			_ct = new();

			Token = _ct.Token;
		}
	}

	public CancellationToken Token { get; }

	public CurrentSubscription AsCurrent()
	{
		if (_isNone)
		{
			return new(this);
		}

		if (_states is null)
		{
			throw new ObjectDisposedException(nameof(SourceContext));
		}

		return new(this);
	}

	public IAsyncEnumerable<Message<T>> GetOrCreateSource<T>(IFeed<T> feed)
	{
		if (_isNone)
		{
			this.Log().Warn(
				$"[PERFORMANCE HIT] Awaiting a feed '{feed}' outside of a valid SourceContext (None). "
				+ "This creates a new **detached** subscription to the feed, which has a negative performance impact, "
				+ "and which might even re-execute some HTTP requests.");

			return feed.GetSource(this);
		}
		else
		{
			return GetOrCreateStateCore(feed).GetSource(_ct!.Token);
		}
	}

	public IState<T> GetOrCreateState<T>(IFeed<T> feed)
		=> GetOrCreateStateCore(feed);

	private State<T> GetOrCreateStateCore<T>(IFeed<T> feed)
	{
		if (_isNone)
		{
			throw new InvalidOperationException("Cannot create a state on SourceContext.None");
		}

		var states = _states;
		if (states is null)
		{
			throw new ObjectDisposedException(nameof(SourceContext));
		}

		State<T> state;
		lock (states)
		{
			state = states.TryGetValue(feed, out var existing)
				? (State<T>)existing
				: (State<T>)(states[feed] = new State<T>(this, feed));
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
		if (_isNone)
		{
			return;
		}

		_ct!.Cancel();

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

	public readonly struct CurrentSubscription : IDisposable
	{
		private readonly SourceContext _ctx;
		private readonly SourceContext _previous;

		public CurrentSubscription(SourceContext ctx)
		{
			_ctx = ctx;

			_previous = _current.Value;
			_current.Value = ctx;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (_current.Value == _ctx)
			{
				_current.Value = _previous;
			}
		}
	}
}
