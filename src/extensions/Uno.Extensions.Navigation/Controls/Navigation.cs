using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
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

namespace Uno.Extensions.Navigation.Controls
{
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

        public static readonly DependencyProperty AdapterProperty =
       DependencyProperty.RegisterAttached(
         "Adapter",
         typeof(INavigationService),
         typeof(Navigation),
         new PropertyMetadata(null)
       );

        public static readonly DependencyProperty ContextProperty =
        DependencyProperty.RegisterAttached(
          "Context",
          typeof(NavigationContext),
          typeof(Navigation),
          new PropertyMetadata(null)
        );

        public static readonly DependencyProperty IsContainerProperty =
        DependencyProperty.RegisterAttached(
          "IsContainer",
          typeof(bool),
          typeof(Navigation),
          new PropertyMetadata(false, IsContainerChanged)
        );

        private static void IsContainerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                RegisterElement(element, string.Empty);
            }
        }

        public static readonly DependencyProperty RouteNameProperty =
        DependencyProperty.RegisterAttached(
          "RouteName",
          typeof(string),
          typeof(Navigation),
          new PropertyMetadata(false, RouteNameChanged)
        );

        private static void RouteNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                RegisterElement(element, e.NewValue as string);
            }
        }

        private static void RegisterElement(FrameworkElement element, string routeName)
        {

            element.Loaded += (sLoaded, eLoaded) =>
            {
                var loadedElement = sLoaded as FrameworkElement;
                var existingAdapter = loadedElement.GetAdapter();
                var parent = ScopedServiceForControl(loadedElement.Parent);
                var adapter = NavigationManager.AddAdapter(parent, routeName, element, existingAdapter);
                loadedElement.SetAdapter(adapter);
                loadedElement.Unloaded += (sUnloaded, eUnloaded) =>
               {
                   if (adapter != null)
                   {
                       NavigationManager.RemoveAdapter(adapter);
                   }
               };
            };
        }

        public static void SetAdapter(this FrameworkElement element, INavigationService value)
        {
            element.SetValue(AdapterProperty, value);
        }

        public static INavigationService GetAdapter(this FrameworkElement element)
        {
            if (element is null)
            {
                return null;
            }
            return (INavigationService)element.GetValue(AdapterProperty);
        }
        //public static void SetContext(this FrameworkElement element, NavigationContext value)
        //{
        //    element.SetValue(ContextProperty, value);
        //}

        //public static NavigationContext GetContext(this FrameworkElement element)
        //{
        //    if (element is null)
        //    {
        //        return null;
        //    }
        //    return (NavigationContext)element.GetValue(ContextProperty);
        //}

        public static TElement AsContainer<TElement>(this TElement element)
            where TElement : FrameworkElement
        {
            element.SetValue(IsContainerProperty, true);
            return element;
        }

        public static void SetIsContainer(FrameworkElement element, bool value)
        {
            element.SetValue(IsContainerProperty, value);
        }

        public static bool GetIsContainer(FrameworkElement element)
        {
            return (bool)element.GetValue(IsContainerProperty);
        }

        public static void SetRouteName(FrameworkElement element, string value)
        {
            element.SetValue(RouteNameProperty, value);
        }

        public static string GetRouteName(FrameworkElement element)
        {
            return (string)element.GetValue(RouteNameProperty);
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
                        nav.Navigate(new NavigationRequest(s, new NavigationRoute(new Uri(path, UriKind.Relative))));
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
            var service = (element as FrameworkElement).GetAdapter();
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
}
