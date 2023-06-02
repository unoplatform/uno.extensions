namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Record that references callbacks to be invoked when a view is loaded and unloaded
/// </summary>
/// <param name="View">The view to attach/detach event handlers</param>
/// <param name="LoadedHandler">The loaded callback</param>
/// <param name="UnloadedHandler">The unloaded callback</param>
public record RequestBinding (
	FrameworkElement View,
	RoutedEventHandler LoadedHandler,
	RoutedEventHandler UnloadedHandler) : IRequestBinding
{
	void IRequestBinding.Unbind()
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
