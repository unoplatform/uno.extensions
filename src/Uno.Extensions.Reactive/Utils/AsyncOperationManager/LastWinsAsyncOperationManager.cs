using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Utils;

internal sealed class LastWinsAsyncOperationManager : IAsyncOperationsManager
{
	private readonly TaskCompletionSource<object?> _task = new();
	private readonly bool _silentErrors;

	private CancellationTokenSource? _current;
	private bool _isCompleted;

	public LastWinsAsyncOperationManager(bool silentErrors = false)
	{
		_silentErrors = silentErrors;
	}

	/// <inheritdoc />
	public Task Task => _task.Task;

	/// <inheritdoc />
	public void OnNext(AsyncAction operation)
	{
		// Note: To avoid concurrency issue with Dispose, we must set the new CT prior to check the _task.Task.IsCancel
		var ct = new CancellationTokenSource();
		Interlocked.Exchange(ref _current, ct)?.Cancel();

		if (_isCompleted)
		{
			throw new InvalidOperationException($"{nameof(LastWinsAsyncOperationManager)} has already been completed.");
		}

		if (_task.Task.IsCanceled)
		{
			throw new ObjectDisposedException(nameof(LastWinsAsyncOperationManager));
		}

		if (_task.Task.IsCompleted) // i.e. IsFaulted as we already checked Completed and Canceled, but lets be safer
		{
			return;
		}

		Run(operation, ct.Token);
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
		if (_current is null)
		{
			_task.TrySetResult(default);
		}
	}

	private async void Run(AsyncAction operation, CancellationToken ct)
	{
		try
		{
			await operation(ct).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			if (_silentErrors)
			{
				this.Log().Warn(error, $"{nameof(LastWinsAsyncOperationManager)} got a silent exception.");
			}
			else
			{
				_task.TrySetException(error);
				Dispose();
			}
		}
		finally
		{
			Interlocked.Exchange(ref _current, null);
			if (_isCompleted)
			{
				_task.TrySetResult(default);
			}
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_task.TrySetCanceled();
		_current?.Cancel();
	}
}
