using Uno.Extensions.Navigation.UI;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation.Navigators;

public class FlyoutNavigator : ControlNavigator
{
	public override bool CanGoBack => true;

	private Flyout? Flyout { get; set; }

	public FlyoutNavigator(
		ILogger<ContentDialogNavigator> logger,
		IResolver resolver,
		IRegion region)
		: base(logger, resolver, region)
	{
	}

	protected override bool CanNavigateToRoute(Route route) =>
		base.CanNavigateToRoute(route) &&
		(
			route.IsBackOrCloseNavigation() ||
			Resolver.Routes.Find(route)?.View?.RenderView is not null
		);

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
		if (route.FrameIsBackNavigation() && Flyout is not null)
		{
			CloseFlyout();
		}
		else
		{
			if(Flyout is not null)
			{
				return Route.Empty;
			}

			var mapping = Resolver.Routes.Find(route);
			injectedFlyout = !(mapping?.View?.RenderView?.IsSubclassOf(typeof(Flyout))??false);
			var viewModel = CreateViewModel(Region.Services, request, route, mapping);
			Flyout = await DisplayFlyout(request, mapping?.View?.RenderView, viewModel, injectedFlyout);
		}
		var responseRequest = injectedFlyout ? Route.Empty : route with { Path = null };
		return responseRequest;
	}

	private void CloseFlyout()
	{
		Flyout?.Hide();
	}

	private async Task<Flyout?> DisplayFlyout(NavigationRequest request, Type? viewType, object? viewModel, bool injectedFlyout)
	{
		var route = request.Route;
		var navigation = Region.Navigator();
		var services = Region.Services;

		if (navigation is null ||
			services is null)
		{
			return null;
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

		var flyoutElement = flyout?.Content as FrameworkElement;
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
		}

		flyout?.ShowAt(flyoutHost);


		await flyoutElement.EnsureLoaded();


		if (flyoutElement is not null && flyoutElement.Parent is not null)
		{
			flyoutElement.SetInstance(null); // Clear region off the flyout element
			flyoutElement.Parent.SetInstance(Region); // Set region on parent (now that it will be not null)
			flyoutElement.ReassignRegionParent(); // Update any sub-regions to correct their relationship with the parent (ie set the name)
		}

		return flyout;
	}
}
