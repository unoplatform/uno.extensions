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

    public static readonly DependencyProperty AttachedProperty =
    DependencyProperty.RegisterAttached(
      "Attached",
      typeof(bool),
      typeof(Navigation),
      new PropertyMetadata(false, AttachedChanged)
    );

    private static void AttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, string.Empty, false);
        }
    }

    public static readonly DependencyProperty NameProperty =
    DependencyProperty.RegisterAttached(
      "Name",
      typeof(string),
      typeof(Navigation),
      new PropertyMetadata(false, NameChanged)
    );

    private static void NameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, e.NewValue as string, false);
        }
    }

    public static readonly DependencyProperty CompositeProperty =
DependencyProperty.RegisterAttached(
  "Composite",
  typeof(bool),
  typeof(Navigation),
  new PropertyMetadata(false, CompositeChanged)
);

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

        Logger.LazyLogDebug(() => $"Creating region manager");
        var parent = element.Parent.RegionNavigationServiceForControl(true) ?? Ioc.Default.GetService<IRegionNavigationService>();
        var navRegion = element.RegionNavigationServiceForControl(false) ?? NavigationServiceFactory.CreateService(parent, element, isComposite);
        Logger.LazyLogDebug(() => $"Region manager created");

        element.Loaded += async (sLoaded, eLoaded) =>
        {
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
            parent.Attach(regionName, navRegion);
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

    public static readonly DependencyProperty RequestProperty =
                DependencyProperty.RegisterAttached(
                  "Request",
                  typeof(string),
                  typeof(Navigation),
                  new PropertyMetadata(null, RequestChanged)
                );

    public static readonly DependencyProperty RouteProperty =
                DependencyProperty.RegisterAttached(
                  "Route",
                  typeof(string),
                  typeof(Navigation),
                  new PropertyMetadata(null)
                );

    private static void RequestChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Make sure we set the Route property too
        d.SetValue(RouteProperty, e.NewValue);

        if (d is ButtonBase element)
        {
            var path = GetRequest(element);
            var command = new AsyncRelayCommand(async () =>
            {
                try
                {
                    var nav = element.NavigationServiceForControl(true);
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



    public static void SetRoute(FrameworkElement element, string value)
    {
        element.SetValue(RouteProperty, value);
    }

    public static string GetRoute(FrameworkElement element)
    {
        return (string)element.GetValue(RouteProperty);
    }

    public static void SetRequest(FrameworkElement element, string value)
    {
        element.SetValue(RequestProperty, value);
    }

    public static string GetRequest(FrameworkElement element)
    {
        return (string)element.GetValue(RequestProperty);
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
