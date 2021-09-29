using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.Dialogs.Managers;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Regions.Managers;
using Uno.Extensions.Navigation.ViewModels;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
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
                    // Register the region for each control type
                    .AddScoped<CompositeRegionManager>()
                    .AddRegion<Frame, FrameRegionManager>()
                    .AddRegion<TabView, TabRegionManager>()
                    .AddRegion<ContentControl, ContentControlRegionManager>()
                   .AddRegion<Grid, GridVisiblityRegionManager>()
                   .AddRegion<Page, PageVisualStateRegionManager>()
                   .AddRegion<Microsoft.UI.Xaml.Controls.NavigationView, NavigationViewRegionManager>()

                    // Register the different types of dialogs
                    .AddSingleton<IDialogManager, ContentDialogManager>()
                    .AddSingleton<IDialogManager, MessageDialogManager>()
                    .AddSingleton<IDialogFactory, DialogFactory>()

                    // Register the navigation mappings repository
                    .AddSingleton<INavigationMappings, NavigationMappings>()

                    // Register the view model manager
                    .AddScoped<IViewModelManager, ViewModelManager>()

                    // Register the navigation manager and the providers for
                    // navigation data and the navigation service
                    .AddSingleton<INavigationManager, NavigationManager>()

                    .AddScoped<ScopedServiceHost<IRegionManager>>()
                    .AddScoped<IRegionManager>(services => services.GetService<ScopedServiceHost<IRegionManager>>().Service)

                    .AddScoped<ScopedServiceHost<IRegionService>>()
                    .AddScoped<IRegionService>(services => services.GetService<ScopedServiceHost<IRegionService>>().Service)

                    .AddScoped<ViewModelDataProvider>()
                    .AddScoped<RegionControlProvider>()
                    .AddScoped<IDictionary<string, object>>(services => services.GetService<ViewModelDataProvider>().Parameters)

                    .AddScoped<ScopedServiceHost<INavigationService>>()
                    .AddScoped<INavigationService>(services =>
                            services.GetService<ScopedServiceHost<INavigationService>>().Service ??
                            services.GetService<INavigationManager>().Root.Navigation
                            );

    }

    public static IServiceCollection AddRegion<TControl, TRegionManager>(this IServiceCollection services)
        where TRegionManager : class, IRegionManager
    {
        if (services is null)
        {
            return services;
        }

        return services
                    .AddScoped<TRegionManager>()
                    .AddSingleton<IRegionManagerFactory, RegionManagerFactory<TControl, TRegionManager>>();
    }

    public static IServiceCollection AddViewModelData<TData>(this IServiceCollection services)
        where TData : class
    {
        if (services is null)
        {
            return services;
        }

        return services
                    .AddScoped<TData>(services => services.GetService<ViewModelDataProvider>().GetData<TData>());
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

public class ScopedServiceHost<T>
{
    public T Service { get; set; }
}

public class RegionControlProvider
{
    public object RegionControl { get; set; }
}
