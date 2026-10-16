using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Core;

internal sealed class StateImpl<T> : IState<T>, IFeed<T>, IAsyncDisposable, IStateImpl
{
	private readonly SubscriptionMode _mode;
	private readonly StateUpdateKind _updatesKind;
	private readonly UpdateFeed<T> _inner;
	private StateImpl<T>? _updatesTarget;

	private FeedSubscription<T>? _subscription;
	private IDisposable? _subscriptionMode;

	private Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>>? _validator;
	private CancellationTokenSource? _validation;

	/// <summary>
	/// Gets the context to which this state belongs.
	/// </summary>
	public SourceContext Context { get; }
	SourceContext IStateImpl.Context => Context;

	/// <inheritdoc cref="IState{T}"/>
	public IRequestSource Requests
	{
		get
		{
			Enable();
			return _subscription.Requests;
		}
	}

	internal Message<T> Current => _subscription?.Current ?? Message<T>.Initial;

	private StateImpl<T> UpdatesTarget => _updatesTarget?.UpdatesTarget ?? this;

	/// <summary>
	/// Gets direct access to the underlying UpdateFeed so we can have full control of update operation made on it.
	/// </summary>
	internal UpdateFeed<T> Inner => UpdatesTarget._inner;

	/// <summary>
	/// Gets the feed this state was built over. Its updates apply on the context's subscription to that feed.
	/// </summary>
	internal IFeed<T> Source => _inner.Source;

	/// <summary>
	/// Legacy - Used only be legacy IInput syntax
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public StateImpl(Option<T> defaultValue)
		: this(SourceContext.None, defaultValue)
	{
	}

	public StateImpl(SourceContext context, Option<T> defaultValue)
		: this(context, new ValueFeed<T>(defaultValue), SubscriptionMode.Eager)
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

	/// <summary>
	/// Redirects future updates made through this state to the state that replaced it.
	/// Source replacement itself is owned by the shared <see cref="FeedSubscription{T}"/>.
	/// </summary>
	internal void HotSwap(StateImpl<T> replacement)
	{
		if (ReferenceEquals(this, replacement))
		{
			_updatesTarget = null;
			return;
		}

		_updatesTarget = replacement;

		// The replacement now owns the validation (if any, its results are forwarded to us through the source),
		// so we stop our own validation so the previous validator cannot overwrite results of the updated one.
		StopValidation();
	}

	/// <summary>
	/// Drops the updates made through this state, so it shows its source again (see <see cref="UpdateFeed{T}.Clear"/>
	/// for an update made after the source completed).
	/// </summary>
	internal void ClearUpdates()
		=> _inner.Clear();

	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct = default)
	{
		Enable();

		// Note: The subscription has been created using our own Context, and we forward the subscriber context only for requests propagation.
		return _subscription.GetMessages(context, ct);
	}

	/// <inheritdoc />
	public async ValueTask UpdateMessageAsync(Action<MessageBuilder<T>> updater, CancellationToken ct)
	{
		// First we make sure that the target UpdateFeed is active, so the update will be applied.
		var target = UpdatesTarget;
		target.Enable();
		target._subscription.Enable();

		var update = new Update(updater, _updatesKind);
		target._inner.Add(update);
		await update.HasBeenApplied.ConfigureAwait(false); // Makes sure to forward (the first) error to the caller if any.
	}

	/// <summary>
	/// Sets the validator which is used to produce the <see cref="MessageAxis.Validation"/> of this state each time its data changes.
	/// </summary>
	/// <remarks>
	/// This is idempotent: if a validator has already been set, it is replaced (it will be used starting at the next data change).
	/// </remarks>
	internal void SetValidator(Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> validator)
	{
		if (Interlocked.Exchange(ref _validator, validator) is not null)
		{
			return; // Validation loop already running, it will use the new validator on next data change.
		}

		var validation = CancellationTokenSource.CreateLinkedTokenSource(Context.Token);
		if (Interlocked.CompareExchange(ref _validation, validation, null) is not null)
		{
			validation.Dispose();
			return;
		}

		// Note: We use the AbortPrevious mode, so a pending validation is cancelled as soon as the data changes.
		_ = Context
			.GetOrCreateSource(this)
			.Where(msg => msg.Changes.Contains(MessageAxis.Data))
			.ForEachAwaitWithCancellationAsync(Validate, ConcurrencyMode.AbortPrevious, continueOnError: true, validation.Token);
	}

	private async ValueTask Validate(Message<T> msg, CancellationToken ct)
	{
		try
		{
			var data = msg.Current.Data;
			if (data.IsUndefined())
			{
				return; // Nothing to validate yet, keep the previous results (if any).
			}

			if (_validator is not { } validator)
			{
				return; // Validation has been stopped.
			}

			// Note: Some(null) is a value (unlike None): the validator is invoked with null.
			var hasValue = data.IsSome(out var value);
			var results = hasValue
				? await Task.Run(async () => await validator(value!, ct).ConfigureAwait(false), ct).ConfigureAwait(false)
				: null; // No value to validate (None), clear the results.

			if (ct.IsCancellationRequested)
			{
				return;
			}

			await UpdateMessageAsync(
					msg =>
					{
						// Makes sure to not publish results for a stale version of the data.
						// As this is invoked by the update pipeline of the state, this check is atomic with other updates of the state.
						var isSameData = msg.CurrentData.IsSome(out var currentValue)
							? hasValue && EqualityComparer<T>.Default.Equals(currentValue, value)
							: !hasValue;
						if (isSameData)
						{
							msg.Validation(results);
						}
					},
					ct)
				.ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			// Expected: the value changed (a new validation has been started) or the validation has been stopped.
		}
		catch (Exception error)
		{
			// Validation must never fault the state: we only log the error and keep the previous results.
			// Note: We do not log the message of the error as it might contain user input.
			var log = this.Log();
			if (log.IsEnabled(LogLevel.Error))
			{
				log.LogError("The validator of the state '{State}' failed with an '{ErrorType}'. Previous validation results are kept.", LogHelper.GetIdentifier(this), error.GetType().Name);
			}
			if (log.IsEnabled(LogLevel.Debug))
			{
				log.LogDebug(error, "Validator failure details for the state '{State}'.", LogHelper.GetIdentifier(this));
			}
		}
	}

	private void StopValidation()
	{
		if (Interlocked.Exchange(ref _validation, null) is { } validation)
		{
			validation.Cancel();
			validation.Dispose();
		}
		_validator = null;
	}

	[MemberNotNull(nameof(_subscription))]
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
		StopValidation();

		// Note: As the _innerFeed as been created by us, we dispose the _subscription (even it belongs to the StateStore)
		//		 in order to make sure to release the original underlying feed.
		//		 This is a temporary patch until we support dynamic updates of the subscription mode in FeedSubscription (i.e. the _subscriptionMode).
		if (_subscription is { } sub)
		{
			await sub.DisposeAsync().ConfigureAwait(false);
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
		public bool IsCompactable()
			=> _firstResult.Task.IsCompletedSuccessfully;

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
