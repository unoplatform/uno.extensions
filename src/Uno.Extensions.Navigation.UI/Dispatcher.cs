namespace Uno.Extensions;

public class Dispatcher : IDispatcher
{
	private readonly Window _window;
	public Dispatcher(Window window)
	{
		_window = window;
	}

	//public Task Run(Func<Task> action) => _window.DispatcherQueue.Run(action);
	public Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> actionWithResult) {
#if WINUI
		return _window.DispatcherQueue.ExecuteAsync(actionWithResult);
#else
		return _window.Dispatcher.ExecuteAsync(actionWithResult);
#endif
	}
}
