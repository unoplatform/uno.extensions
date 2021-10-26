using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Regions;

public static class RegionExtensions
{
    public static INavigator Navigator(this IRegion region)
    {
        if (string.IsNullOrWhiteSpace(region.Name))
        {
            var parentNavigator = region.Parent?.Services?.GetService<INavigator>();
            if (parentNavigator is not null &&
                parentNavigator.GetType() == typeof(Navigator) &&
                region.Parent.Children.Count > 1)
            {
                return parentNavigator;
            }
        }

        return region.Services?.GetService<INavigator>();
    }

    public static INavigatorFactory NavigatorFactory(this IRegion region) => region.Services?.GetService<INavigatorFactory>();

    public static Task<NavigationResponse> NavigateAsync(this IRegion region, NavigationRequest request) => region.Services?.GetService<INavigator>()?.NavigateAsync(request);

    public static void Attach(this IRegion region, IRegion childRegion)
    {
        region.Children.Add(childRegion);
    }

    public static void Detach(this IRegion region, IRegion childRegion)
    {
        region.Children.Remove(childRegion);
    }

    public static void AttachAll(this IRegion region, IEnumerable<IRegion> children)
    {
        children.ForEach(n => region.Attach(n));
    }

    public static IEnumerable<IRegion> DetachAll(this IRegion region)
    {
        var children = region.Children.ToArray();
        children.ForEach(child => region.Detach(child));
        return children;
    }

    public static IRegion Root(this IRegion region)
    {
        return region.Parent is not null ? region.Parent.Root() : region;
    }
}
