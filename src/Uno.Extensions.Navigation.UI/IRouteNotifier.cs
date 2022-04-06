namespace Uno.Extensions.Navigation;

public interface IRouteNotifier
{
	event EventHandler<RouteChangedEventArgs> RouteChanged;
}

#if WINUI
public interface IWindowProvider
{
	Window Current { get; }
}

public class WindowProvider : IWindowProvider
{
	private readonly Func<Window> _windowCallback;
	public WindowProvider(Func<Window> windowCallback)
	{
		_windowCallback = windowCallback;
	}

	public Window Current => _windowCallback.Invoke();
}
#endif
