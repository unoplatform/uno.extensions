using System;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Regions;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
#endif

namespace Uno.Extensions.Navigation.Controls;

public static class Dependency
{
    public static readonly DependencyProperty ServiceProviderProperty =
        DependencyProperty.RegisterAttached(
            "ServiceProvider",
            typeof(IServiceProvider),
            typeof(ServiceProvider),
            new PropertyMetadata(null));

    public static void SetServiceProvider(this FrameworkElement element, IServiceProvider value)
    {
        element.SetValue(ServiceProviderProperty, value);
    }

    public static IServiceProvider GetServiceProvider(this FrameworkElement element)
    {
        if (element is null)
        {
            return null;
        }
        return (IServiceProvider)element.GetValue(ServiceProviderProperty);
    }

    //public static IRegionNavigationService RegionNavigationServiceForControl(this DependencyObject element, bool searchParent)
    //{
    //    return element.ServiceForControl<IRegionNavigationService>(searchParent);
    //}

    public static INavigationService Navigator(this DependencyObject element)
    {
        return element.Region().Navigation().AsInner();
    }

    public static IRegion Region(this DependencyObject element)
    {
        return (element as FrameworkElement).ServiceForControl<IRegion>(true, element => element.GetRegion());
    }

    public static IServiceProvider ServiceProviderForControl(this DependencyObject element)
    {
        return element.ServiceForControl<IServiceProvider>(true, element => (element as FrameworkElement)?.GetServiceProvider());
    }

    private static TService ServiceForControl<TService>(this DependencyObject element, bool searchParent)
    {
        return element.ServiceForControl(searchParent, element =>
        {
            var sp = GetServiceProvider(element as FrameworkElement);
            if (sp is not null)
            {
                return sp.GetService<TService>();
            }

            return default;
        });
        //if (element is null)
        //{
        //    return default;
        //}

        //var sp = GetServiceProvider(element as FrameworkElement);
        //if (sp is not null)
        //{
        //    var service = sp.GetService<TService>();
        //    if (service is not null)
        //    {
        //        return service;
        //    }
        //}

        //if (!searchParent)
        //{
        //    return default;
        //}

        //var parent = VisualTreeHelper.GetParent(element);
        //return parent.ServiceForControl<TService>(searchParent);
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
