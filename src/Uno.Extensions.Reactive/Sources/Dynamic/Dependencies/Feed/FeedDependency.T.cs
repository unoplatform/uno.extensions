using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Utils;
using Uno.Extensions.Threading;

namespace Uno.Extensions.Reactive.Sources;

internal sealed class FeedDependency<T> : FeedDependency, IDependency
{
	private readonly FeedSession _session; // The session fo the dependent feed
	private readonly ISignal<Message<T>> _feed; // The dependency feed on which the session depends
	private readonly FastAsyncLock _loadingGate = new();
	private readonly List<MessageAxis> _touchedAxes = new();
	private readonly TaskCompletionSource<Unit> _hasLast = new();

	private Message<T> _last = Message<T>.Initial; // The most recent message we received from the dependency feed
	private (FeedExecution execution, Message<T> message, IDisposable updateLock)? _current; // Temporary cached value while the dependent feed is loading (i.e. between IDependency.OnLoading and IDependency.OnLoaded)

	public FeedDependency(FeedExecution execution, ISignal<Message<T>> feed)
		: base(feed)
	{
		_session = execution.Session;
		_feed = feed;

		_current = (execution, Message<T>.Initial, Disposable.Empty); // Dummy current that will be completed in the Subscribe with the first message.
		_ = Subscribe(execution);
	}

	/// <summary>
	/// Gets the current message for the given <paramref name="execution"/>.
	/// </summary>
	/// <param name="execution"></param>
	/// <returns></returns>
	public async ValueTask<Message<T>> GetCurrentMessage(FeedExecution execution)
	{
		// Make sure to wait for at least one message (and the _current and been fully initialized).
		await _hasLast.Task.ConfigureAwait(false);

		// When we have an active execution, we prefer to return the cached value in order to improve data coherence.
		return IsActive(execution)
			? _current!.Value.message
			: _last; // If the dependency feed has completed, this will still contain the last/final message.
	}

	/// <inheritdoc />
	private protected override void NotifyTouched(FeedExecution execution, MessageAxis axis)
	{
		if (IsActive(execution))
		{
			// We track the touched axes only while we have an active execution, i.e. between IDependency.OnLoading and IDependency.OnLoaded.
			_touchedAxes.Add(axis);
		}
	}

	/// <inheritdoc />
	async ValueTask IDependency.OnExecuting(FeedExecution execution, CancellationToken ct)
	{
		Debug.Assert(_current is null);
		_current?.updateLock.Dispose();

		// If we received a 'OnLoading' it's because this FeedDependency has been registered in the '_session' and we probably already have loaded a value.
		// BUT if the previous execution has been aborted, the _last might not have been set yet.
		await _hasLast.Task.ConfigureAwait(false);

		Init(execution, await _loadingGate.LockAsync(_session.Token).ConfigureAwait(false));
	}

	/// <inheritdoc />
	async ValueTask IDependency.OnExecuted(FeedExecution execution, FeedExecutionResult result, CancellationToken ct)
	{
		if (_current is { } current) // If this dependency has been added while loading, we will not have set the _current value yet.
		{
			Debug.Assert(_current.Value.execution == execution);

			_session.Feeds.CleanupCache(this, current.message); // Avoids leak and implicitly disables the touched axis tracking
			_current = null; // Disable the touched axis tracking

			current.updateLock.Dispose();
		}
	}

	private bool IsActive(FeedExecution execution)
	{
		if (_current is { } current)
		{
			Debug.Assert(execution == current.execution);

			if (current.execution == execution)
			{
				return true;
			}
		}

		return false;
	}

	private void Init(FeedExecution execution, IDisposable @lock)
	{
		// We capture the current value on loading, so we make sure to have the same value as long as the execution is active.
		_current = (execution, _last, @lock);
		_session.Feeds.Update(this, _last, updateEntryCache: true);
		_touchedAxes.Clear(); // Once locked, we reset the touched axis for this new execution.
	}

	private async Task Subscribe(FeedExecution execution)
	{
		try
		{
			var isFirst = true;
			var messages = _session.Context.States.GetOrCreateSubscription<ISignal<Message<T>>, T>(_feed).GetMessages(_session.Context, _session.Token);
			await foreach (var message in messages.WithCancellation(_session.Token).ConfigureAwait(false))
			{
				// We wait for the loading to complete before updating the value (and continue the enumeration of the dependency feed).
				// This is to ensure that the current does not change while loading, and we properly invalidate the dependent feed depending of the touched axes.
				var gate = await _loadingGate.LockAsync(_session.Token).ConfigureAwait(false);
				try
				{
					_last = message;
					if (isFirst && IsActive(execution)) // IsActive is false it means that the execution has been aborted before we get the first message.
					{
						Init(execution, gate);
						gate = null; // We transfer the ownership of the lock to the _current.
					}
					isFirst = false;
					execution = null!; // Prevent leak
					_hasLast.TrySetResult(default);

					if (message.Changes.FirstOrDefault(axis => _touchedAxes.Contains(axis)) is { } axis)
					{
						// An axis used by the dependent feed has changed, we need to reload the dependent feed.
						_session.Execute(new Request(_feed, axis));
					}
					else
					{
						// The change from the parent concerns only axis that are not used by the dependent feed, so we can just forward the parent without re-loading the dependent feed.
						_session.Feeds.Update(this, message);
					}
				}
				finally
				{
					gate?.Dispose();
				}
			}
		}
		catch (OperationCanceledException) when (_session.Token.IsCancellationRequested)
		{
			// The session has been cancelled, we don't need to do anything.
		}
		catch (Exception error)
		{
			this.Log().Error(error, $"Subscription to the dependency feed '{_feed}' has failed, the value will no longer be updated.");
		}
		finally
		{
			_hasLast.TrySetResult(default);

			// Note: Even if unregistered, this FeedDependency might still be used to replay the last/final value which will remain as current!
			_session.Feeds.Cleanup(this);
		}
	}

	private record Request(ISignal<Message<T>> feed, MessageAxis axis)
		: ExecuteRequest(feed, $"the axis '{axis.Identifier}' used from dependency '{feed}' has changed (some other used axes might also have changed)");
}
