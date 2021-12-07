using System;
using System.Diagnostics;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation.UI;

public static class Region
{
    public static readonly DependencyProperty InstanceProperty =
       DependencyProperty.RegisterAttached(
           "Instance",
           typeof(IRegion),
           typeof(Navigation),
           new PropertyMetadata(null));

    public static readonly DependencyProperty AttachedProperty =
        DependencyProperty.RegisterAttached(
            "Attached",
            typeof(bool),
            typeof(Navigation),
            new PropertyMetadata(false, AttachedChanged));

    public static readonly DependencyProperty ParentProperty =
        DependencyProperty.RegisterAttached(
            "Parent",
            typeof(FrameworkElement),
            typeof(Navigation),
            new PropertyMetadata(null));

    public static readonly DependencyProperty NameProperty =
        DependencyProperty.RegisterAttached(
            "Name",
            typeof(string),
            typeof(Navigation),
            new PropertyMetadata(null));

    public static readonly DependencyProperty NavigatorProperty =
        DependencyProperty.RegisterAttached(
            "Navigator",
            typeof(string),
            typeof(Navigation),
            new PropertyMetadata(null));

    private static void AttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
        {
            return;
        }

        if (d is FrameworkElement element)
        {
            RegisterElement(element);
        }
    }

    private static void RegisterElement(FrameworkElement element)
    {
        var existingRegion = Region.GetInstance(element);
        var region = existingRegion ?? new NavigationRegion(element);
        element.SetInstance(region);
    }

    public static void SetInstance(this DependencyObject element, IRegion? value)
    {
        element.SetValue(InstanceProperty, value);
    }

    public static IRegion GetInstance(this DependencyObject element)
    {
        return (IRegion)element.GetValue(InstanceProperty);
    }

    public static void SetAttached(DependencyObject element, bool value)
    {
        element.SetValue(AttachedProperty, value);
    }

    public static bool GetAttached(DependencyObject element)
    {
        return (bool)element.GetValue(AttachedProperty);
    }

    public static void SetParent(DependencyObject element, FrameworkElement value)
    {
        element.SetValue(ParentProperty, value);
    }

    public static FrameworkElement GetParent(this DependencyObject element)
    {
        return (FrameworkElement)element.GetValue(ParentProperty);
    }

    public static void SetName(this FrameworkElement element, string? value)
    {
        element.SetValue(NameProperty, value);
    }

    public static void ReassignRegionParent(this FrameworkElement element)
    {

        var childrenCount = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(element, i);
            if (child.GetInstance() is IRegion region &&
                string.IsNullOrWhiteSpace(region.Name))
            {
                region.ReassignParent();
            }
            else
            {
                if (child is FrameworkElement childElement)
                {
                    ReassignRegionParent(childElement);
                }
            }
        }
    }

    public static string GetName(this FrameworkElement element)
    {
        return (string)element.GetValue(NameProperty);
    }

    public static void SetNavigator(this FrameworkElement element, string value)
    {
        element.SetValue(NavigatorProperty, value);
    }

    public static string GetNavigator(this FrameworkElement element)
    {
        return (string)element.GetValue(NavigatorProperty);
    }
}
