using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
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
                //try
                //{
                    var nav = element.NavigationServiceForControl(true);
                    await nav.NavigateByPathAsync(element, path);
                //}
                //catch (Exception ex)
                //{
                //    Logger.LazyLogError(() => $"Navigation failed - {ex.Message}");
                //}
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
