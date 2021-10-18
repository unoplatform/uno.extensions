using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Regions;
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
    public static readonly DependencyProperty RegionProperty =
       DependencyProperty.RegisterAttached(
           "Region",
           typeof(IRegion),
           typeof(Navigation),
           new PropertyMetadata(null));

    public static readonly DependencyProperty AttachedProperty =
        DependencyProperty.RegisterAttached(
            "Attached",
            typeof(bool),
            typeof(Navigation),
            new PropertyMetadata(false, AttachedChanged));

    public static readonly DependencyProperty NameProperty =
        DependencyProperty.RegisterAttached(
            "Name",
            typeof(string),
            typeof(Navigation),
            new PropertyMetadata(false, NameChanged));

    public static readonly DependencyProperty CompositeProperty =
        DependencyProperty.RegisterAttached(
            "Composite",
            typeof(bool),
            typeof(Navigation),
            new PropertyMetadata(false, CompositeChanged));

    private static void AttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, string.Empty, false);
        }
    }

    private static void NameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, e.NewValue as string, false);
        }
    }

    private static void CompositeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            RegisterElement(element, string.Empty, true);
        }
    }

    private static void RegisterElement(FrameworkElement element, string regionName, bool isComposite)
    {
        var existingRegion = element.GetRegion();
        var region = existingRegion ?? new NavigationRegion(regionName, element);
        element.SetRegion(region);
        //element.Loaded += async (sLoaded, eLoaded) =>
        //{
        //    var loadedElement = sLoaded as FrameworkElement;
        //    var navService = region.Navigation();

        //    if (navService is null)
        //    {
        //        var services = loadedElement.ServiceProviderForControl(); // Retrieve services from somewhere up the hierarchy
        //        var parent = loadedElement.ParentRegion();// parentServices.GetInstance<IRegion>();
        //        if (parent == region)
        //        {
        //            // This is either the root element, or Loaded has previously been run
        //        }
        //        else
        //        {
        //            parent.Attach(region);
        //            services = services.CreateScope().ServiceProvider;
        //            services.AddInstance<IRegion>(region);
        //            // At this point the region should have service provider set
        //            // We need to attach the service provider back onto the elemnt so
        //            // we can access the iregion/navservice for this element etc at a later stage
        //            loadedElement.SetServiceProvider(services);
        //        }

        //        // At this point the region is established with both parent set and
        //        // has a service provider. Can proceed with creating the nav service

        //        navService = region.NavigationFactory().CreateService(isComposite ? null : loadedElement);
        //        services.AddInstance<INavigationService>(navService);

        //        loadedElement.Unloaded += (sUnloaded, eUnloaded) =>
        //            {
        //                if (parent is not null)
        //                {
        //                    parent.Detach(region);
        //                }
        //            };
        //    }
        //};

        //var parent = new PlaceholderRegionNavigationService();
        //var navRegion = element.RegionNavigationServiceForControl(false) ?? NavigationServiceFactory.CreateService(parent, element, isComposite);

        //element.Loaded += async (sLoaded, eLoaded) =>
        //{
        //    var loadedparent = element.Parent.RegionNavigationServiceForControl(true) ?? Ioc.Default.GetService<IRegionNavigationService>();
        //    parent.NavigationService = loadedparent;

        //    var loadedElement = sLoaded as FrameworkElement;

        //    loadedElement.Unloaded += (sUnloaded, eUnloaded) =>
        //    {
        //        if (navRegion != null)
        //        {
        //            parent.Detach(navRegion);
        //        }
        //    };

        //    parent.Attach(navRegion, regionName);
        //};
    }

    public static TElement AsNavigationContainer<TElement>(this TElement element, IServiceProvider services)
        where TElement : FrameworkElement
    {
        // Create the Root region
        var rootRegion = new NavigationRegion(String.Empty, null, services);
        services.AddInstance<INavigationService>(new InnerNavigationService(services.GetInstance<INavigationService>()));

        // Create the element region
        var elementRegion = new NavigationRegion(String.Empty, element, rootRegion);
        element.SetRegion(elementRegion);

        return element;
    }

    public static void SetRegion(this DependencyObject element, IRegion value)
    {
        element.SetValue(RegionProperty, value);
    }

    public static IRegion GetRegion(this DependencyObject element)
    {
        return (IRegion)element.GetValue(RegionProperty);
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
}
