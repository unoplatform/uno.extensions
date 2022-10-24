using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Core;

internal sealed class StateImpl<T> : IState<T>, IFeed<T>, IAsyncDisposable, IStateImpl
{
	private readonly object _updateGate = new();
	private readonly UpdateFeed<T>? _updates;
	private readonly IForEachRunner? _innerEnumeration;
	private readonly CompositeRequestSource _requests = new();

	private bool _hasCurrent;
	private Message<T> _current = Message<T>.Initial;
	private TaskCompletionSource<Node>? _next = new(TaskCreationOptions.AttachedToParent);

	/// <summary>
	/// Gets the context to which this state belongs.
	/// </summary>
	internal SourceContext Context { get; }
	SourceContext IStateImpl.Context => Context;

	internal Message<T> Current => _current;

	private readonly struct Node
	{
		private readonly Message<T> _value;
		private readonly TaskCompletionSource<Node> _next;

		public Node(Message<T> value, TaskCompletionSource<Node> next)
		{
			_value = value;
			_next = next;
		}

		public void Deconstruct(out Message<T> current, out TaskCompletionSource<Node> next)
		{
			current = _value;
			next = _next;
		}
	}

	public StateImpl(
		SourceContext context,
		IFeed<T> feed,
		StateSubscriptionMode mode = StateSubscriptionMode.Default,
		StateUpdateKind updatesKind = StateUpdateKind.Volatile)
	{
		Context = context.CreateChild(_requests);

		if (updatesKind is StateUpdateKind.Persistent)
		{
			// Note: We expect to use the UpdateFeed for all kind of updates, but to avoid silent breaking changes,
			//		 for now we use it only when using the new Persistent mode.
			feed = _updates = new UpdateFeed<T>(feed);

			// If updates has to be persistent, the subscription to the UpdateFeed must remain active.
			mode &= ~StateSubscriptionMode.RefCounted;
		}

		_innerEnumeration = mode.HasFlag(StateSubscriptionMode.RefCounted)
			? new RefCountedForEachRunner<Message<T>>(GetSource, UpdateState)
			: new ForEachRunner<Message<T>>(GetSource, UpdateState);
		if (mode.HasFlag(StateSubscriptionMode.Eager))
		{
			_innerEnumeration.Prefetch();
		}

		IAsyncEnumerable<Message<T>> GetSource()
			=> feed.GetSource(Context);

		ValueTask UpdateState(Message<T> newSrcMsg, CancellationToken ct)
			// Note: When using the StateUpdateKind.Persistent, the newScrMsg is known to be the same as the currentStateMsg
			//		 (as updates are made on the source 'feed' instead of locally), so this is equivalent to:
			//=> UpdateCore(_ => newSrcMsg, ct); // Note2 : OverrideBy is optimized to handle that case!
			=> UpdateCore(currentStateMsg => currentStateMsg.OverrideBy(newSrcMsg), ct);
	}

	public StateImpl(SourceContext context, Option<T> defaultValue)
	{
		Context = context?.CreateChild(_requests)!; // Null check override only for legacy IInput support

		_hasCurrent = true; // Even if undefined, we consider that we do have a value in order to produce an initial state
		if (!defaultValue.IsUndefined())
		{
			_current = _current.With().Data(defaultValue);
		}
	}

	/// <summary>
	/// Legacy - Used only be legacy IInput syntax
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public StateImpl(Option<T> defaultValue)
		: this(null!, defaultValue)
	{
	}

	internal IAsyncEnumerable<Message<T>> GetSource(CancellationToken ct)
		=> GetSource(Context, ct);

	public async IAsyncEnumerable<Message<T>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	{
		using var _ = _innerEnumeration?.Enable();

		if (Context is not null /*Legacy IInput support*/ && context != Context)
		{
			_requests.Add(context.RequestSource, ct);
		}

		var isFirstMessage = true;
		TaskCompletionSource<Node>? next;
		lock(_updateGate)
		{
			// We access to the _current only in the _updateGate, so we make sure that we would never miss or replay a value
			// by listening to the _next too late/early
			if (_hasCurrent)
			{
				yield return Message<T>.Initial.OverrideBy(_current);
				isFirstMessage = false;
			}

			next = _next;
		}

		while (!ct.IsCancellationRequested && next is not null)
		{
			Message<T> current;
			try
			{
				(current, next) = await next.Task.ConfigureAwait(false);
			}
			catch (TaskCanceledException)
			{
				yield break;
			}

			if (isFirstMessage)
			{
				current = Message<T>.Initial.OverrideBy(current);
				isFirstMessage = false;
			}

			yield return current;
		}

		if (isFirstMessage)
		{
			yield return Message<T>.Initial;
		}
	}

	/// <inheritdoc />
	public ValueTask UpdateMessage(Action<MessageBuilder<T>> updater, CancellationToken ct)
	{
		if (_updates is not null)
		{
			// WARNING: There is a MAJOR issue here: if updater fails, the error will be propagated into the output feed instead of the caller!
			// This is acceptable for now as so far there is no way to reach this code from public API

			// First we make sure that the UpdateFeed is active, so the update will be applied ^^
			_innerEnumeration?.Enable();
			return _updates.Update((_, _) => true, (_, msg) => updater(new(msg.Get, ((IMessageBuilder)msg).Set)), ct);
		}
		else
		{
			// Compatibility mode
			return UpdateCore(msg => msg.With().Apply(updater).Build(), ct);
		}
	}

	private async ValueTask UpdateCore(Func<Message<T>, Message<T>> updater, CancellationToken ct)
	{
		if (_next is null)
		{
			return;
		}

		Message<T> updated;
		TaskCompletionSource<Node>? current, next;
		lock (_updateGate)
		{
			updated = updater(_current);

			if (_current.Current != updated.Previous)
			{
				throw new InvalidOperationException(
					"The updated message is not based on the current message. "
					+ "You must use the Message.With() or Message.OverrideBy() to create a new version.");
			}

			if (_hasCurrent && updated is { Changes.Count: 0 })
			{
				return;
			}

			if (!MoveNext(out current, out next, ct))
			{
				return;
			}

			_current = updated;
			_hasCurrent = true;
		}

		current.TrySetResult(new Node(updated, next));
	}

	private bool MoveNext(
		[NotNullWhen(true)] out TaskCompletionSource<Node>? current, 
		[NotNullWhen(true)] out TaskCompletionSource<Node>? next, 
		CancellationToken ct)
	{
		next = new TaskCompletionSource<Node>(TaskCreationOptions.AttachedToParent);
		while (true)
		{
			current = _next;
			if (ct.IsCancellationRequested || current is null)
			{
				// Update has been aborted or State has been disposed
				return false;
			}

			if (Interlocked.CompareExchange(ref _next, next, current) == current)
			{
				return true;
			}
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (Context is not null)
		{
			await Context.DisposeAsync();
		}
		_requests.Dispose(); // Safety only, should have already been disposed by the Context
		_innerEnumeration?.Dispose();
		Interlocked.Exchange(ref _next, null)?.TrySetCanceled();
	}
}
