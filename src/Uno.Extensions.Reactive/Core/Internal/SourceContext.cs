using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

	#region Context factories
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

	/// <summary>
	/// Creates a child context given a request source.
	/// </summary>
	/// <param name="requests">The request source to use for the new context.</param>
	/// <returns>A new child SourceContext.</returns>
	internal SourceContext CreateChild(IRequestSource requests)
		=> new(this, requests: requests);
	#endregion

	private static long _nextRootId = 1;

	private readonly bool _isNone;
	private readonly CancellationTokenSource? _ct;

	private readonly IStateStore? _localStates;
	private readonly IRequestSource? _localRequests;

	// Creates a root (or none) context
	private SourceContext(bool isNone = false)
	{
		if (isNone)
		{
			_isNone = isNone;

			States = new NoneStateStore();
			RequestSource = new NoneRequestSource();
		}
		else
		{
			_ct = new CancellationTokenSource();

			Token = _ct.Token;
			RootId = (uint)Interlocked.Increment(ref _nextRootId);
			States = _localStates = new StateStore(this);
			RequestSource = _localRequests = new NoneRequestSource(); // Currently we do not support messages directly on the root, using None allows AsyncFeed to complete enumeration
		}
	}

	// Creates a sub context
	private SourceContext(SourceContext parent, IStateStore? states = null, IRequestSource? requests = null)
	{
		if (parent._isNone)
		{
			throw new InvalidOperationException("Cannot create a sub context from None context.");
		}

		_ct = CancellationTokenSource.CreateLinkedTokenSource(parent.Token);
		_localStates = states;
		_localRequests = requests;

		Token = _ct.Token;
		RootId = parent.RootId;
		States = states ?? parent.States;
		RequestSource = requests ?? parent.RequestSource;
	}

	/// <summary>
	/// A <see cref="CancellationToken"/> associated to the owner.
	/// </summary>
	public CancellationToken Token { get; }

	/// <summary>
	/// Gets an identifier of the root context.
	/// </summary>
	internal uint RootId { get; }

	/// <summary>
	/// The states store that holds all the states linked to this current context.
	/// </summary>
	/// <remarks>Usually this is inherited from the root context.</remarks>
	internal IStateStore States { get; }

	/// <summary>
	/// The request source that can be used to send request to the feed subscribed using that context
	/// WARNING: This internally only debug purposes, you should never use it to send a request on an existing context.
	/// </summary>
	/// <remarks>
	/// The default implementation from the root does not allow to send any request,
	/// you have to create your own context if you want to send request.
	/// </remarks>
	internal IRequestSource RequestSource { get; }

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

		if (Token.IsCancellationRequested)
		{
			throw new ObjectDisposedException(nameof(SourceContext));
		}

		return new(this);
	}

	#region State factories
	/// <summary>
	/// Get or create a cached with replay async enumeration of messages produced by the given feed.
	/// </summary>
	/// <typeparam name="T">Type of the value of feed.</typeparam>
	/// <param name="feed">The feed to get source from.</param>
	/// <returns>The cached with replay async enumeration of messages produced by the given feed</returns>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public IAsyncEnumerable<Message<T>> GetOrCreateSource<T>(IFeed<T> feed)
	{
		if (_isNone)
		{
			this.Log().Warn(
				$"[PERFORMANCE HIT] Awaiting a feed '{feed}' outside of a valid SourceContext (None). "
				+ "This creates a new **detached** subscription to the feed, which has a negative performance impact, "
				+ "and which might even re-execute some HTTP requests.");

			return feed.GetSource(this, Token);
		}
		else
		{
			return GetOrCreateStateCore(feed).GetSource(Token);
		}
	}

	/// <summary>
	/// Get or create a cached with replay async enumeration of messages produced by the given feed.
	/// </summary>
	/// <typeparam name="T">Type of the value of feed.</typeparam>
	/// <param name="feed">The feed to get source from.</param>
	/// <returns>The cached with replay async enumeration of messages produced by the given feed</returns>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public IAsyncEnumerable<Message<IImmutableList<T>>> GetOrCreateSource<T>(IListFeed<T> feed)
	{
		if (_isNone)
		{
			this.Log().Warn(
				$"[PERFORMANCE HIT] Awaiting a feed '{feed}' outside of a valid SourceContext (None). "
				+ "This creates a new **detached** subscription to the feed, which has a negative performance impact, "
				+ "and which might even re-execute some HTTP requests.");

			return feed.AsFeed().GetSource(this, Token);
		}
		else
		{
			return GetOrCreateStateCore(feed.AsFeed()).GetSource(Token);
		}
	}

	/// <summary>
	/// Get or create a <see cref="IState{T}"/> for a given feed.
	/// </summary>
	/// <typeparam name="T">Type of the value of feed.</typeparam>
	/// <param name="feed">The feed to get source from.</param>
	/// <returns>The state wrapping the given feed</returns>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public IState<T> GetOrCreateState<T>(IFeed<T> feed)
		=> GetOrCreateStateCore(feed);

	/// <summary>
	/// Get or create a <see cref="IListState{T}"/> for a given list feed.
	/// </summary>
	/// <typeparam name="T">Type of the value of items.</typeparam>
	/// <param name="feed">The list feed to get source from.</param>
	/// <returns>The list state wrapping the given list feed</returns>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public IListState<T> GetOrCreateListState<T>(IListFeed<T> feed)
		=> new ListStateImpl<T>(GetOrCreateStateCore(feed.AsFeed()));

	/// <summary>
	/// Get or create a <see cref="IListState{T}"/> for a given list feed.
	/// </summary>
	/// <typeparam name="T">Type of the value of items.</typeparam>
	/// <param name="feed">The list feed to get source from.</param>
	/// <returns>The list state wrapping the given list feed</returns>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public IListState<T> GetOrCreateListState<T>(IFeed<IImmutableList<T>> feed)
		=> new ListStateImpl<T>(GetOrCreateStateCore(feed));

	private StateImpl<T> GetOrCreateStateCore<T>(IFeed<T> feed)
		=> States.GetOrCreateState(feed, (ctx, f) => new StateImpl<T>(ctx, f));

	/// <summary>
	/// Create a <see cref="IState{T}"/> for a given value.
	/// </summary>
	/// <typeparam name="T">Type of the value of items.</typeparam>
	/// <param name="initialValue">The initial value of the state</param>
	/// <returns>The list state wrapping the given list feed</returns>
	/// <exception cref="ObjectDisposedException">This context has been disposed.</exception>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public IState<T> CreateState<T>(Option<T> initialValue)
		=> States.CreateState(initialValue);

	/// <summary>
	/// Create a <see cref="IState{T}"/> for a given value.
	/// </summary>
	/// <typeparam name="T">Type of the value of items.</typeparam>
	/// <param name="initialValue">The initial value of the state</param>
	/// <returns>The list state wrapping the given list feed</returns>
	/// <exception cref="ObjectDisposedException">This context has been disposed.</exception>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public IListState<T> CreateListState<T>(Option<IImmutableList<T>> initialValue)
		=> new ListStateImpl<T>(CreateState(initialValue));
	#endregion

	#region Requests
	/// <summary>
	/// Listen for some specific request sent by a subscriber
	/// </summary>
	/// <typeparam name="T">Type of the requests to listen to.</typeparam>
	/// <returns>An async enumerable sequence of requests.</returns>
	internal IAsyncEnumerable<T> Requests<T>()
		where T : IContextRequest
		=> RequestSource.OfType<T>();
	#endregion

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_isNone)
		{
			return;
		}

		_ct!.Cancel();

		// Note: We make sure dispose only the values explicitly defined on this context,
		//		 but not those that are inherited from the parent context.
		await (_localStates?.DisposeAsync() ?? default);
		_localRequests?.Dispose();
	}

	~SourceContext()
	{
		DisposeAsync();
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
