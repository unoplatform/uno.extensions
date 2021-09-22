﻿using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Uno.Extensions.Logging;
using Microsoft.Extensions.Logging;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
#endif

namespace Uno.Extensions.Navigation.Controls;

public static class Navigation
{
    private static INavigationManager navigationManager;

    private static INavigationManager NavigationManager
    {
        get
        {
            return navigationManager ?? (navigationManager = Ioc.Default.GetService<INavigationManager>());
        }
    }

    private static ILogger logger;

    private static ILogger Logger
    {
        get
        {
            return logger ?? (logger = Ioc.Default.GetService(typeof(ILogger<NavigationManager>)) as ILogger);
        }
    }

    public static readonly DependencyProperty RegionManagerProperty =
   DependencyProperty.RegisterAttached(
     "RegionManager",
     typeof(INavigationService),
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

    private static void RegisterElement(FrameworkElement element, string regionName)
    {
        Logger.LazyLogDebug(() => $"Attaching to Loaded event on element {element.GetType().Name}");
        element.Loaded += (sLoaded, eLoaded) =>
        {
            Logger.LazyLogDebug(() => $"Creating region manager");
            var loadedElement = sLoaded as FrameworkElement;
            var existingRegion = loadedElement.GetRegionManager();
            var parent = ScopedServiceForControl(loadedElement.Parent);
            var region = NavigationManager.AddRegion(parent, regionName, element, existingRegion);
            loadedElement.SetRegionManager(region);
            Logger.LazyLogDebug(() => $"Region manager created");

            Logger.LazyLogDebug(() => $"Attaching to Unloaded event on element {element.GetType().Name}");
            loadedElement.Unloaded += (sUnloaded, eUnloaded) =>
           {
               if (region != null)
               {
                   Logger.LazyLogDebug(() => $"Removing region manager");
                   NavigationManager.RemoveRegion(region);
               }
           };
        };
    }

    public static void SetRegionManager(this FrameworkElement element, INavigationService value)
    {
        element.SetValue(RegionManagerProperty, value);
    }

    public static INavigationService GetRegionManager(this FrameworkElement element)
    {
        if (element is null)
        {
            return null;
        }
        return (INavigationService)element.GetValue(RegionManagerProperty);
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

    public static readonly DependencyProperty PathProperty =
                DependencyProperty.RegisterAttached(
                  "Path",
                  typeof(string),
                  typeof(Navigation),
                  new PropertyMetadata(null, PathChanged)
                );

    private static void PathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Button element)
        {
            var path = GetPath(element);
            RoutedEventHandler handler = (s, e) =>
                {
                    var nav = ScopedServiceForControl(s as DependencyObject);
                    nav.NavigateAsync(new NavigationRequest(s, new NavigationRoute(new Uri(path, UriKind.Relative))));
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

    private static INavigationService ScopedServiceForControl(DependencyObject element)
    {
        var service = (element as FrameworkElement).GetRegionManager();
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
}
