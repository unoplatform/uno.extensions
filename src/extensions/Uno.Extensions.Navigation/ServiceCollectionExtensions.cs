using System;
using System.Collections.Generic;
//using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Navigators;

using Windows.UI.Popups;
//#if !WINDOWS_UWP && !WINUI
//using Popup = Windows.UI.Xaml.Controls.Popup;
//#endif
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#else
using Windows.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
#endif

namespace Uno.Extensions.Navigation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNavigation(this IServiceCollection services)
    {
        if (services is null)
        {
            return services;
        }

        return services
                    .AddScoped<IInstanceRepository, InstanceRepository>()

                    //.AddSingleton<IMessenger, WeakReferenceMessenger>()
                    .AddSingleton<INavigationNotifier, NavigationNotifier>()
                    .AddScoped<Navigator>()

                    // Register the region for each control type
                    .AddRegion<Frame, FrameNavigator>()
                    .AddRegion<ContentControl, ContentControlNavigator>()
                   .AddRegion<Panel, PanelVisiblityNavigator>(PanelVisiblityNavigator.NavigatorName)
                   .AddRegion<Microsoft.UI.Xaml.Controls.NavigationView, NavigationViewNavigator>()
                    .AddRegion<ContentDialog, ContentDialogNavigator>()
                    .AddRegion<MessageDialog, MessageDialogNavigator>()
                    .AddRegion<Flyout, FlyoutNavigator>()
                    .AddRegion<Popup, PopupNavigator>()

                    .AddSingleton<INavigationBindingHandler, ButtonBaseNavigationBindingHandler>()
                    .AddSingleton<INavigationBindingHandler, SelectorNavigationBindingHandler>()

                    // Register the navigation mappings repository
                    .AddSingleton<IRouteMappings, RouteMappingsDefault>()

                    // Register the navigation manager and the providers for
                    // navigation data and the navigation service
                    //.AddSingleton<NavigationServiceFactory>()
                    .AddScoped<INavigatorFactory, NavigatorFactory>() // services => services.GetService<NavigationServiceFactory>())

                    //.AddScopedInstance<IRegionNavigationService>(services => services.GetService<IRegionNavigationServiceFactory>().CreateService(null, null, false))
                    //.AddSingleton<IRegionNavigationService>(services => services.GetService<IRegionNavigationServiceFactory>().CreateService(null, null, false))

                    //.AddScopedInstance<IScopedServiceProvider>()
                    .AddScopedInstance<IRegion>()

                    .AddScoped<NavigationDataProvider>()
                    .AddScoped<RegionControlProvider>()
                    .AddScoped<IDictionary<string, object>>(services => services.GetService<NavigationDataProvider>().Parameters)

                    .AddScopedInstance<INavigator>();
    }

    public static IServiceCollection ConfigureNavigatorFactory(this IServiceCollection services, Action<INavigatorFactory> register)
    {
        return services.AddSingleton(new NavigatorFactoryBuilder() { Configure = register });
    }

    public static IServiceCollection AddScopedInstance<T>(this IServiceCollection services, Func<IServiceProvider, T> defaultValue = null)
        where T : class
    {
        return services.AddTransient<T>(sp => sp.GetInstance<T>() ?? defaultValue?.Invoke(sp));
    }

    public static void AddInstance<T>(this IServiceProvider provider, Func<T> instanceCreator)
    {
        provider.AddInstance(typeof(T), instanceCreator);
    }

    public static void AddInstance<T>(this IServiceProvider provider, Type serviceType, Func<T> instanceCreator)
    {
        provider.GetService<IInstanceRepository>().Instances[serviceType] = instanceCreator;
    }

    public static T AddInstance<T>(this IServiceProvider provider, T instance)
    {
        provider.AddInstance(typeof(T), instance);
        return instance;
    }

    public static object AddInstance(this IServiceProvider provider, Type serviceType, object instance)
    {
        provider.GetService<IInstanceRepository>().Instances[serviceType] = instance;
        return instance;
    }

    public static T GetInstance<T>(this IServiceProvider provider)
    {
        var value = provider.GetInstance(typeof(T));
        if (value is Func<T> valueCreator)
        {
            var instance = valueCreator();
            provider.AddInstance(instance);
            return instance;
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }

    public static object GetInstance(this IServiceProvider provider, Type type)
    {
        return provider.GetService<IInstanceRepository>().Instances.TryGetValue(type, out var value) ? value : null;
    }

    public static IServiceCollection AddRegion<TControl, TRegion>(this IServiceCollection services, string name = null)
        where TRegion : class, INavigator
    {
        if (services is null)
        {
            return services;
        }

        return services
                    .AddScoped<TRegion>()
                    .ConfigureNavigatorFactory(factory => factory.RegisterNavigator<TRegion>(name ?? typeof(TControl).Name));
    }

    public static IServiceCollection AddViewModelData<TData>(this IServiceCollection services)
        where TData : class
    {
        if (services is null)
        {
            return services;
        }

        return services
                    .AddTransient<TData>(services => services.GetService<NavigationDataProvider>().GetData<TData>());
    }
}

public class NavigationDataProvider
{
    public IDictionary<string, object>? Parameters { get; set; }

    public TData? GetData<TData>()
        where TData : class
    {
        return (Parameters?.TryGetValue(string.Empty, out var data) ?? false) ? data as TData : default;
    }
}

public class RegionControlProvider
{
    public object? RegionControl { get; set; }
}
