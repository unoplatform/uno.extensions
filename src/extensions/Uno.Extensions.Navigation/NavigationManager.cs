using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class NavigationManager : INavigationManager
{
    private INavigationService Root { get; }

    private IServiceProvider Services { get; }

    private INavigationMapping Mapping { get; }

    private IDictionary<Type, IRegionManagerFactory> Factories { get; }

    public NavigationManager(IServiceProvider services, IEnumerable<IRegionManagerFactory> factories, INavigationMapping mapping)
    {
        Services = services;
        Mapping = mapping;
        Factories = factories.ToDictionary(x => x.ControlType);
        Root = new NavigationService(this, null, Mapping, null);
    }

    public INavigationService AddRegion(INavigationService parentRegion, string regionName, object control, INavigationService existingRegion)
    {
        var ans = existingRegion as NavigationService;
        var parent = parentRegion as NavigationService;
        if (ans is null)
        {
            var scope = Services.CreateScope();
            var services = scope.ServiceProvider;
            ans = new NavigationService(this, services, Mapping, parent);

            var factory = Factories[control.GetType()];
            var region = factory.Create(services);
            region.Inject(control);
            ans.Region = region;
        }

        // This ensures all adapter services have a parent. The root service
        // is used to cache initial navigation requests before the first
        // adapter is created
        if (parent is null)
        {
            parent = Root as NavigationService;
        }

        parent.NestedRegions[regionName + string.Empty] = ans;

        if (ans.Region is INavigationAware navAware)
        {
            navAware.Navigation = ans;
        }

        if (parent.PendingNavigation is not null)
        {
            var pending = parent.PendingNavigation;
            parent.PendingNavigation = null;
            if (pending.Route.Path.OriginalString.StartsWith(regionName))
            {
                var nestedRoute = pending.Route.Path.OriginalString.TrimStart($"{regionName}/");
                pending = pending with { Route = pending.Route with { Path = new Uri(nestedRoute, UriKind.Relative) } };
            }
            ans.Navigate(pending);
        }

        return ans;
    }

    public void RemoveRegion(INavigationService region)
    {
        var ans = region as NavigationService;
        if (ans is null)
        {
            return;
        }

        // Detach adapter from parent
        var parent = region.Parent as NavigationService;
        if (parent is not null)
        {
            parent.NestedRegions.Remove(kvp => kvp.Value == region);
        }
    }

    public INavigationService Parent => Root;

    public NavigationResponse Navigate(NavigationRequest request)
    {
        return Root.Navigate(request);
    }

    public INavigationService Nested(string routeName = null)
    {
        return Root.Nested(routeName);
    }
}
