using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Utils;
using Uno.Extensions.Reactive.Utils.Debugging;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// A context used to cache subscriptions to <see cref="IFeed{T}"/> for a given owner.
/// </summary>
public sealed class SourceContext : IAsyncDisposable
{
	internal const string NoneContextErrorDesc =
		"This error usually means that you are trying to interact with feeds, typically awaiting a feed, out of a valid SourceContext."
		+ "The SourceContext holds the state of feeds and is accessible through the static SourceContext.Current, "
		+ "but is valid only within the scope of an async operation invoked by feeds (like Command.Execute or Feed.SelectAsync)."
		+ "You have to ensure that a valid context is set before doing any application async operation using the SourceContext.AsCurrent()."
		+ "If you are implementing your own operator, use the context you get in the GetSource before invoking any async operation."
		+ "If you are trying to react to an external source within a VM, use context of that VM which can be accessed using the static SourceContext.GetOrCreate(VM)."
		+ "More info: https://github.com/unoplatform/uno.extensions/blob/main/doc/Reference/Reactive/Reactive%20-%20Dev.md";

	private static readonly SourceContext _none = new();
	private static readonly AsyncLocal<SourceContext> _current = new();
	private static readonly ConditionalWeakTable<object, SourceContext> _contexts = new();

	/// <summary>
	/// Gets the none context.
	/// </summary>
	internal static SourceContext None => _none;

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

		if (_preConfiguration?.AppliesTo(owner, out ctx) ?? false)
		{
			_contexts.Add(owner, ctx);
			return ctx;
		}

		return _contexts.GetValue(owner, Create);

		static SourceContext Create(object o)
		{
			var newCtx = new SourceContext(RootOwner.Create(o));
			if (o is not ISourceContextAware)
			{
				ConditionalDisposable.Link(o, newCtx);
			}

			return newCtx;
		}
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
		if (_contexts.TryGetValue(owner, out var current))
		{
			if (current == ctx)
			{
				return; // 'ctx' is already the context defined for the 'owner', nothing to do!
			}

			throw new InvalidOperationException($"'{owner}' already has a context attached.");
		}

		_contexts.Add(owner, ctx);
	}

	/// <summary>
	/// Explicitly preconfigure the context to use for the given type.
	/// </summary>
	/// <param name="ownerType">Type of the owner to configure.</param>
	/// <param name="ctx">The context to use for the next instance of <paramref name="ownerType"/>.</param>
	/// <remarks>This method allow </remarks>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static PreConfiguration PreConfigure(Type ownerType, SourceContext ctx)
		=> _preConfiguration = new PreConfiguration(ownerType, ctx, _preConfiguration);

	[ThreadStatic]
	private static PreConfiguration? _preConfiguration = default;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="TargetType"></param>
	/// <param name="Context"></param>
	/// <param name="Previous"></param>
	internal record PreConfiguration(Type TargetType, SourceContext Context, PreConfiguration? Previous) : IDisposable
	{
		internal bool AppliesTo(object target, [NotNullWhen(true)] out SourceContext? context)
		{
			if (target.GetType() == TargetType)
			{
				context = Context;
				Dispose(); // Configuration applies only to the first instance!
				return true;
			}
			else
			{
				context = default;
				return false;
			}
		}

		/// <summary>
		/// Makes sure that the pre-configuration is applied to the given owner.
		/// </summary>
		/// <param name="target"></param>
		public void EnsureApplied(object target)
			=> Set(target, Context);

		/// <inheritdoc />
		public void Dispose()
		{
			if (_preConfiguration == this)
			{
				_preConfiguration = Previous;
			}
		}
	}

	/// <summary>
	/// Creates a child context given a request source.
	/// </summary>
	/// <param name="owner">The info about the owner that it creating a child context</param>
	/// <param name="requests">The request source to use for the new context.</param>
	/// <returns>A new child SourceContext.</returns>
	internal SourceContext CreateChild(ISourceContextOwner owner, IRequestSource requests)
		=> new(this, owner, requests: requests);
	#endregion

	private static long _nextRootId = 1;

	private readonly bool _isNone;
	private readonly CancellationTokenSource? _ct;

	private readonly IStateStore? _localStates;
	private readonly IRequestSource? _localRequests;

	// Create the "None" context
	private SourceContext()
	{
		_isNone = true;

		Owner = new NoneOwner();
		States = new NoneStateStore();
		RequestSource = new NoneRequestSource();
	}

	// Creates a root (or none) context
	private SourceContext(RootOwner ownerInfo)
	{
		_ct = new CancellationTokenSource();
		Owner = ownerInfo;

		Token = _ct.Token;
		RootId = (uint)Interlocked.Increment(ref _nextRootId);
		States = _localStates = new StateStore(this);
		RequestSource = _localRequests = new NoneRequestSource(); // Currently we do not support messages directly on the root, using None allows AsyncFeed to complete enumeration
	}

	// Creates a sub context
	private SourceContext(SourceContext parent, ISourceContextOwner owner, IStateStore? states = null, IRequestSource? requests = null)
	{
		if (parent._isNone)
		{
			throw new InvalidOperationException("Cannot create a sub context from None context. " + NoneContextErrorDesc);
		}

		_ct = CancellationTokenSource.CreateLinkedTokenSource(parent.Token);
		_localStates = states;
		_localRequests = requests;

		Token = _ct.Token;
		RootId = parent.RootId;
		Parent = parent;
		Owner = owner;
		States = states ?? parent.States; // Note: A child StateStore should forward request to its parent store!
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
	/// The parent context, if any.
	/// </summary>
	internal SourceContext? Parent { get; }

	/// <summary>
	/// Gets information about the owner of this context
	/// </summary>
	internal ISourceContextOwner Owner { get; }

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
	public IAsyncEnumerable<Message<T>> GetOrCreateSource<T>(ISignal<Message<T>> feed)
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
			return States.GetOrCreateSubscription(feed).GetMessages(this, Token);
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
		=> States.GetOrCreateState<IFeed<T>, IState<T>>(feed, static (ctx, f) => new StateImpl<T>(ctx, f));

	/// <summary>
	/// Get or create a <see cref="IListState{T}"/> for a given list feed.
	/// </summary>
	/// <typeparam name="T">Type of the value of items.</typeparam>
	/// <param name="feed">The list feed to get source from.</param>
	/// <returns>The list state wrapping the given list feed</returns>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public IListState<T> GetOrCreateListState<
		[DynamicallyAccessedMembers(ListFeed.TRequirements)]
		T
	>(IListFeed<T> feed)
		=> States.GetOrCreateState<IListFeed<T>, IListState<T>>(feed, static (ctx, f) => new ListStateImpl<T>((StateImpl<IImmutableList<T>>)ctx.GetOrCreateState(f.AsFeed())));

	/// <summary>
	/// Get or create a <see cref="IListState{T}"/> for a given list feed.
	/// </summary>
	/// <typeparam name="T">Type of the value of items.</typeparam>
	/// <param name="feed">The list feed to get source from.</param>
	/// <returns>The list state wrapping the given list feed</returns>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public IListState<T> GetOrCreateListState<
		[DynamicallyAccessedMembers(ListFeed.TRequirements)]
		T
	>(IFeed<IImmutableList<T>> feed)
		=> States.GetOrCreateState<IFeed<IImmutableList<T>>, IListState<T>>(feed, static (ctx, f) => new ListStateImpl<T>((StateImpl<IImmutableList<T>>)ctx.GetOrCreateState(f)));

	// WARNING: DO NOT USE, this breaks the cache by providing a custom config!
	// We need to make those config "upgradable" in order to properly share the instances of State
	internal ListStateImpl<T> DoNotUse_GetOrCreateListState<
		[DynamicallyAccessedMembers(ListFeed.TRequirements)]
		T
	>(IListFeed<T> feed, StateUpdateKind updatesKind)
		=> States.GetOrCreateState(feed, /*static*/ (ctx, f) => new ListStateImpl<T>(new StateImpl<IImmutableList<T>>(ctx, f.AsFeed(), updatesKind: updatesKind)));

	/// <summary>
	/// Create a <see cref="IState{T}"/> for a given value.
	/// </summary>
	/// <typeparam name="T">Type of the value of items.</typeparam>
	/// <param name="initialValue">The initial value of the state</param>
	/// <returns>The list state wrapping the given list feed</returns>
	/// <exception cref="ObjectDisposedException">This context has been disposed.</exception>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public IState<T> CreateState<T>(Option<T> initialValue)
		=> States.CreateState<T, IState<T>>(initialValue, static (ctx, iv) => new StateImpl<T>(ctx, iv));

	/// <summary>
	/// Create a <see cref="IState{T}"/> for a given value.
	/// </summary>
	/// <typeparam name="T">Type of the value of items.</typeparam>
	/// <param name="initialValue">The initial value of the state</param>
	/// <returns>The list state wrapping the given list feed</returns>
	/// <exception cref="ObjectDisposedException">This context has been disposed.</exception>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public IListState<T> CreateListState<
		[DynamicallyAccessedMembers(ListFeed.TRequirements)]
		T
	>(Option<IImmutableList<T>> initialValue)
		=> States.CreateState<IImmutableList<T>, IListState<T>>(initialValue, static (ctx, iv) => new ListStateImpl<T>((StateImpl<IImmutableList<T>>)ctx.CreateState(iv)));
	#endregion

	#region Requests
	/// <summary>
	/// Listen for some specific request sent by a subscriber
	/// </summary>
	/// <typeparam name="T">Type of the requests to listen to.</typeparam>
	/// <returns>An async enumerable sequence of requests.</returns>
	internal void Requests<T>(Action<T> callback, CancellationToken ct)
	{
		RequestSource.RequestRaised += OnRequest;
		ct.Register(() => RequestSource.RequestRaised -= OnRequest);

		void OnRequest(object? _, IContextRequest request)
		{
			if (request is EndRequest)
			{
				RequestSource.RequestRaised -= OnRequest;
			}
			if (request is T req)
			{
				callback(req);
			}
		}
	}
	#endregion

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		try
		{
			if (_isNone)
			{
				return;
			}

			_ct!.Cancel();

			// Note: We make sure dispose only the values explicitly defined on this context,
			//		 but not those that are inherited from the parent context.
			await (_localStates?.DisposeAsync() ?? default).ConfigureAwait(false);
			_localRequests?.Dispose();
		}
		catch (Exception) { }
	}

	/// <inheritdoc />
	~SourceContext()
	{
#pragma warning disable CS4014 // Call not awaited: The dispose cannot throw
		DisposeAsync();
#pragma warning restore CS4014
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

			_previous = _current.Value ?? SourceContext.None;
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

	private record RootOwner(string Name) : ISourceContextOwner
	{
		public static RootOwner Create(object owner)
			=> new(DebugConfiguration.IsDebugging ? owner.GetType().Name + " - " + owner.GetHashCode().ToString("X8") : "-debugging disabled-");

		public IDispatcher? Dispatcher => null;
	}

	private record NoneOwner : ISourceContextOwner
	{
		public string Name => "None context";
		public IDispatcher? Dispatcher => null;
	}
}
