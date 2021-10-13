using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
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

    public static readonly DependencyProperty CompositeProperty =
        DependencyProperty.RegisterAttached(
            "Composite",
            typeof(bool),
            typeof(Navigation),
            new PropertyMetadata(false, CompositeChanged));

    private static IRegionNavigationServiceFactory navigationServiceFactory;

    private static IRegionNavigationServiceFactory NavigationServiceFactory
    {
        get
        {
            return navigationServiceFactory ?? (navigationServiceFactory = Ioc.Default.GetService<IRegionNavigationServiceFactory>());
        }
    }

    private static ILogger logger;

    private static ILogger Logger
    {
        get
        {
            return logger ?? (logger = Ioc.Default.GetService<ILogger<NavigationServiceFactory>>());
        }
    }

    private static void AttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, string.Empty, false);
        }
    }

    private static void NameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, e.NewValue as string, false);
        }
    }

    private static void CompositeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, string.Empty, true);
        }
    }

    private static void RegisterElement(FrameworkElement element, string regionName, bool isComposite)
    {
        Logger.LazyLogDebug(() => $"Attaching to Loaded event on element {element.GetType().Name}");

        var parent = new PlaceholderRegionNavigationService();
        var navRegion = element.RegionNavigationServiceForControl(false) ?? NavigationServiceFactory.CreateService(parent, element, isComposite);

        element.Loaded += async (sLoaded, eLoaded) =>
        {
            Logger.LazyLogDebug(() => $"Creating region manager");
            var loadedparent = element.Parent.RegionNavigationServiceForControl(true) ?? Ioc.Default.GetService<IRegionNavigationService>();
            parent.NavigationService = loadedparent;
            Logger.LazyLogDebug(() => $"Region manager created");

            var loadedElement = sLoaded as FrameworkElement;

            Logger.LazyLogDebug(() => $"Attaching to Unloaded event on element {element.GetType().Name}");
            loadedElement.Unloaded += (sUnloaded, eUnloaded) =>
            {
                if (navRegion != null)
                {
                    Logger.LazyLogDebug(() => $"Removing region manager");
                    parent.Detach(navRegion);
                }
            };

            Logger.LazyLogDebug(() => $"Attaching region manager");
            parent.Attach(navRegion, regionName);
        };
    }

    public static TElement AsNavigationContainer<TElement>(this TElement element)
        where TElement : FrameworkElement
    {
        element.SetValue(AttachedProperty, true);
        return element;
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

    public static void SetComposite(FrameworkElement element, bool value)
    {
        element.SetValue(CompositeProperty, value);
    }

    public static bool GetComposite(FrameworkElement element)
    {
        return (bool)element.GetValue(CompositeProperty);
    }
}
