namespace Uno.Extensions.Navigation.UI;

internal class NavigationRequestBinder
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
		// A failed bind means taps never navigate and nothing else logs it,
		// so every bail-out below warns (studio.live#2716).
		try
		{
			if (element is null)
			{
				return;
			}

			var existingBinding = element.GetRequestBinding();

			if (existingBinding is not null)
			{
				// just exit, since we're already bound?
				return;
			}

			var region = element.FindRegion();

			if (region is null)
			{
				if (Region.Logger.IsEnabled(LogLevel.Warning))
				{
					Region.Logger.LogWarningMessage($"Navigation.Request '{element.GetRequest()}' on {element.GetType().Name} not bound: no region found from the element. The request will not trigger until the element reloads inside a region.");
				}
				return;
			}

			await region.View.EnsureLoaded();

			// This picks the last handler so that handlers can be overridden for
			// specific controls by registering another handler
			var handler = region.Services?.GetServices<IRequestHandler>().LastOrDefault(x => x.CanBind(element));
			if (handler is null)
			{
				if (Region.Logger.IsEnabled(LogLevel.Warning))
				{
					var reason = region.Services is null
						? "the region has no service provider (detached or not yet attached to a parent region)"
						: "no IRequestHandler can bind this element type";
					Region.Logger.LogWarningMessage($"Navigation.Request '{element.GetRequest()}' on {element.GetType().Name} not bound: {reason}.");
				}
				return;
			}

			var binding = handler.Bind(element);

			// Unbind existing binding if it doesn't match this binding
			if (element.GetRequestBinding() is { } existing)
			{
				existing.Unbind();
			}

			if (binding is not null)
			{
				element.SetRequestBinding(binding);
			}
		}
		catch (OperationCanceledException)
		{
			// Element/region torn down while awaiting EnsureLoaded; the replacement element gets its own binder.
		}
		catch (Exception ex)
		{
			// Ensuring no bleeding of exceptions that could tear down app
			if (Region.Logger.IsEnabled(LogLevel.Warning))
			{
				Region.Logger.LogWarningMessage($"Navigation.Request binding failed for {element?.GetType().Name}: {ex.GetType().Name}: {ex.Message}. Taps on this element will not navigate.");
			}
		}
	}
}
