using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
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
using Uno.Extensions.Logging;

namespace Uno.Extensions.Navigation.Controls;

public static class Navigation
{
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

    public static readonly DependencyProperty NavigationServiceProperty =
   DependencyProperty.RegisterAttached(
     "NavigationService",
     typeof(INavigationService),
     typeof(Navigation),
     new PropertyMetadata(null)
   );

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
            RegisterElement(element, string.Empty, false);
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
            RegisterElement(element, e.NewValue as string, false);
        }
    }

    public static readonly DependencyProperty IsCompositeRegionProperty =
DependencyProperty.RegisterAttached(
  "IsCompositeRegion",
  typeof(bool),
  typeof(Navigation),
  new PropertyMetadata(false, IsCompositeRegionChanged)
);

    private static void IsCompositeRegionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, string.Empty, true);
        }
    }

    private static void RegisterElement(FrameworkElement element, string regionName, bool isComposite)
    {
        Logger.LazyLogDebug(() => $"Attaching to Loaded event on element {element.GetType().Name}");

        Logger.LazyLogDebug(() => $"Creating region manager");
        var navRegion = element.GetRegion() ?? NavigationServiceFactory.CreateService(element, isComposite);
        element.SetRegion(navRegion);
        if (isComposite)
        {
            element.SetNavigationService(navRegion);
        }
        Logger.LazyLogDebug(() => $"Region manager created");

        element.Loaded += async (sLoaded, eLoaded) =>
        {
            var loadedElement = sLoaded as FrameworkElement;
            var parent = RegionForControl(loadedElement.Parent) ?? Ioc.Default.GetService<IRegionNavigationService>();

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

    public static void SetNavigationService(this FrameworkElement element, INavigationService value)
    {
        element.SetValue(NavigationServiceProperty, value);
    }

    public static INavigationService GetNavigationService(this FrameworkElement element)
    {
        if (element is null)
        {
            return null;
        }
        return (INavigationService)element.GetValue(NavigationServiceProperty);
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

    public static void SetIsRegion(DependencyObject element, bool value)
    {
        element.SetValue(IsRegionProperty, value);
    }

    public static bool GetIsRegion(DependencyObject element)
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

    public static void SetIsCompositeRegion(FrameworkElement element, bool value)
    {
        element.SetValue(IsCompositeRegionProperty, value);
    }

    public static bool GetIsCompositeRegion(FrameworkElement element)
    {
        return (bool)element.GetValue(IsCompositeRegionProperty);
    }

    public static readonly DependencyProperty NavigateOnClickRouteProperty =
                DependencyProperty.RegisterAttached(
                  "NavigateOnClickRoute",
                  typeof(string),
                  typeof(Navigation),
                  new PropertyMetadata(null, NavigateOnClickRouteChanged)
                );

    public static readonly DependencyProperty RouteProperty =
                DependencyProperty.RegisterAttached(
                  "Route",
                  typeof(string),
                  typeof(Navigation),
                  new PropertyMetadata(null)
                );

    private static void NavigateOnClickRouteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Make sure we set the Route property too
        d.SetValue(RouteProperty, e.NewValue);

        if (d is ButtonBase element)
        {
            var path = GetNavigateOnClickRoute(element);
            var command = new AsyncRelayCommand(async () =>
            {
                try
                {
                    var nav = NavigationServiceForControl(element);
                    await nav.NavigateByPathAsync(element, path);
                }
                catch (Exception ex)
                {
                    Logger.LazyLogError(() => $"Navigation failed - {ex.Message}");
                }
            });
            var binding = new Binding { Source = command, Path = new PropertyPath(nameof(command.IsRunning)), Converter = new InvertConverter() };

            element.Loaded += (s, e) =>
            {
                element.Command = command;
                element.SetBinding(ButtonBase.IsEnabledProperty, binding);
            };
            element.Unloaded += (s, e) =>
            {
                element.Command = null;
                element.ClearValue(ButtonBase.IsEnabledProperty);
            };
        }
    }

    private static IRegionNavigationService RegionForControl(DependencyObject element)
    {
        return ServiceForControl(element, GetRegion);
    }

    private static INavigationService NavigationServiceForControl(DependencyObject element)
    {
        return ServiceForControl(element, GetNavigationService);
    }

    private static TService ServiceForControl<TService>(DependencyObject element, Func<FrameworkElement, TService> getService)
    {
        var service = getService(element as FrameworkElement);
        if (service is not null)
        {
            return service;
        }

        var parent = VisualTreeHelper.GetParent(element);
        // If parent is null, we're at top of visual tree,
        // so just return the nav manager itself
        return parent is not null ? ServiceForControl(parent, getService) : default;
    }

    public static void SetRoute(FrameworkElement element, string value)
    {
        element.SetValue(RouteProperty, value);
    }

    public static string GetRoute(FrameworkElement element)
    {
        return (string)element.GetValue(RouteProperty);
    }

    public static void SetNavigateOnClickRoute(FrameworkElement element, string value)
    {
        element.SetValue(NavigateOnClickRouteProperty, value);
    }

    public static string GetNavigateOnClickRoute(FrameworkElement element)
    {
        return (string)element.GetValue(NavigateOnClickRouteProperty);
    }

    private class InvertConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
