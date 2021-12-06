using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
using Windows.Foundation;

namespace Uno.Extensions.Navigation.Navigators;

public abstract class DialogNavigator : ControlNavigator
{
    public override bool CanGoBack => true;

    private IAsyncInfo? ShowTask { get; set; }

    protected DialogNavigator(
        ILogger<DialogNavigator> logger,
        IRouteResolver routeResolver, IViewResolver viewResolver,
        IRegion region)
        : base(logger, routeResolver, viewResolver, region)
    {
    }

    protected override bool CanNavigateToRoute(Route route) => base.CanNavigateToRoute(route) || route.IsBackOrCloseNavigation();

    protected override async Task<Route?> ExecuteRequestAsync(NavigationRequest request)
    {
        var route = request.Route;
        // If this is back navigation, then make sure it's used to close
        // any of the open dialogs
        if (route.FrameIsBackNavigation() && ShowTask is not null)
        {
            await CloseDialog();
        }
        else
        {
            var mapping = ViewResolver.FindView(route);
            var viewModel = (Region.Services is not null && mapping?.ViewModel is not null) ? CreateViewModel(Region.Services, this, route, mapping) : default(object);
            ShowTask = DisplayDialog(request, mapping?.View, viewModel);
        }
        var responseRequest = route with { Path = null };
        return responseRequest;
    }

    protected async Task CloseDialog()
    {
        var dialog = ShowTask;
        ShowTask = null;

        dialog?.Cancel();
    }

    protected abstract IAsyncInfo? DisplayDialog(NavigationRequest request, Type? viewType, object? viewModel);
}
