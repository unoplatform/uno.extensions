using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Utils;

internal sealed class SequentialAsyncOperationsManager : IAsyncOperationsManager
{
	private readonly Queue<AsyncAction> _queue = new();
	private readonly CancellationTokenSource _ct = new();
	private readonly TaskCompletionSource<object?> _task = new();
	private readonly bool _silentErrors;

	private bool _isRunning;
	private bool _isCompleted;

	public Task Task => _task.Task;

	public SequentialAsyncOperationsManager(bool silentErrors = false)
	{
		_silentErrors = silentErrors;
	}

	public void OnNext(AsyncAction operation)
	{
		lock (_queue)
		{
			if (_isCompleted)
			{
				throw new InvalidOperationException($"{nameof(SequentialAsyncOperationsManager)} has already been completed.");
			}

			if (_task.Task.IsCanceled)
			{
				throw new ObjectDisposedException(nameof(SequentialAsyncOperationsManager));
			}

			if (_task.Task.IsCompleted) // i.e. IsFaulted as we already checked Completed and Canceled, but lets be safer
			{
				return;
			}

			_queue.Enqueue(operation);
			if (!_isRunning)
			{
				_ = Dequeue();
			}
		}
	}

	/// <inheritdoc />
	public void OnError(Exception error)
	{
		if (_silentErrors)
		{
			this.Log().Warn(error, $"{nameof(FirstWinsAsyncOperationManager)} got a silent exception from parent.");
			OnCompleted();
		}
		else
		{
			_task.TrySetException(error);
			Dispose();
		}
	}

	/// <inheritdoc />
	public void OnCompleted()
	{
		// This might be invoked by caller on dispose, so do not throw if already disposed.
		bool completeNow;
		lock (_queue)
		{
			_isCompleted = true;
			completeNow = _queue is { Count: 0 };
		}

		if (completeNow)
		{
			_task.TrySetResult(default);
		}
	}

	private async Task Dequeue()
	{
		while (!_task.Task.IsCompleted)
		{
			AsyncAction? operation;
			bool isCompleted;
			lock (_queue)
			{
				isCompleted = _isCompleted;
				if (_queue.Count == 0)
				{
					_isRunning = false;
					operation = default;
				}
				else
				{
					_isRunning = true;
					operation = _queue.Dequeue();
				}
			}

			if (operation is null)
			{
				if (isCompleted)
				{
					_task.TrySetResult(default);
				}

				return;
			}

			try
			{
				await operation(_ct.Token).ConfigureAwait(false);
			}
			catch (OperationCanceledException) when (_ct.IsCancellationRequested)
			{
			}
			catch (Exception error)
			{
				if (_silentErrors)
				{
					this.Log().Warn(error, $"{nameof(SequentialAsyncOperationsManager)}  got a silent exception.");
				}
				else
				{
					_task.TrySetException(error);
					Dispose();
				}
			}
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_task.TrySetCanceled();
		_ct.Cancel();
	}
}
