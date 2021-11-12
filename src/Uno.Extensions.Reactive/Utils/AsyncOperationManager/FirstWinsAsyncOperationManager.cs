using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Utils.Logging;

namespace Uno.Extensions.Reactive.Utils;

internal sealed class FirstWinsAsyncOperationManager : IAsyncOperationsManager
{
	private readonly TaskCompletionSource<object?> _task = new();
	private readonly CancellationTokenSource _ct = new();
	private readonly bool _silentErrors;

	private int _isRunning;
	private bool _isCompleted;

	public FirstWinsAsyncOperationManager(bool silentErrors = false)
	{
		_silentErrors = silentErrors;
	}

	/// <inheritdoc />
	public Task Task => _task.Task;

	/// <inheritdoc />
	public void OnNext(ActionAsync asyncOperation)
	{
		if (_isCompleted)
		{
			throw new InvalidOperationException($"{nameof(FirstWinsAsyncOperationManager)} has already been completed.");
		}

		if (_task.Task.IsCanceled)
		{
			throw new ObjectDisposedException(nameof(FirstWinsAsyncOperationManager));
		}

		if (_task.Task.IsCompleted) // i.e. IsFaulted as we already checked Completed and Canceled, but lets be safer
		{
			return;
		}

		if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0)
		{
			_ = asyncOperation(_ct.Token).AsTask().ContinueWith(
				t =>
				{
					if (t.IsFaulted)
					{
						if (_silentErrors)
						{
							this.Log().Warn(t.Exception!, $"{nameof(FirstWinsAsyncOperationManager)} got a silent exception.");
						}
						else
						{
							_task.TrySetException(t.Exception!);
							Dispose();
						}
					}

					// Note: To avoid concurrency issue with the COmplete(). we must set the _isRunning false prior to check the _isCompleted
					_isRunning = 0;

					if (_isCompleted)
					{
						_task.TrySetResult(default);
					}
				},
				TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);
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
		_isCompleted = true;
		if (_isRunning == 0)
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
