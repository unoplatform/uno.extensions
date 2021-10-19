using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class Navigator : INavigator
{
    protected ILogger Logger { get; }

    private bool IsRoot => Region?.Parent is null;

    protected IRegion Region { get; }

    public Navigator(
        ILogger<Navigator> logger,
        IRegion region)
    {
        Region = region;
        Logger = logger;
    }

    protected Navigator(
    ILogger logger,
    IRegion region)
    {
        Region = region;
        Logger = logger;
    }

    public Task<NavigationResponse> NavigateAsync(NavigationRequest request)
    {
        // Handle root navigations
        if (request?.Route?.IsRoot ?? false)
        {
            if (!IsRoot)
            {
                return Region.Parent?.NavigateAsync(request);
            }
            else
            {
                // This is the root nav service - need to pass the
                // request down to children by making the request nested
                request = request with { Route = request.Route.Root };
            }
        }

        if (request?.Route?.IsParent ?? false)
        {
            request = request with { Route = request.Route.TrimScheme(Schemes.Parent) };

            // Handle parent navigations
            if (request?.Route?.IsParent ?? false)
            {
                return Region.Parent?.NavigateAsync(request);
            }
        }

        // Run dialog requests
        if (request.Route.IsDialog)
        {
            request = request with { Route = request.Route.TrimScheme(Schemes.Dialog) };
            return DialogNavigateAsync(request);
        }

        return CoreNavigateAsync(request);
    }

    private async Task<NavigationResponse> DialogNavigateAsync(NavigationRequest request)
    {
        var dialogService = Region.NavigationFactory().CreateService(Region, request);

        var dialogResponse = await dialogService.NavigateAsync(request);

        return dialogResponse;
    }

    protected virtual async Task<NavigationResponse> CoreNavigateAsync(NavigationRequest request)
    {
        if (request.Route.IsEmpty)
        {
            return null;
        }

        var route = request.Route.Base;

        // Scenarios:
        // Region.Name == "" : in this case just forward the request unchanged
        // Region.Name == request.Route.Base and scheme is  "./" : in this case trim both the scheme and base (ie Route.Next.Next)
        // Region.Name == request.Route.Base : trim the base (ie Route.Next)
        // Scheme is "./" : trim the scheme (ie Route.Next)
        var children = (from child in Region.Children
                       let childRoute =
                                    (child.Name == request.Route.Base && request.Route.IsNested) ?
                                       request with { Route = request.Route.Next.Next } :
                                       ((child.Name == request.Route.Base || request.Route.IsNested) ?
                                           request with { Route = request.Route.Next } :
                                           request)
                       where
                           child.Name is not { Length: > 0 } ||
                           child.Name == request.Route.Base
                       select
                           new { Child = child, Route = childRoute }).ToArray();

        var tasks = new List<Task<NavigationResponse>>();
        foreach (var region in children)
        {
            tasks.Add(region.Child.NavigateAsync(region.Route));
        }

        await Task.WhenAll(tasks);
#pragma warning disable CA1849 // We've already waited all tasks at this point (see Task.WhenAll in line above)
        return tasks.First().Result;
#pragma warning restore CA1849
    }
}
