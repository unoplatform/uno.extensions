namespace Uno.Extensions.Navigation.UI;

public abstract class ControlRequestHandlerBase<TControl> : IRequestHandler
{
	public abstract IRequestBinding? Bind(FrameworkElement view);

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
}

public record RequestBinding (FrameworkElement View, RoutedEventHandler LoadedHandler, RoutedEventHandler UnloadedHandler) : IRequestBinding
{
	public void Unbind()
	{
		if (LoadedHandler is not null)
		{
			if (View is not null)
			{
				View.Loaded -= LoadedHandler;
			}
		}
		if (UnloadedHandler is not null)
		{
			UnloadedHandler(View, null);
			if (View is not null)
			{
				View.Unloaded -= UnloadedHandler;
			}
		}
	}
}
