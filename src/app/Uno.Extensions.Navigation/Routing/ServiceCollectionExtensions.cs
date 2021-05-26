using System;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
using Windows.UI.Xaml.Controls;
//-:cnd:noEmit
#else
//+:cnd:noEmit
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

namespace Uno.Extensions.Navigation
{
    public static class ServiceCollectionExtensions
    {
        public static IHostBuilder UseRouting<TRouteDefinitions, TLaunchMessage>(
            this IHostBuilder builder, Func<Frame> navigationFrameLocator)
            where TRouteDefinitions : class, IRouteDefinitions, new()
            where TLaunchMessage : RoutingMessage, new()
        {
            return builder
                .ConfigureServices(sp =>
             {
                 sp.AddRouting<TRouteDefinitions, TLaunchMessage>(navigationFrameLocator);
             });
        }

        public static IServiceCollection AddRouting<TRouteDefinitions, TLaunchMessage>(
            this IServiceCollection services, Func<Frame> navigationFrameLocator)
            where TRouteDefinitions : class, IRouteDefinitions, new()
            where TLaunchMessage : RoutingMessage, new()
        {
            var def = new TRouteDefinitions();
            var routes = def.Routes;

            // Register each of the view model types
            foreach (var r in routes)
            {
                services.AddTransient(r.Value.Item2);
            }

            return services
                .AddSingleton<INavigator>(s => new Navigator(navigationFrameLocator()))
                .AddSingleton<IMessenger, WeakReferenceMessenger>()
                .AddSingleton<IRouteMessenger, RouteMessenger>()
                .AddSingleton<IRouteDefinitions>(def)
                .AddSingleton<IRouter, Router>()
                .AddHostedService<NavigationService<TLaunchMessage>>();
        }
    }
}

