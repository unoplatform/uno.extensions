using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.UI;
using Uno.Extensions.Navigation.Regions;
using Windows.Foundation;
#if !WINUI
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Windows.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
#endif

namespace Uno.Extensions.Navigation.Navigators;

public class FlyoutNavigator : ControlNavigator
{
    public override bool CanGoBack => true;

    private Flyout? Flyout { get; set; }

    public FlyoutNavigator(
        ILogger<ContentDialogNavigator> logger,
        IRouteResolver routeResolver, //IViewResolver viewResolver,
        IRegion region)
        : base(logger, routeResolver, region)
    {
    }

    protected override bool CanNavigateToRoute(Route route) => base.CanNavigateToRoute(route) || route.IsBackOrCloseNavigation();

    protected override async Task<Route?> ExecuteRequestAsync(NavigationRequest request)
    {
        if (Region.Services is null)
        {
            return default;
        }

        var route = request.Route;
        // If this is back navigation, then make sure it's used to close
        // any of the open dialogs
        if (route.FrameIsBackNavigation() && Flyout is not null)
        {
            CloseFlyout();
        }
        else
        {
            var mapping = RouteResolver.Find(route);
            var viewModel = CreateViewModel(Region.Services, this, route, mapping);
            Flyout = await DisplayFlyout(request, mapping?.View, viewModel);
        }
        var responseRequest = route with { Path = null };
        return responseRequest;
    }

    private void CloseFlyout()
    {
        Flyout?.Hide();
    }

    private async Task<Flyout?> DisplayFlyout(NavigationRequest request, Type? viewType, object? viewModel)
    {
        var route = request.Route;
        var navigation = Region.Navigator();
        var services = Region.Services;
        var mapping = RouteResolver.Find(route);

        if (navigation is null ||
            services is null)
        {
            return null;
        }

        Flyout? flyout = null;
        if (mapping?.View is not null)
        {
            flyout = Activator.CreateInstance(mapping.View) as Flyout;
        }
        else
        {
            object? resource = request.RouteResourceView(Region);
            flyout = resource as Flyout;
        }

        var flyoutElement = flyout?.Content as FrameworkElement;
        if (flyoutElement is not null)
        {
            flyoutElement.InjectServicesAndSetDataContext(services, navigation, viewModel);
            flyoutElement.SetInstance(Region);
            flyoutElement.SetName(route.Base); // Set the name on the flyout element
        }

        var flyoutHost = request.Sender as FrameworkElement;
        if (flyoutHost is null)
        {
            flyoutHost = Region.View;
        }

        flyout?.ShowAt(flyoutHost);


        await flyoutElement.EnsureLoaded();


		if (flyoutElement is not null)
		{
			flyoutElement.SetInstance(null); // Clear region off the flyout element
			flyoutElement.Parent.SetInstance(Region); // Set region on parent (now that it will be not null)
			flyoutElement.ReassignRegionParent(); // Update any sub-regions to correct their relationship with the parent (ie set the name)
		}

        return flyout;
    }
}
