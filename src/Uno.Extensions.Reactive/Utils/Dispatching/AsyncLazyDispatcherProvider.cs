using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Dispatching;

/// <summary>
/// An helper class to create a <see cref="Dispatching.FindDispatcher"/> with the ability
/// to asynchronously get notified when the first dispatcher is being resolved.
/// </summary>
internal sealed class AsyncLazyDispatcherProvider : IDisposable
{
	private readonly FindDispatcher _dispatcherProvider;
	private readonly TaskCompletionSource<IDispatcher> _first = new();

	public AsyncLazyDispatcherProvider(FindDispatcher? dispatcherProvider = null)
	{
		_dispatcherProvider = dispatcherProvider ?? DispatcherHelper.GetForCurrentThread;
	}

	public bool TryResolve()
		=> FindDispatcher() is not null;

	public Task<IDispatcher> GetFirstResolved(CancellationToken ct)
		=> _first.Task;

	public IDispatcher? FindDispatcher()
	{
		if (_dispatcherProvider() is { } dispatcher)
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
