using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Adapters;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Dialogs;
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
                    // Register the control navigation implementation (simple or stack based navigation)
                    .AddTransient<IStackViewManager<Frame>, FrameStackNavigation>()
                    .AddTransient<IViewManager<TabView>, TabSimpleNavigation>()
                    .AddTransient<IViewManager<ContentControl>, ContentControlSimpleNavigation>()

                    // Register the adapter for each control type
                    .AddAdapter<Frame, StackNavigationAdapter<Frame>>()
                    .AddAdapter<TabView, SimpleNavigationAdapter<TabView>>()
                    .AddAdapter<ContentControl, SimpleNavigationAdapter<ContentControl>>()

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
                    .AddScoped<IDictionary<string, object>>(services => services.GetService<ViewModelDataProvider>().Parameters);
    }

    private static IServiceCollection AddAdapter<TControl, TAdapter>(this IServiceCollection services)
        where TAdapter : class, INavigationAdapter
    {
        if (services is null)
        {
            return services;
        }

        return services
                    .AddTransient<TAdapter>()
                    .AddSingleton<IAdapterFactory, AdapterFactory<TControl, TAdapter>>();
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
