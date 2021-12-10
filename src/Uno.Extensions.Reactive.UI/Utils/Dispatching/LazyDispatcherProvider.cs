using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Dispatching;

internal class LazyDispatcherProvider
{
	private Action? _onFirstResolved;

	public LazyDispatcherProvider(Action onFirstResolved)
	{
		_onFirstResolved = onFirstResolved;
	}

	/// <summary>
	/// Try to run the callback if not yet ran and **if current thread is a UI thread**.
	/// </summary>
	public void TryRunCallback()
	{
		FindDispatcher();
	}

	/// <summary>
	/// Force to run the callback if not ran yet, **no matter if the current thread is a UI thread or not**.
	/// </summary>
	public void RunCallback()
	{
		if (Interlocked.Exchange(ref _onFirstResolved, null) is { } onFirstResolved)
		{
			onFirstResolved();
		}
	}

	public DispatcherQueue? FindDispatcher()
	{
		if (DispatcherHelper.GetForCurrentThread() is { } dispatcher)
		{
			RunCallback();

			return dispatcher;
		}
		else
		{
			return null;
		}
	}
}

internal class AsyncLazyDispatcherProvider : IDisposable
{
	private readonly TaskCompletionSource<DispatcherQueue> _first = new();

	public void TryResolve()
		=> FindDispatcher();

	public Task<DispatcherQueue> GetFirstResolved(CancellationToken ct)
		=> _first.Task;

	public DispatcherQueue? FindDispatcher()
	{
		if (DispatcherHelper.GetForCurrentThread() is { } dispatcher)
		{
			_first.TrySetResult(dispatcher);

			return dispatcher;
		}
		else
		{
			return null;
		}
	}

	/// <inheritdoc />
	public void Dispose()
		=> _first.TrySetCanceled();
}
