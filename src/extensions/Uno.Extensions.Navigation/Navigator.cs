using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
using static Uno.Extensions.GenericExtensions;

namespace Uno.Extensions.Navigation;

public class Navigator : INavigator, IInstance<IServiceProvider>
{
    protected ILogger Logger { get; }

    protected IRegion Region { get; }

    private INavigationNotifier? Notifier => Region.Services?.GetRequiredService<INavigationNotifier>();

    IServiceProvider? IInstance<IServiceProvider>.Instance => Region.Services;

    public Route? Route { get; protected set; }

    public Navigator(ILogger<Navigator> logger, IRegion region) : this((ILogger)logger, region)
    {
    }

    protected Navigator(ILogger logger, IRegion region)
    {
        Region = region;
        Logger = logger;
    }

    public async Task<NavigationResponse?> NavigateAsync(NavigationRequest request)
    {
        Logger.LogInformation($"Pre-navigation: - {Region.ToString()}");
        try
        {
            // Handle root navigations
            if (request.Route.IsRoot())
            {
                // Either
                // - forward to parent (if parent is not null)
                // - trim the Root scheme ready for handling
                if (Region.Parent is not null)
                {
                    return await (Region.Parent?.NavigateAsync(request) ?? Task.FromResult<NavigationResponse?>(default));
                }
                else
                {
                    // This is the root nav service - need to pass the
                    // request down to children by making the request nested
                    request = request with { Route = request.Route.TrimScheme(Schemes.Root) };
                }
            }

            // Request for parent (ignore the first layer of parent scheme)
            if (request.Route.IsParent())
            {
                request = request with { Route = request.Route.TrimScheme(Schemes.Parent) };

                // Handle parent navigations
                if (request.Route.IsParent())
                {
                    return await (Region.Parent?.NavigateAsync(request) ?? Task.FromResult<NavigationResponse?>(default));
                }
            }

            // Is this region is an unnamed child of a composite,
            // send request to parent if the route has no scheme
            if (request.Route.IsCurrent() &&
                !Region.IsNamed() &&
                Region.Parent is not null)
            {
                return await Region.Parent.NavigateAsync(request);
            }


            // Run dialog requests
            if (request.Route.IsDialog())
            {
                request = request with { Route = request.Route with { Scheme = Schemes.Current } };
                return await DialogNavigateAsync(request);
            }

            // If the base matches the region name, than need to strip the base
            if (!string.IsNullOrWhiteSpace(request.Route.Base) &&
                request.Route.Base == Region.Name)
            {
                request = request with { Route = request.Route.Next() };
            }

            // Initialise the region
            var requestMap = Region.Services?.GetRequiredService<IRouteMappings>().FindByPath(request.Route.Base);
            if (requestMap?.RegionInitialization is not null)
            {
                var newRequest = requestMap.RegionInitialization(Region, request);
                request = newRequest;
            }

            return await CoreNavigateAsync(request);
        }
        finally
        {
            Logger.LogInformation($"Post-navigation: {Region.ToString()}");
            Logger.LogInformation($"Post-navigation (route): {Region.Root().GetRoute()}");
            Notifier?.Update(Region);
        }
    }

    private async Task<NavigationResponse?> DialogNavigateAsync(NavigationRequest request)
    {
        var dialogService = Region.Services?.GetService<INavigatorFactory>()?.CreateService(Region, request);

        var dialogResponse = await (dialogService?.NavigateAsync(request)??Task.FromResult<NavigationResponse?>(default));

        return dialogResponse;
    }

    protected virtual async Task<NavigationResponse?> CoreNavigateAsync(NavigationRequest request)
    {
        //if (request.Route.IsNested())
        //{
        //    // At this point the request should be passed to nested, so remove
        //    // any nested scheme (ie ./ )
        //    request = request with { Route = request.Route.TrimScheme(Schemes.Nested) };// with { Scheme = Schemes.Current } };
        //}

        if (request.Route.IsCurrent())
        {
            request = request with { Route = request.Route.AppendScheme(Schemes.Nested) };
        }

        if (request.Route.IsEmpty())
        {
            return null;
        }

        var children = Region.Children.Where(region =>
                                        // Unnamed child regions
                                        string.IsNullOrWhiteSpace(region.Name) ||
                                        // Regions whose name matches the next route segment
                                        region.Name == request.Route.Base ||
                                        // Regions whose name matches the current route
                                        // eg currently selected tab
                                        region.Name == Route?.Base
                                    ).ToArray();

        var tasks = new List<Task<NavigationResponse?>>();
        foreach (var region in children)
        {
            tasks.Add(region.NavigateAsync(request));
        }

        await Task.WhenAll(tasks);
#pragma warning disable CA1849 // We've already waited all tasks at this point (see Task.WhenAll in line above)
        return tasks.FirstOrDefault(r => r.Result is not null)?.Result;
#pragma warning restore CA1849
    }

    public override string ToString()
    {
        var current = NavigatorToString;
        if (!string.IsNullOrWhiteSpace(current))
        {
            current = $"({current})";
        }
        return $"{this.GetType().Name}{current}";
    }

    protected virtual string NavigatorToString { get; } = string.Empty;
}
