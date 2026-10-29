using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
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

	private ValidationLoop? _validation;
	private readonly object _validationResultsGate = new();
	private ValidationUpdate? _validationResults;
	private static long _validationVersion; // Shared by the states (not per instance), so versions remain comparable when results are redirected to the state that replaced this one.

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
		// so we stop our own validation and drop our results, so the previous validator cannot overwrite results of the updated one.
		StopValidation();
		ClearValidationResults();
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
	/// <param name="validator">The validator.</param>
	/// <param name="source">The user's validator (delegate or service) from which the <paramref name="validator"/> has been created, used to identify it in logs.</param>
	/// <remarks>
	/// This is idempotent: if a validator has already been set, it is replaced (it will be used starting at the next data change).
	/// </remarks>
	internal void SetValidator(AsyncFunc<T, IEnumerable<ValidationResult>> validator, object? source = null)
	{
		source ??= validator;
		while (true)
		{
			if (Volatile.Read(ref _validation) is { } current)
			{
				// Validation loop already running, it will use the new validator on next data change.
				if (Interlocked.CompareExchange(ref _validation, current with { Validator = validator, Source = source }, current) == current)
				{
					return;
				}

				continue;
			}

			var loop = new ValidationLoop(validator, source, CancellationTokenSource.CreateLinkedTokenSource(Context.Token));
			if (Interlocked.CompareExchange(ref _validation, loop, null) is not null)
			{
				loop.Token.Dispose();
				continue;
			}

			// Note: We use the AbortPrevious mode, so a pending validation is cancelled as soon as the data changes.
			_ = Context
				.GetOrCreateSource(this)
				.Where(msg => msg.Changes.Contains(MessageAxis.Data))
				.ForEachAwaitWithCancellationAsync(Validate, ConcurrencyMode.AbortPrevious, continueOnError: true, loop.Token.Token);
			return;
		}
	}

	private async ValueTask Validate(Message<T> msg, CancellationToken ct)
	{
		var data = msg.Current.Data;
		if (data.IsUndefined())
		{
			return; // Nothing to validate yet, keep the previous results (if any).
		}

		if (Volatile.Read(ref _validation) is not { } loop)
		{
			return; // Validation has been stopped.
		}

		// The version is taken before running the validator: if results are published (e.g. using SetValidationAsync) while it runs, they win.
		var version = NextValidationVersion();

		// Note: Some(null) is a value (unlike None): the validator is invoked with null.
		var hasValue = data.IsSome(out var value);
		IEnumerable<ValidationResult>? results;
		try
		{
			results = hasValue
				? await Task.Run(() => loop.Validator(value!, ct).AsTask(), ct).ConfigureAwait(false)
				: null; // No value to validate (None), clear the results.
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			return; // Expected: the value changed (a new validation has been started) or the validation has been stopped.
		}
		catch (Exception error)
		{
			// Validation must never fault the state: we only log the error (results of a previous value are not reported for this one, cf. ValidationUpdate).
			LogFailure(error, loop, isPublishing: false);
			return;
		}

		if (ct.IsCancellationRequested)
		{
			return;
		}

		try
		{
			// Makes sure to not publish results for a stale version of the data.
			await PublishValidationAsync(
					results?.ToImmutableList(),
					currentData => currentData.IsSome(out var currentValue)
						? hasValue && EqualityComparer<T>.Default.Equals(currentValue, value)
						: !hasValue,
					version,
					ct)
				.ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			// Expected: the value changed (a new validation has been started) or the validation has been stopped.
		}
		catch (Exception error)
		{
			LogFailure(error, loop, isPublishing: true);
		}
	}

	/// <remarks>
	/// The message of the error is logged only at the debug level, as it might contain user input.
	/// </remarks>
	private void LogFailure(Exception error, ValidationLoop loop, bool isPublishing)
	{
		var log = this.Log();
		if (log.IsEnabled(LogLevel.Error))
		{
			if (isPublishing)
			{
				log.LogError("Failed to publish the results of the validator '{Validator}' on a state of '{ValueType}': its update pipeline failed with an '{ErrorType}'.", Describe(loop.Source), typeof(T).Name, error.GetType().Name);
			}
			else
			{
				log.LogError("The validator '{Validator}' of a state of '{ValueType}' failed with an '{ErrorType}'. The value has not been validated.", Describe(loop.Source), typeof(T).Name, error.GetType().Name);
			}
		}
		if (log.IsEnabled(LogLevel.Debug))
		{
			log.LogDebug(error, "Validation failure details for the validator '{Validator}' of a state of '{ValueType}'.", Describe(loop.Source), typeof(T).Name);
		}

		static string Describe(object source)
		{
			if (source is not Delegate { Method: var method })
			{
				return source.GetType().Name;
			}

			// For a lambda, the method is declared by a compiler generated type: we use the type of the user's code which declares it.
			var type = method.DeclaringType;
			while (type?.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) is true && type.DeclaringType is not null)
			{
				type = type.DeclaringType;
			}

			return $"{type?.Name}.{method.Name}";
		}
	}

	/// <summary>
	/// Publishes validation results on the <see cref="MessageAxis.Validation"/> of this state, replacing the results previously published.
	/// </summary>
	/// <param name="results">The validation results, null or empty to clear them.</param>
	/// <param name="isCurrent">
	/// An optional predicate to determine if the results are relevant for the current data of the state.
	/// It is invoked by the update pipeline of the state each time the message is rebuilt (e.g. when the source of the state produces a new message),
	/// so this check is atomic with other updates of the state and results are not shown for another value.
	/// </param>
	/// <param name="version">
	/// The version of the results (cf. <see cref="NextValidationVersion"/>), taken when the validation started:
	/// results are not published if results of a later version have already been published, so the last validation started wins (not the last one to complete).
	/// </param>
	/// <param name="ct">A cancellation token: the results are not published if already cancelled, and the wait for the publication is aborted when cancelled.</param>
	/// <remarks>
	/// Results are kept in a single update of the state, which is replaced on each publication (results of a state are not merged and are not stacked),
	/// and which is not dropped when the source of the state produces a new message (unlike updates of the data of a <see cref="StateUpdateKind.Volatile"/> state).
	/// </remarks>
	internal async ValueTask PublishValidationAsync(IImmutableList<ValidationResult>? results, Func<Option<T>, bool>? isCurrent, long version, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		// Like UpdateMessageAsync, the results go to the state that replaced this one (if any), and we make sure its UpdateFeed is active.
		var target = UpdatesTarget;
		target.Enable();
		target._subscription.Enable();

		var update = new ValidationUpdate(target, results, isCurrent, version);
		lock (target._validationResultsGate)
		{
			if (target._validationResults is { } current && current.Version > version)
			{
				return; // Results of a validation started later have already been published.
			}

			// Note: The previous results are not removed from the UpdateFeed (which would rebuild the message), they are superseded (cf. ValidationUpdate.IsLatest).
			target._validationResults = update;
			target._inner.Add(update);
		}

		var applied = update.HasBeenApplied;
		if (applied.IsCompleted)
		{
			await applied.ConfigureAwait(false); // Propagates the error (if any).
		}
		else
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, target.Context.Token);
			await applied.WaitAsync(cts.Token).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Gets the version to use to publish validation results (cf. <see cref="PublishValidationAsync"/>).
	/// </summary>
	internal static long NextValidationVersion()
		=> Interlocked.Increment(ref _validationVersion);

	private void StopValidation()
	{
		if (Interlocked.Exchange(ref _validation, null) is { } loop)
		{
			loop.Token.Cancel();
			loop.Token.Dispose();
		}
	}

	private void ClearValidationResults()
	{
		lock (_validationResultsGate)
		{
			if (_validationResults is { } results)
			{
				_inner.Remove(results);
				_validationResults = null;
			}
		}
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

	/// <summary>
	/// The validator of a state, with the token of its validation loop.
	/// </summary>
	/// <remarks>Immutable, so the validator can be replaced and the loop stopped atomically.</remarks>
	private sealed record ValidationLoop(AsyncFunc<T, IEnumerable<ValidationResult>> Validator, object Source, CancellationTokenSource Token);

	/// <summary>
	/// The validation results of a state (cf. <see cref="PublishValidationAsync"/>).
	/// </summary>
	/// <remarks>
	/// Unlike an <see cref="Update"/>, this remains active when the source of the state produces a new message, until it is superseded by the next results:
	/// results are re-applied on each new message and after each update of the data, as long as they are relevant for its data.
	/// </remarks>
	private sealed class ValidationUpdate(StateImpl<T> owner, IImmutableList<ValidationResult>? results, Func<Option<T>, bool>? isCurrent, long version) : IFeedUpdate<T>
	{
		private readonly TaskCompletionSource<object?> _firstResult = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public long Version => version;

		public Task HasBeenApplied => _firstResult.Task;

		/// <summary>
		/// Gets a boolean indicating if these are the last results published on the state.
		/// </summary>
		/// <remarks>
		/// Results are published by adding a new update (removing the previous one would rebuild the message, which drops the updates of a state whose source completed).
		/// So previous results remain in the active updates of the state, but are no longer applied, until they are removed.
		/// </remarks>
		private bool IsLatest => ReferenceEquals(Volatile.Read(ref owner._validationResults), this);

		/// <inheritdoc />
		/// <remarks>Results are relevant only for a given data, so they are checked after the updates of the data.</remarks>
		public bool IsAppliedLast => true;

		/// <inheritdoc />
		public bool IsActive(Message<T>? parent, bool parentChanged, IMessageEntry<T> entry)
			=> !_firstResult.Task.IsFaulted && IsLatest;

		/// <inheritdoc />
		/// <remarks>
		/// Even once the source of the state has completed, the last results have to be re-applied on each update of the data, to remove them if no longer relevant.
		/// </remarks>
		public bool IsCompactable()
			=> _firstResult.Task.IsCompletedSuccessfully && !IsLatest;

		/// <inheritdoc />
		public void Apply(bool _, MessageBuilder<T, T> message)
		{
			try
			{
				if (IsLatest)
				{
					// Results which are not relevant for the current data are removed (they would remain when an update of the data is applied incrementally).
					message.Validation(isCurrent?.Invoke(message.CurrentData) ?? true ? results : null);
				}
				_firstResult.TrySetResult(null);
			}
			catch (Exception error) when (!_firstResult.Task.IsCompleted) // Otherwise let the exception bubble up.
			{
				_firstResult.TrySetException(error);
			}
		}
	}
}
