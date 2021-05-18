using System;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
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

namespace ApplicationTemplate.Navigation
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddRouting<TRouteDefinitions, TLaunchMessage>
            (this IServiceCollection services, Func<Frame> navigationFrameLocator)
            where TRouteDefinitions: class, IRouteDefinitions
            where TLaunchMessage : BaseRoutingMessage, new()

        {
            return services
                .AddSingleton<INavigator>(s => new Navigator(navigationFrameLocator()))
                .AddSingleton<IMessenger, WeakReferenceMessenger>()
                .AddSingleton<IRouteMessenger, RouteMessenger>()
                .AddSingleton<IRouteDefinitions, TRouteDefinitions>()
                .AddSingleton<IRouter, Router>()
                .AddHostedService<NavigationService<TLaunchMessage>>();
        }
    }
}

