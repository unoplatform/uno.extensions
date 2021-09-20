using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Controls.Managers;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.Dialogs.Managers;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Regions.Managers;
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
                    .AddSingleton<INavigationMapping, NavigationMapping>()

                    // Register the navigation manager and the providers for
                    // navigation data and the navigation service
                    .AddSingleton<INavigationManager, NavigationManager>()
                    .AddScoped<NavigationServiceProvider>()
                    .AddScoped<INavigationService>(services => services.GetService<NavigationServiceProvider>().Navigation)
                    .AddScoped<ViewModelDataProvider>()
                    .AddScoped<RegionControlProvider>()
                    .AddScoped<IDictionary<string, object>>(services => services.GetService<ViewModelDataProvider>().Parameters);
    }

    public static IServiceCollection AddRegion<TControl, TControlManager, TRegionManager>(this IServiceCollection services)
        where TControl : class
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

public class NavigationServiceProvider
{
    public INavigationService Navigation { get; set; }

    public NavigationServiceProvider(INavigationManager manager)
    {
        // Set the default Navigation Service - expect this to be
        // overriden for scoped contexts
        Navigation = manager;
    }
}

public class RegionControlProvider
{
    public object RegionControl { get; set; }
}
