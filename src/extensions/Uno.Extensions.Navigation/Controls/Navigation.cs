using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
#endif
using Uno.Extensions.Logging;

namespace Uno.Extensions.Navigation.Controls;

public static class Navigation
{
    private static INavigationServiceFactory navigationServiceFactory;

    private static INavigationServiceFactory NavigationServiceFactory
    {
        get
        {
            return navigationServiceFactory ?? (navigationServiceFactory = Ioc.Default.GetService<INavigationServiceFactory>());
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

    public static readonly DependencyProperty RegionProperty =
   DependencyProperty.RegisterAttached(
     "Region",
     typeof(IRegionNavigationService),
     typeof(Navigation),
     new PropertyMetadata(null)
   );

    public static readonly DependencyProperty IsRegionProperty =
    DependencyProperty.RegisterAttached(
      "IsRegion",
      typeof(bool),
      typeof(Navigation),
      new PropertyMetadata(false, IsRegionChanged)
    );

    private static void IsRegionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, string.Empty);
        }
    }

    public static readonly DependencyProperty RegionNameProperty =
    DependencyProperty.RegisterAttached(
      "RegionName",
      typeof(string),
      typeof(Navigation),
      new PropertyMetadata(false, RegionNameChanged)
    );

    private static void RegionNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, e.NewValue as string);
        }
    }

    public static readonly DependencyProperty ComposeWithProperty =
DependencyProperty.RegisterAttached(
  "ComposeWith",
  typeof(FrameworkElement),
  typeof(Navigation),
  new PropertyMetadata(null, ComposeWithChanged)
);

    private static void ComposeWithChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, string.Empty, e.NewValue as FrameworkElement);
        }
    }

    private static void RegisterElement(FrameworkElement element, string regionName, FrameworkElement composeWith = null)
    {
        Logger.LazyLogDebug(() => $"Attaching to Loaded event on element {element.GetType().Name}");
        element.Loaded += async (sLoaded, eLoaded) =>
        {
            Logger.LazyLogDebug(() => $"Creating region manager");
            var loadedElement = sLoaded as FrameworkElement;
            var parent = ScopedServiceForControl(loadedElement.Parent) ?? NavigationServiceFactory.Root;
            var navRegion = loadedElement.GetRegion() ?? NavigationServiceFactory.CreateService(parent, loadedElement, composeWith);

            loadedElement.SetRegion(navRegion);
            if (composeWith is not null)
            {
                composeWith.SetRegion(navRegion);
            }
            Logger.LazyLogDebug(() => $"Region manager created");

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
            parent.Attach(regionName, navRegion);
        };
    }

    public static void SetRegion(this FrameworkElement element, IRegionNavigationService value)
    {
        element.SetValue(RegionProperty, value);
    }

    public static IRegionNavigationService GetRegion(this FrameworkElement element)
    {
        if (element is null)
        {
            return null;
        }
        return (IRegionNavigationService)element.GetValue(RegionProperty);
    }

    public static TElement AsNavigationContainer<TElement>(this TElement element)
        where TElement : FrameworkElement
    {
        element.SetValue(IsRegionProperty, true);
        return element;
    }

    public static void SetIsRegion(FrameworkElement element, bool value)
    {
        element.SetValue(IsRegionProperty, value);
    }

    public static bool GetIsRegion(FrameworkElement element)
    {
        return (bool)element.GetValue(IsRegionProperty);
    }

    public static void SetRegionName(FrameworkElement element, string value)
    {
        element.SetValue(RegionNameProperty, value);
    }

    public static string GetRegionName(FrameworkElement element)
    {
        return (string)element.GetValue(RegionNameProperty);
    }

    public static void SetComposeWith(FrameworkElement element, FrameworkElement value)
    {
        element.SetValue(ComposeWithProperty, value);
    }

    public static FrameworkElement GetComposeWith(FrameworkElement element)
    {
        return (FrameworkElement)element.GetValue(ComposeWithProperty);
    }

    public static readonly DependencyProperty NavigateOnClickPathProperty =
                DependencyProperty.RegisterAttached(
                  "NavigateOnClickPath",
                  typeof(string),
                  typeof(Navigation),
                  new PropertyMetadata(null, NavigateOnClickPathChanged)
                );

    public static readonly DependencyProperty PathProperty =
                DependencyProperty.RegisterAttached(
                  "Path",
                  typeof(string),
                  typeof(Navigation),
                  new PropertyMetadata(null)
                );

    private static void NavigateOnClickPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Make sure we set the Path property too
        d.SetValue(PathProperty, e.NewValue);

        if (d is ButtonBase element)
        {
            var path = GetNavigateOnClickPath(element);
            RoutedEventHandler handler = async (s, e) =>
                {
                    try
                    {
                        var nav = ScopedServiceForControl(s as DependencyObject);
                        await nav.NavigateByPathAsync(s, path);
                    }
                    catch (Exception ex)
                    {
                        Logger.LazyLogError(() => $"Navigation failed - {ex.Message}");
                    }
                };
            element.Loaded += (s, e) =>
            {
                element.Click += handler;
            };
            element.Unloaded += (s, e) =>
            {
                element.Click -= handler;
            };
        }
    }

    private static IRegionNavigationService ScopedServiceForControl(DependencyObject element)
    {
        var service = (element as FrameworkElement).GetRegion();
        if (service is not null)
        {
            return service;
        }

        var parent = VisualTreeHelper.GetParent(element);
        // If parent is null, we're at top of visual tree,
        // so just return the nav manager itself
        return parent is not null ? ScopedServiceForControl(parent) : null;
    }

    public static void SetPath(FrameworkElement element, string value)
    {
        element.SetValue(PathProperty, value);
    }

    public static string GetPath(FrameworkElement element)
    {
        return (string)element.GetValue(PathProperty);
    }

    public static void SetNavigateOnClickPath(FrameworkElement element, string value)
    {
        element.SetValue(NavigateOnClickPathProperty, value);
    }

    public static string GetNavigateOnClickPath(FrameworkElement element)
    {
        return (string)element.GetValue(NavigateOnClickPathProperty);
    }
}
