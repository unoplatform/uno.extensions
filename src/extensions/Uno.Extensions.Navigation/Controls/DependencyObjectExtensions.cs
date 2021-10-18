using System;
using Uno.Extensions.Navigation.Regions;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
#endif

namespace Uno.Extensions.Navigation.Controls;

public static class DependencyObjectExtensions
{
    public static INavigator Navigator(this DependencyObject element)
    {
        return element.FindRegion().Navigation();
    }

    public static IRegion FindRegion(this DependencyObject element)
    {
        return element.ServiceForControl<IRegion>(true, element => Region.GetInstance(element));
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
        return parent.ServiceForControl<TService>(searchParent, retrieveFromElement);
    }
}
