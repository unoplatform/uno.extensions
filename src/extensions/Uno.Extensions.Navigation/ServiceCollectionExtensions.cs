using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.ViewModels;
using Windows.UI.Popups;
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
                    .AddRegion<Frame, FrameRegion>()
                    .AddRegion<TabView, TabRegion>()
                    .AddRegion<ContentControl, ContentControlRegion>()
                   .AddRegion<Grid, GridVisiblityRegion>()
                   .AddRegion<Page, PageVisualStateRegion>()
                   .AddRegion<Microsoft.UI.Xaml.Controls.NavigationView, NavigationViewRegion>()
                    .AddRegion<ContentDialog, ContentDialogRegion>()
                    .AddRegion<MessageDialog, MessageDialogRegion>()

                    // Register the navigation mappings repository
                    .AddSingleton<IRouteMappings, RouteMappings>()

                    // Register the view model manager
                    .AddScoped<IViewModelManager, ViewModelManager>()

                    // Register the navigation manager and the providers for
                    // navigation data and the navigation service
                    .AddSingleton<IRegionNavigationServiceFactory, RegionNavigationServiceFactory>()
                    .AddSingleton<IDialogNavigationServiceFactory, DialogNavigationServiceFactory>()
                    .AddTransient<DialogNavigationService>()

                    .AddScoped<ScopedServiceHost<IRegion>>()
                    .AddScoped<IRegion>(services => services.GetService<ScopedServiceHost<IRegion>>().Service)

                    .AddSingleton<IRegionNavigationService>(services => services.GetService<IRegionNavigationServiceFactory>().CreateService(null, false))

                    .AddScoped<ScopedServiceHost<IRegionNavigationService>>()

                    .AddScoped<ViewModelDataProvider>()
                    .AddScoped<RegionControlProvider>()
                    .AddScoped<IDictionary<string, object>>(services => services.GetService<ViewModelDataProvider>().Parameters)

                    .AddScoped<ScopedServiceHost<INavigationService>>()
                    .AddScoped<INavigationService>(services =>
                            services.GetService<ScopedServiceHost<INavigationService>>().Service ??
                            (services.GetService<ScopedServiceHost<IRegionNavigationService>>().Service as INavigationService) ??
                            services.GetService<IRegionNavigationService>()
                            );
    }

    public static IServiceCollection AddRegion<TControl, TRegion>(this IServiceCollection services)
        where TRegion : class, IRegion
    {
        if (services is null)
        {
            return services;
        }

        return services
                    .AddScoped<TRegion>()
                    .AddSingleton<IRegionFactory, RegionFactory<TControl, TRegion>>();
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

public class ScopedServiceHost<T>
{
    public T Service { get; set; }
}

public class RegionControlProvider
{
    public object RegionControl { get; set; }
}
