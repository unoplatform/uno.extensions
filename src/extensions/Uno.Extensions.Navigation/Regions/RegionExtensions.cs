using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Navigators;

namespace Uno.Extensions.Navigation.Regions;

public static class RegionExtensions
{
    public static INavigator Navigator(this IRegion region)
    {
        var parentNavigator = region.Parent?.Services?.GetService<INavigator>();
        if (parentNavigator is not null &&
            (parentNavigator.GetType() == typeof(Navigator) ||
            parentNavigator is ICompositeNavigator))
        {
            return parentNavigator;
        }

        return region.LocalNavigator();
    }

    public static INavigator LocalNavigator(this IRegion region) => region.Services?.GetService<INavigator>();

    public static Task<NavigationResponse> NavigateAsync(this IRegion region, NavigationRequest request) => region.Services?.GetService<INavigator>()?.NavigateAsync(request);

    public static IRegion Root(this IRegion region)
    {
        return region.Parent is not null ? region.Parent.Root() : region;
    }

    public static Route GetRoute(this IRegion region)
    {
        return region.LocalNavigator().Route.Merge(region.Children.Select(x => (x.Name, CurrentRoute: x.GetRoute())));
    }

    public static IEnumerable<IRegion> FindChildren(this IRegion region, Func<IRegion, bool> predicate)
    {
        foreach (var child in region.Children)
        {
            if (predicate(child))
            {
                yield return child;
            }

            foreach (var nested in child.FindChildren(predicate))
            {
                yield return nested;
            }
        }
    }
}
