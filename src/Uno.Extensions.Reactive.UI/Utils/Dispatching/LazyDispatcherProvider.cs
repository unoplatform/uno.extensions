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

internal class AsyncLazyDispatcherProvider
{
	private readonly TaskCompletionSource<DispatcherQueue> _first = new();

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
}
