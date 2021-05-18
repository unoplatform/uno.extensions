using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Hosting;
using ApplicationTemplate.Navigation;
using ApplicationTemplate.Views;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationTemplate
{
    //public class AppServiceConfigurer : IServiceConfigurer
    //{
    //    public IConfiguration Configuration { get; set; }

    //    public void ConfigureServices(IServiceCollection services)
    //    {
    //        services
    //            .AddSingleton<INavigator>(s=> new Navigator(App.Instance.NavigationFrame))
    //            .AddSingleton<IMessenger, WeakReferenceMessenger>()
    //            .AddSingleton<IRouteMessenger, RouteMessenger>()
    //            .AddSingleton<IRouteDefinitions, RouterConfiguration>()
    //            .AddSingleton<IRouter, Router>()
    //            .AddHostedService<NavigationService<LaunchMessage>>();
    //            //.AddSingleton<IHostedService,UnoAppService>();
    //    }
    //}

    //public record UnoAppService(IRouter router, IMessenger Messenger) : IHostedService
    //{
    //    public async Task StartAsync(CancellationToken cancellationToken)
    //    {
    //        Messenger.Send<BaseRoutingMessage>(new ShowMessage(this));
    //    }

    //    public async Task StopAsync(CancellationToken cancellationToken)
    //    {
    //    }
    //}
}
