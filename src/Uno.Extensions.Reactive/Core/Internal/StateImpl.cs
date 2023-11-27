using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Config;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive.Core;

internal sealed class StateImpl<T> : IState<T>, IFeed<T>, IAsyncDisposable, IStateImpl, IHotSwapState<T>
{
	private readonly SubscriptionMode _mode;
	private readonly StateUpdateKind _updatesKind;
	private /*readonly - but hot-reload*/ UpdateFeed<T> _inner;
	private readonly HotSwapFeed<T>? _hotSwap;

	private FeedSubscription<T>? _subscription;
	private IDisposable? _subscriptionMode;

	/// <summary>
	/// Gets the context to which this state belongs.
	/// </summary>
	public SourceContext Context { get; }
	SourceContext IStateImpl.Context => Context;

	internal Message<T> Current => _subscription?.Current ?? Message<T>.Initial;

	/// <summary>
	/// Gets direct access to the underlying UpdateFeed so we can have full control of update operation made on it.
	/// </summary>
	internal UpdateFeed<T> Inner => _inner;

	/// <summary>
	/// Legacy - Used only be legacy IInput syntax
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public StateImpl(Option<T> defaultValue)
		: this(SourceContext.None, defaultValue)
	{
	}

	public StateImpl(SourceContext context, Option<T> defaultValue)
		: this(context, new AsyncFeed<T>(async _ => defaultValue), SubscriptionMode.Eager)
	{
	}

	public StateImpl(
		SourceContext context,
		IFeed<T> feed,
		SubscriptionMode mode = SubscriptionMode.Default,
		StateUpdateKind updatesKind = StateUpdateKind.Volatile)
	{
		Context = context;

		_mode = mode;
		_updatesKind = updatesKind;

		if (FeedConfiguration.EffectiveHotReload.HasFlag(HotReloadSupport.State))
		{
			// It's valid to use the HotSwap feed here, as we are caching it internally and the subscription is managed by the State itself on its own Context.
			feed = _hotSwap = new HotSwapFeed<T>(feed);
		}
		_inner = new UpdateFeed<T>(feed);

		if (updatesKind is StateUpdateKind.Persistent)
		{
			// If updates has to be persistent, the subscription to the UpdateFeed must remain active.
			_mode &= ~SubscriptionMode.RefCounted;
		}

		if (mode.HasFlag(SubscriptionMode.Eager))
		{
			// Note: Once the dynamic updates of the subscription mode in supported in FeedSubscription,
			//		 we will be able to unconditionally create the _subscription and just push the mode on it instead!
			Enable();
		}
	}

	void IHotSwapState<T>.HotSwap(IFeed<T>? source)
	{
		if (source is IState<T>)
		{
			if (source is StateImpl<T> state)
			{
				// Switch the _inner so when push a new update, it will actually be pushed to the new state.
				// TODO: Should we also transfer the current updates? 
				_inner = state._inner;
			}
			else if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info("Cannot hot swap a State that is not a StateImpl. Changes made on the current implementation won't be propagated to the new instance (but changes made on new instance will be visible in previous instance.)");
			}
		}

		// If source is a state, we will still use it as source/parent.
		// Changes made on it will be treated as parent feed update and will erase our local changes (unless persistent and compatible) which is fine.
		_hotSwap?.Set(source);
	}

	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct = default)
	{
		Enable();

		// Note: The subscription has been created using our own Context, and we forward the subscriber context only for requests propagation.
		return _subscription!.GetMessages(context, ct);
	}

	/// <inheritdoc />
	public async ValueTask UpdateMessageAsync(Action<MessageBuilder<T>> updater, CancellationToken ct)
	{
		// First we make sure that the UpdateFeed is active, so the update will be applied ^^
		Enable();

		var update = new Update(updater, _updatesKind);
		_inner.Add(update);
		await update.HasBeenApplied; // Makes sure to forward (the first) error to the caller if any.
	}

	private void Enable()
	{
		if (_subscription is not null)
		{
			return;
		}

		// Note: The subscription has to be created using our own Context, not the one of our subscribers.
		var subscription = Context.States.GetOrCreateSubscription(_inner);
		if (Interlocked.CompareExchange(ref _subscription, subscription, null) is null)
		{
			_subscriptionMode = _subscription.UpdateMode(_mode);
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync() 
	{
		// Note: As the _innerFeed as been created by us, we dispose the _subscription (even it belongs to the StateStore)
		//		 in order to make sure to release the original underlying feed.
		//		 This is a temporary patch until we support dynamic updates of the subscription mode in FeedSubscription (i.e. the _subscriptionMode).
		if (_subscription is { } sub)
		{
			await sub.DisposeAsync();
		}
		_subscriptionMode?.Dispose();
	}

	private record Update(Action<MessageBuilder<T>> Method, StateUpdateKind Kind) : IFeedUpdate<T>
	{
		private readonly TaskCompletionSource<object?> _firstResult = new();

		public Task HasBeenApplied => _firstResult.Task;

		/// <inheritdoc />
		public bool IsActive(Message<T>? parent, bool parentChanged, IMessageEntry<T> entry)
			=> !_firstResult.Task.IsFaulted
				&& (Kind is StateUpdateKind.Persistent || !parentChanged || !_firstResult.Task.IsCompleted);

		/// <inheritdoc />
		public void Apply(bool _, MessageBuilder<T, T> message)
		{
			try
			{
				Method(new(message.Get, ((IMessageBuilder)message).Set));
				_firstResult.TrySetResult(null);
			}
			catch (Exception error) when (!_firstResult.Task.IsCompleted) // Otherwise let the exception bubble up.
			{
				_firstResult.TrySetException(error);
			}
		}
	}
}
