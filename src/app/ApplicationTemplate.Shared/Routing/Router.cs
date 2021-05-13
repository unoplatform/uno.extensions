using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging;
using FluentValidation.Validators;
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

namespace ApplicationTemplate.Routing
{
    public interface IRouter
    {

    }

    public class Router : IRouter, IRecipient<BaseRoutingMessage>
    {
        public Frame NavigationFrame { get; }
        public IReadOnlyDictionary<Type, IRoute> Routes { get; }
        public IReadOnlyDictionary<Type, Type> Registrations { get; }
        //public IDispatcherScheduler Scheduler { get; }
        public Router(
            Frame navFrame,
            IMessenger messenger,
            //IDispatcherScheduler scheduler,
            IReadOnlyDictionary<Type, Type> viewModelPageRegistrations,
            IReadOnlyDictionary<Type, IRoute> routes)
        {
            NavigationFrame = navFrame;
            Routes = routes;
            Registrations = viewModelPageRegistrations;
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

//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
                NavigationFrame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
//-:cnd:noEmit
#else
//+:cnd:noEmit
                NavigationFrame.DispatcherQueue.TryEnqueue(
//-:cnd:noEmit
#endif
//+:cnd:noEmit
                () =>{
                    switch (message)
                    {
                        case ShowMessage show:
                            var navResult = NavigationFrame.Navigate(pageType);
                            if (navResult)
                            {
                                (NavigationFrame.Content as Page).DataContext = vm;
                            }
                            break;
                    }
                });
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

    public record ShowMessage(object? Sender = null) : BaseRoutingMessage(Sender) { };


    public record ShowItemMessage<TItem>(TItem ItemToShow, object? Sender = null) : BaseRoutingMessage(Sender) { };

    public record CloseMessage(bool? canBeCancelled=true, object? Sender = null) : BaseRoutingMessage(Sender) { };

    public record SelectedItemMessage<TItem>(TItem ItemSelected, object? Sender = null) : BaseRoutingMessage(Sender) { };

    public record SelectItemMessage<TItem>(IEnumerable<TItem> ItemsToSelectFrom, object? Sender = null) : BaseRoutingMessage(Sender) { };
}


namespace System.Runtime.CompilerServices
{
    public class IsExternalInit
    { }

}

