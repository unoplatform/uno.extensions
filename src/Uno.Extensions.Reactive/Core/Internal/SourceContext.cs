using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// A context used to cache subscriptions to <see cref="IFeed{T}"/> for a given owner.
/// </summary>
public sealed class SourceContext : IAsyncDisposable
{
	private static readonly SourceContext _none = new(isNone: true);
	private static readonly AsyncLocal<SourceContext> _current = new();
	private static readonly ConditionalWeakTable<object, SourceContext> _contexts = new();

	/// <summary>
	/// Gets the current context.
	/// </summary>
	public static SourceContext Current => _current.Value ?? _none;

	/// <summary>
	/// Try to get the context of a given owner.
	/// </summary>
	/// <param name="owner">The owner.</param>
	/// <returns>The context of the owner, or null if none created yet.</returns>
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

	/// <summary>
	/// Gets or create the context of a given owner.
	/// </summary>
	/// <param name="owner">The owner.</param>
	/// <returns>The context of the owner.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="owner"/> is null</exception>
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

	/// <summary>
	/// Explicitly set the context to a given owner.
	/// </summary>
	/// <param name="owner">The owner.</param>
	/// <param name="ctx">The context to set.</param>
	/// <exception cref="InvalidOperationException">The <paramref name="owner"/> already has a context attached.</exception>
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

	/// <summary>
	/// A <see cref="CancellationToken"/> associated to the owner.
	/// </summary>
	public CancellationToken Token { get; }

	/// <summary>
	/// Sets the context as <see cref="Current"/>.
	/// </summary>
	/// <returns>A disposable that can be used to resign current.</returns>
	/// <exception cref="ObjectDisposedException">The context has already been disposed.</exception>
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

	/// <summary>
	/// Get or create a cached with replay async enumeration of messages produced by the given feed.
	/// </summary>
	/// <typeparam name="T">Type of the value of feed.</typeparam>
	/// <param name="feed">The feed to get source from.</param>
	/// <returns>The cached with replay async enumeration of messages produced by the given feed</returns>
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

	/// <summary>
	/// Get or create a <see cref="IState{T}"/> for a given feed.
	/// </summary>
	/// <typeparam name="T">Type of the value of feed.</typeparam>
	/// <param name="feed">The feed to get source from.</param>
	/// <returns>The state wrapping the given feed</returns>
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

	/// <summary>
	/// A disposable that can be used to resign current.
	/// </summary>
	public readonly struct CurrentSubscription : IDisposable
	{
		private readonly SourceContext _ctx;
		private readonly SourceContext _previous;

		internal CurrentSubscription(SourceContext ctx)
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
