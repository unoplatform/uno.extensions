namespace Uno.Extensions.Navigation.UI;

public abstract class ControlRequestHandlerBase<TControl> : IRequestHandler
{
	protected RoutedEventHandler? _loadedHandler;
	protected RoutedEventHandler? _unloadedHandler;
	protected FrameworkElement? _view;

	public abstract void Bind(FrameworkElement view);

	public bool CanBind(FrameworkElement view)
	{
		var viewType = view.GetType();
		if (viewType == typeof(TControl))
		{
			return true;
		}

		var baseTypes = viewType.GetBaseTypes();
		return baseTypes.Any(baseType => baseType == typeof(TControl));
	}

	public void Unbind()
	{
		if (_loadedHandler is not null)
		{
			_loadedHandler = null;
			if (_view is not null)
			{
				_view.Loaded -= _loadedHandler;
			}
		}
		if (_unloadedHandler is not null)
		{
			_unloadedHandler(_view, null);
			_unloadedHandler = null;
			if (_view is not null)
			{
				_view.Unloaded -= _unloadedHandler;
			}
		}
		_view = null;
	}
}
