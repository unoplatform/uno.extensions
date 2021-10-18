using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.ViewModels;
using Windows.UI.Popups;
#if !WINDOWS_UWP && !WINUI
using Popup = Windows.UI.Xaml.Controls.Popup;
#endif
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using System;
using Uno.Extensions.Navigation.Services;
using Uno.Extensions.Navigation.Regions;
#else
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

                    .AddScoped<CompositeNavigationService>()

                    // Register the region for each control type
                    .AddRegion<Frame, FrameNavigationService>()
                    .AddRegion<TabView, TabNavigationService>()
                    .AddRegion<ContentControl, ContentControlNavigationService>()
                   .AddRegion<Grid, GridVisiblityNavigationService>()
                   .AddRegion<Page, PageVisualStateNavigationService>()
                   .AddRegion<Microsoft.UI.Xaml.Controls.NavigationView, NavigationViewNavigationService>()
                    .AddRegion<ContentDialog, ContentDialogNavigationService>()
                    .AddRegion<MessageDialog, MessageDialogNavigationService>()
#if __IOS__
                    .AddRegion<Picker, PickerRegion>()
#endif
                    .AddRegion<Popup, PopupNavigationService>()

                    // Register the navigation mappings repository
                    .AddSingleton<IRouteMappings, RouteMappingsDefault>()

                    // Register the navigation manager and the providers for
                    // navigation data and the navigation service
                    //.AddSingleton<NavigationServiceFactory>()
                    .AddScoped<IRegionNavigationServiceFactory, NavigationServiceFactory>() // services => services.GetService<NavigationServiceFactory>())

                    //.AddScopedInstance<IRegionNavigationService>(services => services.GetService<IRegionNavigationServiceFactory>().CreateService(null, null, false))
                    //.AddSingleton<IRegionNavigationService>(services => services.GetService<IRegionNavigationServiceFactory>().CreateService(null, null, false))

                    //.AddScopedInstance<IScopedServiceProvider>()
                    .AddScopedInstance<IRegion>()

                    .AddScoped<ViewModelDataProvider>()
                    .AddScoped<RegionControlProvider>()
                    .AddScoped<IDictionary<string, object>>(services => services.GetService<ViewModelDataProvider>().Parameters)

                    .AddScopedInstance<INavigationService>();
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

    public static IServiceCollection AddRegion<TControl, TRegion>(this IServiceCollection services)
        where TRegion : class, INavigationService
    {
        if (services is null)
        {
            return services;
        }

        return services
                    .AddScoped<TRegion>()
                    .AddSingleton<IControlNavigationServiceFactory, ControlNavigationServiceFactory<TControl, TRegion>>();
    }

    public static IServiceCollection AddViewModelData<TData>(this IServiceCollection services)
        where TData : class
    {
        if (services is null)
        {
            return services;
        }

        return services
                    .AddTransient<TData>(services => services.GetService<ViewModelDataProvider>().GetData<TData>());
    }
}

public class ViewModelDataProvider
{
    public IDictionary<string, object> Parameters { get; set; }

    public TData GetData<TData>()
        where TData : class
    {
        return Parameters.TryGetValue(string.Empty, out var data) ? data as TData : default;
    }
}

public class RegionControlProvider
{
    public object RegionControl { get; set; }
}
