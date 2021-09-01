using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Adapters;
using Uno.Extensions.Navigation.Controls;
using System.Collections.Generic;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
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
                    .AddSingleton<INavigationMapping, NavigationMapping>()
                    .AddTransient<IFrameWrapper, FrameWrapper>()
                    .AddTransient<ITabWrapper, TabWrapper>()
                    .AddTransient<INavigationAdapter<Frame>, FrameNavigationAdapter>()
                    .AddTransient<INavigationAdapter<TabView>, TabNavigationAdapter>()
                    .AddSingleton<INavigationManager, NavigationService>()
                    .AddSingleton<INavigationService>(services => services.GetService<INavigationManager>())
                    .AddScoped<ViewModelDataProvider>()
                    .AddScoped<IDictionary<string, object>>(services => services.GetService<ViewModelDataProvider>().Parameters);
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

    public TData GetData<TData>() where TData : class
    {
        return Parameters.TryGetValue(string.Empty, out var data) ? data as TData : default;
    }
}
