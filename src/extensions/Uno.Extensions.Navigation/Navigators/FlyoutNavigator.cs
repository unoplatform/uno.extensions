using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Regions;
using Windows.Foundation;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
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

    private Flyout Flyout { get; set; }

    public FlyoutNavigator(
        ILogger<ContentDialogNavigator> logger,
        IRouteMappings mappings,
        IRegion region)
        : base(logger, mappings, region)
    {
    }

    protected override bool CanNavigateToRoute(Route route) => base.CanNavigateToRoute(route) || route.IsBackOrCloseNavigation();

    protected override async Task<Route> ExecuteRequestAsync(NavigationRequest request)
    {
        var route = request.Route;
        // If this is back navigation, then make sure it's used to close
        // any of the open dialogs
        if (route.FrameIsBackNavigation() && Flyout is not null)
        {
            await CloseFlyout(route);
        }
        else
        {
            var mapping = Mappings.Find(route);
            var viewModel = CreateViewModel(Region.Services, this, route, mapping);
            Flyout = DisplayFlyout(request, mapping?.View, viewModel);
        }
        var responseRequest = route with { Path = null };
        return responseRequest;
    }

    private async Task CloseFlyout(Route route)
    {
        Flyout.Hide();
    }

    private Flyout? DisplayFlyout(NavigationRequest request, Type? viewType, object? viewModel)
    {
        var route = request.Route;
        var navigation = Region.Navigator();
        var services = Region.Services;
        var mapping = Mappings.Find(route);
        Flyout? flyout = null;
        if (mapping?.View is not null)
        {
            flyout = Activator.CreateInstance(mapping?.View) as Flyout;
        }
        else
        {
            var serviceLookupType = mapping?.View;
            if (serviceLookupType is null)
            {
                object? resource = request.RouteResourceView(Region);
                flyout = resource as Flyout;
            }
        }

        var flyoutElement = flyout?.Content as FrameworkElement;
        if (flyoutElement is not null)
        {
            flyoutElement.SetInstance(Region);
            flyoutElement.InjectServicesAndSetDataContext(services, navigation, viewModel);
        }

        var flyoutHost = request.Sender as FrameworkElement;
        if(flyoutHost is null)
        {
            flyoutHost = Region.View;
        }

        flyout?.ShowAt(flyoutHost);
        return flyout;
    }
}
