using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Controls.Managers;
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
                    .AddRegion<Frame, FrameManager, StackRegionManager<FrameManager>>()
                    .AddRegion<TabView, TabManager, SimpleRegionManager<TabManager>>()
                    .AddRegion<ContentControl, ContentControlManager, SimpleRegionManager<ContentControlManager>>()

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
                    //.AddScoped<NavigationServiceProvider>()
                    //.AddScoped<INavigationService>(services => services.GetService<NavigationServiceProvider>().Navigation)

                    .AddScoped<ScopedServiceHost<IRegionManager>>()
                    .AddScoped<IRegionManager>(services => services.GetService<ScopedServiceHost<IRegionManager>>().Service)

                    .AddScoped<ScopedServiceHost<IRegionServiceContainer>>()
                    .AddScoped<IRegionServiceContainer>(services => services.GetService<ScopedServiceHost<IRegionServiceContainer>>().Service)


                    //.AddScoped<IRegionServiceContainer, RegionService>()

                    .AddScoped<ViewModelDataProvider>()
                    .AddScoped<RegionControlProvider>()
                    .AddScoped<IDictionary<string, object>>(services => services.GetService<ViewModelDataProvider>().Parameters)
                    .AddScoped<INavigationRegionContainer, NavigationRegionContainer>()

                    .AddScoped<ScopedServiceHost<INavigationRegionService>>()
                    .AddScoped<INavigationRegionService>(services =>
                            services.GetService<ScopedServiceHost<INavigationRegionService>>().Service ??
                            services.GetService<INavigationManager>().Root.Navigation
                            )

                    .AddScoped<INavigationService>(services => services.GetService<INavigationRegionService>());
    }

    public static IServiceCollection AddRegion<TControl, TControlManager, TRegionManager>(this IServiceCollection services)
        //where TControl : class
        where TControlManager : class, IViewManager
        where TRegionManager : class, IRegionManager
    {
        if (services is null)
        {
            return services;
        }

        return services
                    .AddScoped<TControlManager>()
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
