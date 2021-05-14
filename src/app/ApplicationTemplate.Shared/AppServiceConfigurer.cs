using System;
using System.Collections.Generic;
using System.Text;
using ApplicationTemplate.Hosting;
using ApplicationTemplate.Routing;
using ApplicationTemplate.Views;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationTemplate
{
    public class AppServiceConfigurer : IServiceConfigurer
    {
        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton<IMessenger, WeakReferenceMessenger>()
                .AddSingleton<IRouteMessenger, RouteMessenger>()
                .AddSingleton<IRouter>(s =>
                new Router(
                    App.Instance.NavigationFrame,
                    s.GetRequiredService<IMessenger>(),
                    //s.GetRequiredService < IDispatcherScheduler>(),
                    RouterConfiguration.GetPageRegistrations(),
                    RouterConfiguration.GetRoutes()
                ));
        }
    }
}
