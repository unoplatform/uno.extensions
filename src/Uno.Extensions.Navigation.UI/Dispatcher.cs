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

	/// <inheritdoc />
	public async ValueTask<TResult> ExecuteAsync<TResult>(AsyncFunc<TResult> func, CancellationToken cancellation)
		=> await _dispatcher.ExecuteAsync(func, cancellation);
}
