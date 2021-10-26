using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class Navigator : INavigator, IInstance<IServiceProvider>
{
    private Route _currentRoute;

    protected ILogger Logger { get; }

    private bool IsRoot => Region?.Parent is null;

    protected IRegion Region { get; }

    IServiceProvider IInstance<IServiceProvider>.Instance => Region?.Services;

    public Route CurrentRoute
    {
        get => _currentRoute.Merge(Region.Children.Select(x => x.Services?.GetService<INavigator>()?.CurrentRoute));
        set
        {
            var route = value;
            if (route is not null &&
               !string.IsNullOrWhiteSpace(this.Region.Name))
            {
                route = route with { Base = this.Region.Name, Path = Schemes.Separator + route.Base + route.Path };
            }
            _currentRoute = route;
        }
    }

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

    public async Task<NavigationResponse> NavigateAsync(NavigationRequest request)
    {
        Logger.LogInformation($"Pre-navigation: - {Region.ToString()}");
        try
        {
            // Handle root navigations
            if (request?.Route?.IsRoot() ?? false)
            {
                if (!IsRoot)
                {
                    return await (Region.Parent?.NavigateAsync(request) ?? Task.FromResult<NavigationResponse>(default));
                }
                else
                {
                    // This is the root nav service - need to pass the
                    // request down to children by making the request nested
                    request = request with { Route = request.Route.TrimScheme(Schemes.Root) };
                }
            }

            if (request?.Route?.IsParent() ?? false)
            {
                request = request with { Route = request.Route.TrimScheme(Schemes.Parent) };

                // Handle parent navigations
                if (request?.Route?.IsParent() ?? false)
                {
                    return await (Region.Parent?.NavigateAsync(request) ?? Task.FromResult<NavigationResponse>(default));
                }
            }

            // Run dialog requests
            if (request.Route.IsDialog())
            {
                request = request with { Route = request.Route with { Scheme = Schemes.Current } };
                return await DialogNavigateAsync(request);
            }

            // If the base matches the region name, than need to strip the base
            if (request.Route.Base == Region.Name)
            {
                request = request with { Route = request.Route.NextRoute()};
            }

            return await CoreNavigateAsync(request);
        }
        finally
        {
            Logger.LogInformation($"Post-navigation: {Region.ToString()}");
            Logger.LogInformation($"Post-navigation (route): {Region.Root().Navigator().CurrentRoute}");
        }
    }

    private async Task<NavigationResponse> DialogNavigateAsync(NavigationRequest request)
    {
        var dialogService = Region.NavigatorFactory().CreateService(Region, request);

        var dialogResponse = await dialogService.NavigateAsync(request);

        return dialogResponse;
    }

    protected virtual async Task<NavigationResponse> CoreNavigateAsync(NavigationRequest request)
    {
        if (request.Route.IsNested())
        {
            // At this point the request should be passed to nested, so remove
            // any nested scheme (ie ./ )
            request = request with { Route = request.Route with { Scheme = Schemes.Current } };
        }

        if (request.Route.IsEmpty())
        {
            return null;
        }

        var children = Region.Children.Where(region =>
                                       region.Name is not { Length: > 0 } ||
                                       region.Name == request.Route.Base
                                    ).ToArray();

        var tasks = new List<Task<NavigationResponse>>();
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
