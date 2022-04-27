namespace Uno.Extensions;

public class Dispatcher : IDispatcher
{
#if WINUI
	private DispatcherQueue _dispatcher;
#else
	private Windows.UI.Core.CoreDispatcher _dispatcher;
#endif

	public Dispatcher(Window window)
	{
#if WINUI
		// We can't grab the DispatcherQueue from the window because it's not supported in Uno yet
		_dispatcher =global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
#else
		_dispatcher = window.Dispatcher;
#endif
	}

	//public Task Run(Func<Task> action) => _window.DispatcherQueue.Run(action);
	public async ValueTask<TResult> ExecuteAsync<TResult>(AsyncFunc<TResult> actionWithResult, CancellationToken cancellation)
	{
		return await _dispatcher.ExecuteAsync(actionWithResult, cancellation);
	}
}
