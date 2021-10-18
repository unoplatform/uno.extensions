using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Regions;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
#endif

namespace Uno.Extensions.Navigation.Controls;

public static class Region
{
    public static readonly DependencyProperty RegionProperty =
       DependencyProperty.RegisterAttached(
           "Region",
           typeof(IRegion),
           typeof(Navigation),
           new PropertyMetadata(null));

    public static readonly DependencyProperty AttachedProperty =
        DependencyProperty.RegisterAttached(
            "Attached",
            typeof(bool),
            typeof(Navigation),
            new PropertyMetadata(false, AttachedChanged));

    public static readonly DependencyProperty NameProperty =
        DependencyProperty.RegisterAttached(
            "Name",
            typeof(string),
            typeof(Navigation),
            new PropertyMetadata(false, NameChanged));

    private static void AttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, string.Empty);
        }
    }

    private static void NameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, e.NewValue as string);
        }
    }

    private static void RegisterElement(FrameworkElement element, string regionName)
    {
        var existingRegion = element.GetRegion();
        var region = existingRegion ?? new NavigationRegion(regionName, element);
        element.SetRegion(region);
    }

    public static TElement AsNavigationContainer<TElement>(this TElement element, IServiceProvider services)
        where TElement : FrameworkElement
    {
        // Create the Root region
        var rootRegion = new NavigationRegion(String.Empty, null, services);
        services.AddInstance<INavigator>(new InnerNavigator(services.GetInstance<INavigator>()));

        // Create the element region
        var elementRegion = new NavigationRegion(String.Empty, element, rootRegion);
        element.SetRegion(elementRegion);

        return element;
    }

    public static void SetRegion(this DependencyObject element, IRegion value)
    {
        element.SetValue(RegionProperty, value);
    }

    public static IRegion GetRegion(this DependencyObject element)
    {
        return (IRegion)element.GetValue(RegionProperty);
    }

    public static void SetAttached(DependencyObject element, bool value)
    {
        element.SetValue(AttachedProperty, value);
    }

    public static bool GetAttached(DependencyObject element)
    {
        return (bool)element.GetValue(AttachedProperty);
    }

    public static void SetName(FrameworkElement element, string value)
    {
        element.SetValue(NameProperty, value);
    }

    public static string GetName(FrameworkElement element)
    {
        return (string)element.GetValue(NameProperty);
    }
}
