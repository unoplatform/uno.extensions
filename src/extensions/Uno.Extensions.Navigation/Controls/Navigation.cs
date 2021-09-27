using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Uno.Extensions.Logging;
using Microsoft.Extensions.Logging;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls.Primitives;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
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

    public static readonly DependencyProperty NavigationRegionContainerProperty =
   DependencyProperty.RegisterAttached(
     "NavigationRegionContainer",
     typeof(INavigationRegionContainer),
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

    public static readonly DependencyProperty RegionContentHostProperty =
DependencyProperty.RegisterAttached(
  "RegionContentHost",
  typeof(FrameworkElement),
  typeof(Navigation),
  new PropertyMetadata(null, RegionContentHostChanged)
);

    private static void RegionContentHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, string.Empty, e.NewValue as FrameworkElement);
        }
    }

    private static void RegisterElement(FrameworkElement element, string regionName, object regionContentHost = null)
    {
        Logger.LazyLogDebug(() => $"Attaching to Loaded event on element {element.GetType().Name}");
        element.Loaded += async (sLoaded, eLoaded) =>
        {
            Logger.LazyLogDebug(() => $"Creating region manager");
            var loadedElement = sLoaded as FrameworkElement;
            var parent = ScopedServiceForControl(loadedElement.Parent) ?? NavigationManager.Root;
            var navRegion = loadedElement.GetNavigationRegionContainer() ?? NavigationManager.CreateRegion(loadedElement, regionContentHost);

            navRegion.Navigation.Parent = parent.Navigation;

            loadedElement.SetNavigationRegionContainer(navRegion);
            Logger.LazyLogDebug(() => $"Region manager created");

            Logger.LazyLogDebug(() => $"Attaching to Unloaded event on element {element.GetType().Name}");
            loadedElement.Unloaded += (sUnloaded, eUnloaded) =>
           {
               if (navRegion != null)
               {
                   Logger.LazyLogDebug(() => $"Removing region manager");
                   parent.RegionContainer.RemoveRegion(navRegion.RegionContainer);
               }
           };

            Logger.LazyLogDebug(() => $"Attaching region manager");
            await parent.RegionContainer.AddRegion(regionName, navRegion.RegionContainer);

        };
    }

    public static void SetNavigationRegionContainer(this FrameworkElement element, INavigationRegionContainer value)
    {
        element.SetValue(NavigationRegionContainerProperty, value);
    }

    public static INavigationRegionContainer GetNavigationRegionContainer(this FrameworkElement element)
    {
        if (element is null)
        {
            return null;
        }
        return (INavigationRegionContainer)element.GetValue(NavigationRegionContainerProperty);
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

    public static void SetRegionContentHost(FrameworkElement element, FrameworkElement value)
    {
        element.SetValue(RegionContentHostProperty, value);
    }

    public static FrameworkElement GetRegionContentHost(FrameworkElement element)
    {
        return (FrameworkElement)element.GetValue(RegionContentHostProperty);
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
                        await nav.Navigation.NavigateByPathAsync(s, path);
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

    private static INavigationRegionContainer ScopedServiceForControl(DependencyObject element)
    {
        var service = (element as FrameworkElement).GetNavigationRegionContainer();
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
