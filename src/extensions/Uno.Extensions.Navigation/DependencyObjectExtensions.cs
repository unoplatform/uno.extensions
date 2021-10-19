using System;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Regions;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
#endif

namespace Uno.Extensions.Navigation;

public static class DependencyObjectExtensions
{
    public static INavigator Navigator(this FrameworkElement element)
    {
        return element.FindRegion().Navigator();
    }

    public static IRegion FindRegion(this FrameworkElement element)
    {
        return element.ServiceForControl(true, element => element.GetInstance());
    }

    public static IRegion FindParentRegion(this FrameworkElement element, out string routeName)
    {
        var name = element?.GetName();
        var region = element?.Parent.ServiceForControl(true, element =>
        {
            if (name is not { Length: > 0 })
            {
                var route = (element as FrameworkElement).GetRoute();
                if (route is { Length: > 0 })
                {
                    name = route;
                }
            }
            return element.GetInstance();
        });

        routeName = name;
        return region;
    }

    private static TService ServiceForControl<TService>(this DependencyObject element, bool searchParent, Func<DependencyObject, TService> retrieveFromElement)
    {
        if (element is null)
        {
            return default;
        }

        var service = retrieveFromElement(element);
        if (service is not null)
        {
            return service;
        }

        if (!searchParent)
        {
            return default;
        }

        var parent = VisualTreeHelper.GetParent(element);
        return parent.ServiceForControl(searchParent, retrieveFromElement);
    }
}
