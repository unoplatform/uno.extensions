namespace Uno.Extensions.Navigation.Navigators;

public class FlyoutNavigator : ControlNavigator
{
	public override bool CanGoBack => true;

	private Flyout? Flyout { get; set; }

	private Window _window;

	public FlyoutNavigator(
		ILogger<ContentDialogNavigator> logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver,
		Window window)
		: base(logger, dispatcher, region, resolver)
	{
		_window = window;
	}

	protected override Task<bool> RegionCanNavigate(Route route, RouteInfo? routeMap)
	{
		if (route.IsBackOrCloseNavigation())
		{
			return Task.FromResult(true);
		}
		if (routeMap?.RenderView is null)
		{
			return Task.FromResult(false);
		}
		return base.RegionCanNavigate(route, routeMap);
	}

	protected override async Task<Route?> ExecuteRequestAsync(NavigationRequest request)
	{
		if (Region.Services is null)
		{
			return default;
		}

		var route = request.Route;
		// If this is back navigation, then make sure it's used to close
		// any of the open dialogs
		var injectedFlyout = false;
		if (route.FrameIsBackNavigation())
		{
			CloseFlyout();
		}
		else
		{
			if (Flyout is not null)
			{
				return Route.Empty;
			}

			var mapping = Resolver.FindByPath(route.Base);
			injectedFlyout = !(mapping?.RenderView?.IsSubclassOf(typeof(Flyout)) ?? false);
			var viewModel = await CreateViewModel(Region.Services, request, route, mapping);
			Flyout = await DisplayFlyout(request, mapping?.RenderView, viewModel, injectedFlyout);
		}
		var responseRequest = injectedFlyout ? Route.Empty : route with { Path = null };
		return responseRequest;
	}

	private void CloseFlyout()
	{
		if (Flyout is not null)
		{
			Flyout.Closed -= Flyout_Closed;
			Flyout.Hide();
		}
	}

	private async Task<Flyout?> DisplayFlyout(NavigationRequest request, Type? viewType, object? viewModel, bool injectedFlyout)
	{
		var route = request.Route;
		var navigation = Region.Navigator();
		var services = Region.Services;

		if (navigation is null ||
			services is null)
		{
			return default;
		}

		Flyout? flyout = null;
		if (viewType is not null)
		{
			if (!injectedFlyout)
			{
				flyout = Activator.CreateInstance(viewType) as Flyout;
			}
			else
			{
				flyout = services.GetService<Flyout>();
			}
		}
		else
		{
			object? resource = request.RouteResourceView(Region);
			flyout = resource as Flyout;
		}

		if (flyout is null)
		{
			return default;
		}

		var flyoutElement = flyout.Content as FrameworkElement;
		if (flyoutElement is not null)
		{
			if (!injectedFlyout)
			{
				flyoutElement.InjectServicesAndSetDataContext(services, navigation, viewModel);
			}

			flyoutElement.SetInstance(Region);
			if (!injectedFlyout)
			{
				flyoutElement.SetName(route.Base); // Set the name on the flyout element
			}
		}

		var flyoutHost = request.Sender as FrameworkElement;
		if (flyoutHost is null)
		{
			flyoutHost = Region.View;

			if (flyoutHost is null)
			{
				flyoutHost = _window.Content as FrameworkElement;
			}
		}

		flyout.ShowAt(flyoutHost);

		flyout.Closed += Flyout_Closed;

		await flyoutElement.EnsureLoaded();


		if (flyoutElement is not null && flyoutElement.Parent is not null)
		{
			flyoutElement.SetInstance(null); // Clear region off the flyout element
			flyoutElement.Parent.SetInstance(Region); // Set region on parent (now that it will be not null)
			flyoutElement.ReassignRegionParent(); // Update any sub-regions to correct their relationship with the parent (ie set the name)
		}

		return flyout;
	}

	private async void Flyout_Closed(object? sender, object e)
	{
		if (Flyout is null)
		{
			return;
		}

		Flyout.Closed -= Flyout_Closed;

		var navigation = Region.Navigator();
		if (navigation is null)
		{
			return;
		}

		await navigation.NavigateBackAsync(this);
	}
}
