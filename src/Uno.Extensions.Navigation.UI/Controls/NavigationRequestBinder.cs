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

			var existingBinding = element.GetRequestBinding();
			if(existingBinding is not null)
			{
				existingBinding.Unbind();
			}

			var region = element.FindRegion();

			if (region is not null)
			{
				await region.EnsureLoaded();

				var handler = region.Services?.GetServices<IRequestHandler>().FirstOrDefault(x => x.CanBind(element));
				if (handler is not null)
				{
					var binding = handler.Bind(element);
					if (binding is not null)
					{
						element.SetRequestBinding(binding);
					}
				}
			}
		}
		catch
		{
			// Ensuring no bleeding of exceptions that could tear down app
		}
	}
}
