﻿using System.Collections.Generic;
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
                    .AddSingleton<INavigationMapping, NavigationMapping>()
                    .AddTransient<IFrameWrapper, FrameWrapper>()
                    .AddTransient<ITabWrapper, TabWrapper>()
                    .AddTransient<IContentWrapper, ContentWrapper>()
                    .AddTransient<FrameNavigationAdapter>()
                    .AddTransient<TabNavigationAdapter>()
                    .AddTransient<ContentNavigationAdapter>()
                    .AddSingleton<IAdapterFactory, AdapterFactory<Frame, FrameNavigationAdapter>>()
                    .AddSingleton<IAdapterFactory, AdapterFactory<TabView, TabNavigationAdapter>>()
                    .AddSingleton<IAdapterFactory, AdapterFactory<ContentControl, ContentNavigationAdapter>>()
                    .AddSingleton<IDialogManager, NavigationContentDialog>()
                    .AddSingleton<IDialogManager, NavigationMessageDialog>()
                    .AddSingleton<IDialogProvider, DialogProvider>()
                    .AddSingleton<INavigationManager, NavigationManager>()
                    .AddScoped<NavigationServiceProvider>()
                    .AddScoped<INavigationService>(services => services.GetService<NavigationServiceProvider>().Navigation)
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
