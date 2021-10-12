using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Navigation;

public class CompositeNavigationService : NavigationService, IRegionNavigationService
{
    protected IList<(string, IRegionNavigationService)> NestedServices { get; } = new List<(string, IRegionNavigationService)>();

    private AsyncAutoResetEvent NestedServiceWaiter { get; } = new AsyncAutoResetEvent(false);

    private IRegionNavigationServiceFactory ServiceFactory { get; }

    public CompositeNavigationService(
        ILogger logger,
        IRegionNavigationService parent,
        IRegionNavigationServiceFactory serviceFactory) : base(logger, parent)
    {
        ServiceFactory = serviceFactory;
    }

    public void Attach(string regionName, IRegionNavigationService childRegion)
    {
        var childService = childRegion;
        NestedServices.Add((regionName + string.Empty, childService));
        NestedServiceWaiter.Set();
    }

    public void Detach(IRegionNavigationService childRegion)
    {
        NestedServices.Remove(kvp => kvp.Item2 == childRegion);
    }

    protected async override Task<NavigationResponse> CoreNavigateAsync(NavigationRequest request)
    {
        var coreResponse = await base.CoreNavigateAsync(request);

        if (coreResponse is not null)
        {
            return coreResponse;
        }

        var dialogResponse = await DialogNavigateAsync(request);
        if (dialogResponse is not null)
        {
            return dialogResponse;
        }

        // At this point, any residual request needs to be handed
        // down to the appropriate nested service
        return await NestedNavigateAsync(request);
    }

    private IRegionNavigationService[] Nested(string regionName = null)
    {
        return NestedServices.Where(kvp => kvp.Item1 == regionName + string.Empty).Select(x => x.Item2).ToArray();
    }

    private async Task<NavigationResponse> DialogNavigateAsync(NavigationRequest request)
    {
        if (request.Route.IsDialog)
        {

            var dialogService = ServiceFactory.CreateService(this, request);
            Attach(RouteConstants.DialogPrefix, dialogService);

            request = request with { Route = request.Route.TrimScheme(Schemes.Dialog) };
            var dialogResponse = await dialogService.NavigateAsync(request);

            if (dialogResponse is null || dialogResponse.Result is null)
            {
                Detach(dialogService);
            }
            else
            {
                _ = dialogResponse.Result.ContinueWith(t => Detach(dialogService));
            }
            return dialogResponse;
        }

        return null;
    }

    protected virtual async Task<NavigationResponse> NestedNavigateAsync(NavigationRequest request)
    {
        if (!(request?.Route?.IsNested ?? false))
        {
            return null;
        }

        var nestedRoute = request.Route.Base;

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
                    var nextRoute = request.Route.Uri.OriginalString.TrimStart($"{Schemes.Nested}");
                    request = request.WithPath(nextRoute);
                }
            }
            else
            {
                var nextRoute = request.Route.Uri.OriginalString.TrimStart($"{Schemes.Nested}{nestedRoute}/");
                request = request.WithPath(nextRoute);
            }

            if (nested is null || !nested.Any())
            {
                await NestedServiceWaiter.Wait();
            }
        }

        var tasks = new List<Task<NavigationResponse>>();
        foreach (var region in nested)
        {
            tasks.Add(region.NavigateAsync(request));
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
