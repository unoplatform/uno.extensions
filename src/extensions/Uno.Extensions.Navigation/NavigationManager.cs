using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Adapters;

namespace Uno.Extensions.Navigation;

public class NavigationManager : INavigationManager
{
    private INavigationService Root { get; }

    private IServiceProvider Services { get; }

    private INavigationMapping Mapping { get; }

    private IDictionary<Type, IAdapterFactory> Factories { get; }

    public NavigationManager(IServiceProvider services, IEnumerable<IAdapterFactory> factories, INavigationMapping mapping)
    {
        Services = services;
        Mapping = mapping;
        Factories = factories.ToDictionary(x => x.ControlType);
        Root = new NavigationService(this, Mapping, null);
    }

    public INavigationService AddAdapter(INavigationService parentAdapter, string routeName, object control, INavigationService existingAdapter)
    {
        var ans = existingAdapter as NavigationService;
        var parent = parentAdapter as NavigationService;
        if (ans is null)
        {
            ans = new NavigationService(this, Mapping, parent);
            var scope = Services.CreateScope();
            var services = scope.ServiceProvider;
            var navWrapper = services.GetService<NavigationServiceProvider>();
            navWrapper.Navigation = ans;

            var factory = Factories[control.GetType()];
            var adapter = factory.Create(services);
            adapter.Inject(control);
            ans.Adapter = adapter;
        }

        // This ensures all adapter services have a parent. The root service
        // is used to cache initial navigation requests before the first
        // adapter is created
        if (parent is null)
        {
            parent = Root as NavigationService;
        }

        parent.NestedAdapters[routeName + string.Empty] = ans;

        if (ans.Adapter is INavigationAware navAware)
        {
            navAware.Navigation = ans;
        }

        if (parent.PendingNavigation is not null)
        {
            var pending = parent.PendingNavigation;
            parent.PendingNavigation = null;
            if (pending.Route.Path.OriginalString.StartsWith(routeName))
            {
                var nestedRoute = pending.Route.Path.OriginalString.TrimStart($"{routeName}/");
                pending = pending with { Route = pending.Route with { Path = new Uri(nestedRoute, UriKind.Relative) } };
            }
            ans.Navigate(pending);
        }

        return ans;
    }

    public void RemoveAdapter(INavigationService adapter)
    {
        var ans = adapter as NavigationService;
        if (ans is null)
        {
            return;
        }

        // Detach adapter from parent
        var parent = adapter.Parent as NavigationService;
        if (parent is not null)
        {
            parent.NestedAdapters.Remove(kvp => kvp.Value == adapter);
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
