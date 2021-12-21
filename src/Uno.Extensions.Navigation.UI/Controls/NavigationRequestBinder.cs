namespace Uno.Extensions.Navigation.UI;

public class NavigationRequestBinder
{
	public NavigationRequestBinder(FrameworkElement view)
	{
		if (view.IsLoaded)
		{
			BindRequestHandler(view);
		}
		else
		{
			view.Loaded += (s, e) => BindRequestHandler(s as FrameworkElement);
		}
	}

	private async void BindRequestHandler(FrameworkElement? element)
	{

		try
		{
			if (element is null)
			{
				return;
			}

			var existingHandler = element.GetHandler();
			if(existingHandler is not null)
			{
				existingHandler.Unbind();
			}

			var region = element.FindRegion();

			if (region is not null)
			{
				await region.EnsureLoaded();

				var binder = region.Services?.GetServices<IRequestHandler>().FirstOrDefault(x => x.CanBind(element));
				if (binder is not null)
				{
					binder.Bind(element);
					element.SetHandler(binder);
				}
			}
		}
		catch
		{
			// Ensuring no bleeding of exceptions that could tear down app
		}
	}
}
