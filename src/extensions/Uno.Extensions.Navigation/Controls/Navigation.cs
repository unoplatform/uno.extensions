using System;
using CommunityToolkit.Mvvm.Input;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
#endif

namespace Uno.Extensions.Navigation.Controls;

public static class Navigation
{
    public static readonly DependencyProperty RequestProperty =
        DependencyProperty.RegisterAttached(
            "Request",
            typeof(string),
            typeof(Navigation),
            new PropertyMetadata(null, RequestChanged));

    public static readonly DependencyProperty RouteProperty =
        DependencyProperty.RegisterAttached(
            "Route",
            typeof(string),
            typeof(Navigation),
            new PropertyMetadata(null));

    private static void RequestChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ButtonBase element)
        {
            var path = GetRequest(element);
            var command = new AsyncRelayCommand(async () =>
            {
                var nav = element.Navigator();
                await nav.NavigateByPathAsync(element, path);
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

    public static string NavigationRoute(this object view, IRouteMappings mappings = null)
    {
        var map = mappings?.FindByView(view.GetType());
        if (map is not null)
        {
            return map.Path;
        }

        if (view is FrameworkElement fe)
        {
            var path = GetRoute(fe);
            if (string.IsNullOrWhiteSpace(path))
            {
                path = fe.Name;
            }

            return path;
        }

        return null;
    }
}
