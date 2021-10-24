using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class Navigator : INavigator, IInstance<IServiceProvider>
{
    protected ILogger Logger { get; }

    private bool IsRoot => Region?.Parent is null;

    protected IRegion Region { get; }

    IServiceProvider IInstance<IServiceProvider>.Instance => Region?.Services;

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
                request = request with { Route = request.Route with { Scheme = Schemes.Current } };
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
            request = request with { Route = request.Route with { Scheme = Schemes.Current } };
            return DialogNavigateAsync(request);
        }

        return CoreNavigateAsync(request);
    }

    private async Task<NavigationResponse> DialogNavigateAsync(NavigationRequest request)
    {
        var dialogService = Region.NavigatorFactory().CreateService(Region, request);

        var dialogResponse = await dialogService.NavigateAsync(request);

        return dialogResponse;
    }

    protected virtual async Task<NavigationResponse> CoreNavigateAsync(NavigationRequest request)
    {
        if (request.Route.IsNested)
        {
            // At this point the request should be passed to nested, so remove
            // any nested scheme (ie ./ )
            request = request with { Route = request.Route with { Scheme = Schemes.Current } };
        }

        if (request.Route.IsEmpty)
        {
            return null;
        }

        var children = (from region in Region.Children
                        let childRoute = (region.Name == request.Route.Base) ?
                                            request with { Route = request.Route with { Base = request.Route.NextBase(), Path = request.Route.NextPath() } } :
                                            request
                        where
                            region.Name is not { Length: > 0 } ||
                            region.Name == request.Route.Base
                        select
                            new { Child = region, Route = childRoute }).ToArray();

        if (children.Length == 0)
        {
            return null;
        }

        var tasks = new List<Task<NavigationResponse>>();
        foreach (var region in children)
        {
            tasks.Add(region.Child.NavigateAsync(region.Route));
        }

        await Task.WhenAll(tasks);
#pragma warning disable CA1849 // We've already waited all tasks at this point (see Task.WhenAll in line above)
        return tasks.FirstOrDefault(r => r.Result is not null)?.Result;
#pragma warning restore CA1849
    }
}
