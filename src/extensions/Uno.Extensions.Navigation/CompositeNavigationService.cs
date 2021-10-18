using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class CompositeNavigationService : NavigationService
{
    protected CompositeNavigationService(
        ILogger logger,
        IRegion region,
        IRegionNavigationServiceFactory serviceFactory)
        : base(logger, region, serviceFactory)
    {
    }

    public CompositeNavigationService(
    ILogger<CompositeNavigationService> logger,
    IRegion region,
    IRegionNavigationServiceFactory serviceFactory)
    : base(logger, region, serviceFactory)
    {
    }

    protected async override Task<NavigationResponse> CoreNavigateAsync(NavigationRequest request)
    {
        // At this point, any residual request needs to be handed
        // down to the appropriate nested service
        return await ChildrenNavigateAsync(request);
    }

    private async Task<NavigationResponse> ChildrenNavigateAsync(NavigationRequest request)
    {
        if (!(request?.Route?.IsNested ?? false))
        {
            return null;
        }

        var route = request.Route.Base;

        var children = await Region.GetChildren(route);
        if (!children.Any())
        {
            return null;
        }

        var childRouteName = children.First().Name;
        var childRequest = request with
        {
            Route = request.Route with
            {
                Scheme = request.Route.Scheme.TrimStartOnce(Schemes.Nested),
                Base = request.Route.NextBase(),
                Path = request.Route.NextPath()
            }
        };

        if (string.IsNullOrWhiteSpace(childRouteName))
        {
            childRequest = request with
            {
                Route = request.Route with
                {
                    Scheme = request.Route.Scheme.TrimStartOnce(Schemes.Nested)
                }
            };
        }
        var old = request.ToString();
        var newR = childRequest.ToString();
        return await ChildrenNavigateAsync(children.Select(r => r.Navigation()), childRequest);

    }

    private async Task<NavigationResponse> ChildrenNavigateAsync(IEnumerable<INavigationService> children, NavigationRequest request)
    {
        var tasks = new List<Task<NavigationResponse>>();
        foreach (var region in children)
        {
            tasks.Add(region.NavigateAsync(request));
        }

        await Task.WhenAll(tasks);
#pragma warning disable CA1849 // We've already waited all tasks at this point (see Task.WhenAll in line above)
        return tasks.First().Result;
#pragma warning restore CA1849
    }
}
