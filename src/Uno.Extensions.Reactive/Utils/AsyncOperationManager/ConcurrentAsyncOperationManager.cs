using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Utils;

internal sealed class ConcurrentAsyncOperationManager : IAsyncOperationsManager
{
	private readonly TaskCompletionSource<object?> _task = new();
	private readonly CancellationTokenSource _ct = new();
	private readonly bool _silentErrors;

	private int _pending = 1;
	private bool _isCompleted;

	public ConcurrentAsyncOperationManager(bool silentErrors = false)
	{
		_silentErrors = silentErrors;
	}

	/// <inheritdoc />
	public Task Task => _task.Task;

	/// <inheritdoc />
	public void OnNext(AsyncAction operation)
	{
		if (_isCompleted)
		{
			throw new InvalidOperationException($"{nameof(ConcurrentAsyncOperationManager)} has already been completed.");
		}

		if (_task.Task.IsCanceled)
		{
			throw new ObjectDisposedException(nameof(ConcurrentAsyncOperationManager));
		}

		if (_task.Task.IsCompleted) // i.e. IsFaulted as we already checked Completed and Canceled, but lets be safer
		{
			return;
		}

		_ = operation(_ct.Token).AsTask().ContinueWith(
			t =>
			{
				if (t.IsFaulted)
				{
					if (_silentErrors)
					{
						this.Log().Warn(t.Exception!, $"{nameof(ConcurrentAsyncOperationManager)} got a silent exception.");
					}
					else
					{
						_task.TrySetException(t.Exception!);
						Dispose();
					}
				}

				Decrease();
			},
			TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);
	}

	/// <inheritdoc />
	public void OnError(Exception error)
	{
		if (_silentErrors)
		{
			this.Log().Warn(error, $"{nameof(ConcurrentAsyncOperationManager)} got a silent exception from parent.");
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
		_isCompleted = true;
		Decrease();
	}

	private void Decrease()
	{
		if (Interlocked.Decrement(ref _pending) <= 0)
		{
			_task.TrySetResult(default);
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_task.TrySetCanceled();
		_ct.Cancel();
	}
}
