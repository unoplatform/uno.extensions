using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
using Windows.Foundation;

namespace Uno.Extensions.Navigation.Navigators;

public abstract class DialogNavigator : ControlNavigator
{
    public override bool CanGoBack => true;

    private IAsyncInfo ShowTask { get; set; }

    protected DialogNavigator(
        ILogger<DialogNavigator> logger,
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
        if (route.FrameIsBackNavigation() && ShowTask is not null)
        {
            await CloseDialog(route);
        }
        else
        {
            var mapping = Mappings.Find(route);
            var viewModel = CreateViewModel(Region.Services, this, route, mapping);
            ShowTask = DisplayDialog(request, mapping?.View, viewModel);
        }
        var responseRequest = route with { Path = null };
        return responseRequest;
    }

    protected async Task CloseDialog(Route route)
    {
        var dialog = ShowTask;
        ShowTask = null;

        var responseData = route.Data.TryGetValue(string.Empty, out var response) ? response : default;

        dialog.Cancel();
    }

    protected abstract IAsyncInfo DisplayDialog(NavigationRequest request, Type? viewType, object? viewModel);
}
