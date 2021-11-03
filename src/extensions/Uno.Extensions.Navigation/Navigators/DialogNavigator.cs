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

    protected override async Task<Route> RouteNavigateAsync(Route route)
    {
        // If this is back navigation, then make sure it's used to close
        // any of the open dialogs
        if (route.FrameIsBackNavigation() && ShowTask is not null)
        {
            await CloseDialog(route);
        }
        else
        {
            var viewModel = CreateViewModel(Region.Services, this, route, Mappings.Find(route));
            ShowTask = DisplayDialog(route, viewModel);
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

    protected abstract IAsyncInfo DisplayDialog(Route route, object vm);
}
