using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if WINDOWS_UWP
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif
using Uno.Extensions.Navigation.Messages;

namespace Uno.Extensions.Navigation
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseRoutingWithRedirection<TRouteDefinitions, TLaunchMessage, TRouteRedirection>(
            this IHostBuilder builder,
            Func<Frame> navigationFrameLocator)
                where TRouteDefinitions : class, IRouteDefinitions, new()
                where TLaunchMessage : RoutingMessage, new()
                where TRouteRedirection : class, IRouteRedirection
        {
            return builder
                .UseRouting<TRouteDefinitions, TLaunchMessage>(navigationFrameLocator)
                .ConfigureServices(sp =>
                {
                    _ = sp.AddSingleton<IRouteRedirection, TRouteRedirection>();
                });
        }

        public static IHostBuilder UseRouting<TRouteDefinitions, TLaunchMessage>(
            this IHostBuilder builder,
            Func<Frame> navigationFrameLocator)
                where TRouteDefinitions : class, IRouteDefinitions, new()
                where TLaunchMessage : RoutingMessage, new()
        {
            return builder
                .ConfigureServices(sp =>
             {
                 _ = sp.AddRouting<TRouteDefinitions, TLaunchMessage>(navigationFrameLocator);
             });
        }
    }
}
