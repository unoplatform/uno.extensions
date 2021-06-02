using System;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif
using Uno.Extensions.Navigation.Messages;

namespace Uno.Extensions.Navigation
{
    public static class ServiceCollectionExtensions
    {
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
