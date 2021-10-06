using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;

namespace Uno.Extensions.Navigation;

public class CompositeNavigationService : NavigationService, IRegionNavigationService
{
    protected IDictionary<string, IRegionNavigationService> NestedServices { get; } = new Dictionary<string, IRegionNavigationService>();

    private AsyncAutoResetEvent NestedServiceWaiter { get; } = new AsyncAutoResetEvent(false);

    public CompositeNavigationService(ILogger logger, IRegionNavigationService parent, IDialogFactory dialogFactory) : base(logger, parent, dialogFactory)
    {
    }

    public void Attach(string regionName, IRegionNavigationService childRegion)
    {
        var childService = childRegion as RegionNavigationService;
        NestedServices[regionName + string.Empty] = childService;
        NestedServiceWaiter.Set();
    }

    public void Detach(IRegionNavigationService childRegion)
    {
        NestedServices.Remove(kvp => kvp.Value == childRegion);
    }

    public async override Task<NavigationResponse> NavigateAsync(NavigationRequest request)
    {

        // At this point, any residual request needs to be handed
        // down to the appropriate nested service
        var nestedNavigationResponse = await RunNestedNavigation(request);

        var baseNavigationResponse = await base.NavigateAsync(request);

        return baseNavigationResponse ?? nestedNavigationResponse;
    }

    private IRegionNavigationService Nested(string regionName = null)
    {
        return NestedServices.TryGetValue(regionName + string.Empty, out var service) ? service : null;
    }

    protected async Task<NavigationResponse> RunNestedNavigation(NavigationRequest request)
    {
        var nestedRequest = request;
        if (nestedRequest is null || !nestedRequest.Segments.IsNested)
        {
            await Task.CompletedTask;
            return null;
        }

        var nestedRoute = nestedRequest.Segments.Base;

        RegionNavigationService nested = null;
        while (nested is null)
        {
            // Try to retrieve nested service based on route name
            nested = Nested(nestedRoute) as RegionNavigationService;
            if (nested is null)
            {
                // No match for named route, so grab any unnamed nested
                nested = Nested() as RegionNavigationService;
                if (nested is not null)
                {
                    var nextRoute = nestedRequest.Route.Uri.OriginalString.TrimStart($"{RouteConstants.Schemes.Nested}/");
                    nestedRequest = nestedRequest.WithPath(nextRoute);
                }
            }
            else
            {
                var nextRoute = nestedRequest.Route.Uri.OriginalString.TrimStart($"{RouteConstants.Schemes.Nested}/{nestedRoute}/");
                nestedRequest = nestedRequest.WithPath(nextRoute);
            }

            if (nested is null)
            {
                await NestedServiceWaiter.Wait();
            }
        }

        var response = await nested.NavigateAsync(nestedRequest);
        return response;
    }
}
