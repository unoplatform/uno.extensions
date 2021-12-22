namespace Uno.Extensions.Navigation.UI;

public class NavigationRequestBinder
{
	private FrameworkElement View { get; }

	public NavigationRequestBinder(FrameworkElement view)
	{
		View = view;
		View.Loaded += LoadedHandler;
	}

	private async void LoadedHandler(object sender, RoutedEventArgs args)
	{
		View.Loaded -= LoadedHandler;

		var region = View.FindRegion();

		if (region is not null)
		{
			await region.EnsureLoaded();

			var binder = region.Services?.GetServices<IRequestHandler>().FirstOrDefault(x => x.CanBind(View));
			if (binder is not null)
			{
				binder.Bind(View);
			}
		}
	}
}
