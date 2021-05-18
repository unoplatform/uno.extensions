using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
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
    public interface INavigator
    {
        void Navigate(Type destinationPage, object? viewModel=null);
        void GoBack();
    }

    public record Navigator : INavigator
    {
        public Frame NavigationFrame { get; }

        public Navigator(Frame navFrame)
        {
            NavigationFrame = navFrame;
        }

        public void Navigate(Type destinationPage, object? viewModel=null)
        {
//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
            NavigationFrame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
            //-:cnd:noEmit
#else
//+:cnd:noEmit
                NavigationFrame.DispatcherQueue.TryEnqueue(
//-:cnd:noEmit
#endif
            //+:cnd:noEmit
            () => {
                var navResults = NavigationFrame.Navigate(destinationPage);
                if (navResults && viewModel is not null)
                {
                    (NavigationFrame.Content as Page).DataContext = viewModel;
                }
            });
        }

        public void GoBack()
        {
            //-:cnd:noEmit
#if WINDOWS_UWP
            //+:cnd:noEmit
            NavigationFrame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
            //-:cnd:noEmit
#else
//+:cnd:noEmit
                NavigationFrame.DispatcherQueue.TryEnqueue(
//-:cnd:noEmit
#endif
            //+:cnd:noEmit
            () => {
                NavigationFrame.GoBack();
            });
        }
    }

    public interface IRouter
    {

    }

    public class Router : IRouter, IRecipient<BaseRoutingMessage>
    {
        public INavigator Navigator { get; }
        public IReadOnlyDictionary<Type, IRoute> Routes { get; }
        public IReadOnlyDictionary<Type, Type> Registrations { get; }
        //public IDispatcherScheduler Scheduler { get; }
        public Router(
            INavigator navigator,
            IMessenger messenger,
            IRouteDefinitions routeDefinitions
            //IDispatcherScheduler scheduler,
            //IReadOnlyDictionary<Type, Type> viewModelPageRegistrations,
            //IReadOnlyDictionary<Type, IRoute> routes
            )
        {
            Navigator = navigator;
            Routes = routeDefinitions.Routes;
            Registrations = routeDefinitions.ViewModelMappings ;
            //Scheduler = scheduler;
            messenger.RegisterAll(this);
        }

        public void Receive(BaseRoutingMessage message)
        {
            if (Routes.TryGetValue(message.GetType(), out var route))
            {
                var vm = route.BuildRoute(message);
                Type pageType = null;
                if (vm is not null)
                {
                    Registrations.TryGetValue(vm.GetType(), out pageType);
                }


                    switch (message)
                    {
                        case LaunchMessage show:
                            Navigator.Navigate(pageType,vm);
                            break;
                    }
            }
        }
    }

    public class RouteMessenger : IRouteMessenger
    {
        public IMessenger Messenger { get; }
        public RouteMessenger(IMessenger messenger)
        {
            Messenger = messenger;
        }
    
        public void Send<TMessage>(TMessage message) where TMessage : BaseRoutingMessage
        {
            Messenger.Send(message);
        }
    }


    public interface IRouteMessenger
    {
        void Send<TMessage>(TMessage message) where TMessage : BaseRoutingMessage;
    }


    public interface IRoute
    {
        object BuildRoute(object message);
    }

    public class Route<TMessage>:IRoute
    {
        public Func<TMessage, object> RouteBuilder { get; }
        public Route(Func<TMessage,object> routeBuilder)
        {
            RouteBuilder = routeBuilder;
        }

        public object BuildRoute(object message)
        {
            return RouteBuilder((TMessage)message);
        }
    }

    public abstract record BaseRoutingMessage(object? Sender = null) { };

    public record LaunchMessage() : BaseRoutingMessage() { };


    public record ShowMessage(object? Sender = null) : BaseRoutingMessage(Sender) { };


    public record ShowItemMessage<TItem>(TItem ItemToShow, object? Sender = null) : BaseRoutingMessage(Sender) { };

    public record CloseMessage(bool? canBeCancelled=true, object? Sender = null) : BaseRoutingMessage(Sender) { };

    public record SelectedItemMessage<TItem>(TItem ItemSelected, object? Sender = null) : BaseRoutingMessage(Sender) { };

    public record SelectItemMessage<TItem>(IEnumerable<TItem> ItemsToSelectFrom, object? Sender = null) : BaseRoutingMessage(Sender) { };


    public record NavigationService<TMessage>(IRouter router, IMessenger Messenger) : IHostedService
        where TMessage : BaseRoutingMessage, new()
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Messenger.Send<BaseRoutingMessage>(new TMessage());
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }

    public interface IRouteDefinitions
    {
        IReadOnlyDictionary<Type, IRoute> Routes { get;}

        IReadOnlyDictionary<Type, Type> ViewModelMappings { get; }
    }

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


namespace System.Runtime.CompilerServices
{
    public class IsExternalInit
    { }

}

