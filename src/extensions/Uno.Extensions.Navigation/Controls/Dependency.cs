using System;
using Microsoft.Extensions.DependencyInjection;
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

    public static IRegionNavigationService RegionNavigationServiceForControl(this DependencyObject element, bool searchParent)
    {
        return element.ServiceForControl<IRegionNavigationService>(searchParent);
    }

    public static INavigationService NavigationServiceForControl(this DependencyObject element, bool searchParent)
    {
        return element.ServiceForControl<INavigationService>(searchParent);
    }

    private static TService ServiceForControl<TService>(this DependencyObject element, bool searchParent)
    {
        if (element is null)
        {
            return default;
        }

        var sp = GetServiceProvider(element as FrameworkElement);
        if (sp is not null)
        {
            var service = sp.GetService<TService>();
            if (service is not null)
            {
                return service;
            }
        }

        if (!searchParent)
        {
            return default;
        }

        var parent = VisualTreeHelper.GetParent(element);
        return parent.ServiceForControl<TService>(searchParent);
    }
}
