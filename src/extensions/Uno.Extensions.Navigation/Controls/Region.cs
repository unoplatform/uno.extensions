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
    public static readonly DependencyProperty InstanceProperty =
       DependencyProperty.RegisterAttached(
           "Instance",
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
            new PropertyMetadata(null, NameChanged));

    private static void AttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element);
        }
    }

    private static void NameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element);
        }
    }

    private static void RegisterElement(FrameworkElement element)
    {
        var existingRegion = Region.GetInstance(element);
        var region = existingRegion ?? new NavigationRegion(element);
        element.SetInstance(region);
    }

    public static TElement AsNavigationContainer<TElement>(this TElement element, IServiceProvider services)
        where TElement : FrameworkElement
    {
        // Create the Root region
        var rootRegion = new NavigationRegion(null, services);
        services.AddInstance<INavigator>(services.GetInstance<INavigator>());

        // Create the element region
        var elementRegion = new NavigationRegion(element, rootRegion);
        element.SetInstance(elementRegion);

        return element;
    }

    public static void SetInstance(this DependencyObject element, IRegion value)
    {
        element.SetValue(InstanceProperty, value);
    }

    public static IRegion GetInstance(this DependencyObject element)
    {
        return (IRegion)element.GetValue(InstanceProperty);
    }

    public static void SetAttached(DependencyObject element, bool value)
    {
        element.SetValue(AttachedProperty, value);
    }

    public static bool GetAttached(DependencyObject element)
    {
        return (bool)element.GetValue(AttachedProperty);
    }

    public static void SetName(this FrameworkElement element, string value)
    {
        element.SetValue(NameProperty, value);
    }

    public static string GetName(this FrameworkElement element)
    {
        return (string)element.GetValue(NameProperty);
    }
}
