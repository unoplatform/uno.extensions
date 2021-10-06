using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;

namespace Uno.Extensions.Navigation;

public class CompositeNavigationService : NavigationService, IRegionNavigationService
{
    protected IList<(string, IRegionNavigationService)> NestedServices { get; } = new List<(string, IRegionNavigationService)>();

    private AsyncAutoResetEvent NestedServiceWaiter { get; } = new AsyncAutoResetEvent(false);

    public CompositeNavigationService(ILogger logger, IDialogFactory dialogFactory) : base(logger, dialogFactory)
    {
    }

    public void Attach(string regionName, IRegionNavigationService childRegion)
    {
        var childService = childRegion;
        NestedServices.Add((regionName + string.Empty, childService));
        NestedServiceWaiter.Set();
        childRegion.Parent = this;
    }

    public void Detach(IRegionNavigationService childRegion)
    {
        NestedServices.Remove(kvp => kvp.Item2 == childRegion);
        childRegion.Parent = null;
    }

    public async override Task<NavigationResponse> NavigateAsync(NavigationRequest request)
    {

        // At this point, any residual request needs to be handed
        // down to the appropriate nested service
        var nestedNavigationResponse = await RunNestedNavigation(request);

        var baseNavigationResponse = await base.NavigateAsync(request);

        return baseNavigationResponse ?? nestedNavigationResponse;
    }

    private IRegionNavigationService[] Nested(string regionName = null)
    {
        return NestedServices.Where(kvp => kvp.Item1 == regionName + string.Empty).Select(x => x.Item2).ToArray();
    }

    protected async Task<NavigationResponse> RunNestedNavigation(NavigationRequest request)
    {
        var nestedRequest = request;
        if (nestedRequest is null || nestedRequest.Segments.IsParent)
        {
            await Task.CompletedTask;
            return null;
        }

        var nestedRoute = nestedRequest.Segments.Base;

        IRegionNavigationService[] nested = null;
        while (nested is null || !nested.Any())
        {
            // Try to retrieve nested service based on route name
            nested = Nested(nestedRoute);
            if (nested is null || !nested.Any())
            {
                // No match for named route, so grab any unnamed nested
                nested = Nested();
                if (nested is not null && nested.Any())
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

            if (nested is null || !nested.Any())
            {
                await NestedServiceWaiter.Wait();
            }
        }

        var tasks = new List<Task<NavigationResponse>>();
        foreach (var region in nested)
        {
            tasks.Add(region.NavigateAsync(nestedRequest));
        }
        await Task.WhenAll(tasks);
        //var response = await nested.NavigateAsync(nestedRequest);
        return tasks.First().Result;
    }

    protected virtual void PrintAllRegions(StringBuilder builder, IRegionNavigationService nav, int indent = 0, string regionName = null)
    {
        if (nav is CompositeNavigationService comp)
        {
            foreach (var nested in comp.NestedServices)
            {
                PrintAllRegions(builder, nested.Item2 as IRegionNavigationService, indent + 1, nested.Item1);
            }
        }
    }
}
