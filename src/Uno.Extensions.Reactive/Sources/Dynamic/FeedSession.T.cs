using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Sources;

internal sealed partial class FeedSession<TResult> : FeedSession, IAsyncEnumerable<Message<TResult>>
{
	private readonly AsyncFunc<Option<TResult>> _mainAsyncAction;
	private readonly AsyncEnumerableSubject<Message<TResult>> _messages = new(ReplayMode.EnabledForFirstEnumeratorOnly);
	private readonly MessageManager<Unit, TResult> _message;

	public FeedSession(DynamicFeed<TResult> feed, SourceContext context, AsyncFunc<Option<TResult>> mainAsyncAction, CancellationToken ct)
		: base(feed, context, ct)
	{
		_mainAsyncAction = mainAsyncAction;

		_message = new MessageManager<Unit, TResult>(_messages.TrySetNext);

		Execute(new ExecuteRequest(this, "Initial load"));
	}

	#region Core executions chain managment (including Request support)
	private readonly object _requestsGate = new();
	private List<ExecuteRequest>? _pendingRequests;
	private Execution? _currentExecution; // This the execution currently loading the data, 

	/// <inheritdoc />
	public override void Execute(ExecuteRequest request)
	{
		lock (_requestsGate)
		{
			if (_isDisposed)
			{
				return;
			}

			(_pendingRequests ??= new()).Add(request);

			if (_currentExecution is { } current)
			{
				// Here we only request to the current to abort, we don't wait for it to complete.
				// It's its responsibility to call TryStartNext() when it's done.
				_ = current.DisposeAsync(); 
			}
			else
			{
				TryStartNext();
			}
		}
	}

	private bool TryStartNext()
	{
		lock (_requestsGate)
		{
			// When the current execution is about to complete, if we have pending request we start immediately the next execution.
			// This will prevent the current one to commit its result (as we already know that it's out-dated, we try to reduce the number of messages in feeds)
			// but it will also allow the new execution to preserve the transient axes (set using the updateTransaction.SetTransient, e.g. the IsTransient axis).
			if (_pendingRequests is { Count: > 0 } requests)
			{
				_pendingRequests = null; // Clear requests first to avoid cycling on same request if teh execution completes sync!
				_ = new Execution(this, requests); // Note: we do NOT set this as _currentExecution. This is done by the new execution itself.

				return true;
			}
			else
			{
				TryComplete();
				return false;
			}
		}
	}
	#endregion

	#region Parent message support (i.e. FeedDependency) (Should this be merged in the FeedDependenciesStore?)
	private readonly Dictionary<ISignal<IMessage>, IMessage> _parentMessages = new();
	private DynamicParentMessage _aggregatedParentMessage = DynamicParentMessage.Initial;
	private bool _isAggregatedParentMessageValid = true;

	internal override void UpdateParent(ISignal<IMessage> feed, IMessage message)
	{
		if (_isDisposed)
		{
			return;
		}

		if (_parentMessages.TryGetValue(feed, out var previous) && previous == message)
		{
			return;
		}

		lock (_parentMessages)
		{
			_isAggregatedParentMessageValid = false;
			_parentMessages[feed] = message;
		}

		// Note: No needs to lock here to protect against _currentExecution being replaced,
		//		 the found 'current' will forward the update to the '_message' if Completed or disposed.
		//		 The only effect will be to produce more messages than we would like to.
		if (_currentExecution is { } current)
		{
			lock (current.StateGate)
			{
				if (current.State == Execution.States.Loading)
				{
					return; // Nothing to do, the update will be processed when the main load action pushes a messages.
				}
			}
		}

		_message.Update(static (m, p) => m.With(p), GetParent(), default);
	}

	private IMessage GetParent()
	{
		lock (_parentMessages)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(FeedSession), $"Cannot get parent message on a completed session.");
			}

			if (!_isAggregatedParentMessageValid)
			{
				_aggregatedParentMessage = _aggregatedParentMessage.With(_parentMessages.Values);
				_isAggregatedParentMessageValid = true;
			}

			return _aggregatedParentMessage;
		}
	}
	#endregion

	/// <inheritdoc />
	public IAsyncEnumerator<Message<TResult>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		=> _messages.GetAsyncEnumerator(cancellationToken);

	/// <inheritdoc />
	protected override void TryComplete()
	{
		lock (_requestsGate)
		{
			if (_currentExecution is null && _pendingRequests is null or { Count: 0 } && Dependencies is null or { Count: 0 })
			{
				_ = DisposeAsync();
			}
		}
	}

	/// <inheritdoc />
	public override async ValueTask DisposeAsync()
	{
		if (_currentExecution is { } current)
		{
			await current.DisposeAsync().ConfigureAwait(false);
		}

		_messages.Complete(); // Prevent any new message to be published

		await base.DisposeAsync().ConfigureAwait(false);

		lock (_parentMessages)
		{
			_parentMessages.Clear();
		}
	}
}
