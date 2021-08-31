using System;
using System.Collections.Generic;
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
    public partial class Navigation : DependencyObject
    {
        public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
          "IsEnabled",
          typeof(bool),
          typeof(Navigation),
          new PropertyMetadata(false, IsEnabledChanged)
        );

        private static void IsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                RegisterElement(element);
            }
        }

        private static void RegisterElement(FrameworkElement element)
        {
            var nav = Ioc.Default.GetService<INavigationManager>();

            element.Loaded += (s, e) =>
            {
                var adapter = element switch
                {
                    Frame frame => nav.AddAdapter(frame, false),
                    TabView tabs => nav.AddAdapter(tabs, false),
                    _ => default
                };
                var predicates = new List<Func<bool>>();
                var disposes = new List<Action>();
                Action updateActivation = () =>
                {
                    var enabled = true;
                    foreach (var p in predicates)
                    {
                        if (!p())
                        {
                            enabled = false;
                            break;
                        }
                    }
                    if (enabled)
                    {
                        nav.ActivateAdapter(adapter);
                    }
                    else
                    {
                        nav.DeactivateAdapter(adapter, false);
                    }
                };
                WalkHierarchy(element, predicates, updateActivation, disposes);
                updateActivation();

                element.Unloaded += (s, e) =>
                {
                    if (adapter != null)
                    {
                        foreach (var d in disposes)
                        {
                            d();
                        }
                        nav.DeactivateAdapter(adapter);
                    }
                };
            };
        }

        private static void WalkHierarchy(FrameworkElement element, List<Func<bool>> predicates, Action updateActivation, List<Action> disposes)
        {
            var parent = VisualTreeHelper.GetParent(element);
            var elements = new List<DependencyObject>();
            elements.Add(element);
            while (parent != null)
            {
                switch (parent)
                {
                    case Frame frame:
                        break;
                    case TabView tabs:
                        var tab = (from t in tabs.TabItems.OfType<TabViewItem>()
                                   where elements.Contains(t.Content)
                                   select t).FirstOrDefault();
                        if (tab is null)
                        {
                            break;
                        }

                        predicates.Add(() => tabs.SelectedItem as TabViewItem == tab);
                        SelectionChangedEventHandler handler = (s, e) => updateActivation();
                        tabs.SelectionChanged += handler;
                        disposes.Add(() => tabs.SelectionChanged -= handler);
                        break;
                }
                elements.Add(parent);
                parent = VisualTreeHelper.GetParent(parent);
            }
        }

        public static void SetIsEnabled(FrameworkElement element, bool value)
        {
            element.SetValue(IsEnabledProperty, value);
        }

        public static bool GetIsEnabled(FrameworkElement element)
        {
            return (bool)element.GetValue(IsEnabledProperty);
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
                        var nav = Ioc.Default.GetService<INavigationService>();
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
