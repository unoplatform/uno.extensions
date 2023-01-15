namespace Uno.Extensions;

public class Dispatcher : IDispatcher
{
#if WINUI
	private readonly DispatcherQueue _dispatcher;
#else
	private readonly Windows.UI.Core.CoreDispatcher _dispatcher;
#endif

	public Dispatcher(Window window)
	{
#if WINUI
		// We can't grab the DispatcherQueue from the window because it's not supported in Uno yet
		_dispatcher = global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
#else
		_dispatcher = window.Dispatcher;
#endif
	}

	public Dispatcher(FrameworkElement element)
	{
#if WINUI
		// We can't grab the DispatcherQueue from the window because it's not supported in Uno yet
		_dispatcher = global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
#else
		_dispatcher = element.Dispatcher;
#endif

	}

	/// <inheritdoc />
	public bool TryEnqueue(Action action)
#if WINUI
		=> _dispatcher.TryEnqueue(() => action());
#else
	{
		_ = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
		return true;
	}
#endif

	/// <inheritdoc />
	public async ValueTask<TResult> ExecuteAsync<TResult>(AsyncFunc<TResult> func, CancellationToken cancellation)
	{
		if (PlatformHelper.IsThreadingEnabled && 
			HasThreadAccess)
		{
			return await func(cancellation);
		}
		return await _dispatcher.ExecuteAsync(func, cancellation);
	}

	/// <inheritdoc />
	public bool HasThreadAccess => _dispatcher.HasThreadAccess;
}
