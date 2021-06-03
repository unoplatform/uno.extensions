using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Messages;

namespace Uno.Extensions.Navigation
{
    public class Router : IRouter, IRecipient<RoutingMessage>
    {
        public Router(
            ILogger<Router> logger,
            INavigator navigator,
            IMessenger messenger,
            IRouteDefinitions routeDefinitions,
            IServiceProvider services,
            IRouteRedirection redirection = default
            )
        {
            Logger = logger;
            Navigator = navigator;
            Routes = routeDefinitions?.Routes;
            Redirection = redirection?.Redirection;
            Services = services;
            messenger.RegisterAll(this);
        }

        public ILogger Logger { get; }

        public INavigator Navigator { get; }

        public IReadOnlyDictionary<string, (Type, Type)> Routes { get; }

        public Func<string[], string, IDictionary<string, object>, string> Redirection { get; }

        public IServiceProvider Services { get; }

        public Stack<string> NavigationStack { get; } = new Stack<string>();

        public Stack<object> NavigationViewModelInstances { get; } = new Stack<object>();

        public void Receive(RoutingMessage message)
        {
            if (message == null)
            {
                Logger.LazyLogDebug(() => $"Received null message - exiting handler");
                return;
            }

            Logger.LazyLogDebug(() => $"Received message of type {message.GetType().Name} - {message}");
            var fullPath = Redirection?.Invoke(NavigationStack.ToArray(), message?.Path, message?.Args) ?? message?.Path + string.Empty;
            Logger.LazyLogDebug(() => $"Full path to navigate to {fullPath}");
#pragma warning disable CA1304 // Specify CultureInfo - no culture required
            var routeSegments = fullPath.Split('/').Select(x => (x + string.Empty).ToLower()).ToArray();
#pragma warning restore CA1304 // Specify CultureInfo

            var path = routeSegments.Last();
            Logger.LazyLogDebug(() => $"Last route segment {path}");

            if (path == "..")
            {
                Logger.LazyLogDebug(() => $"Pop from path and viewmodel stack");
                NavigationStack.Pop();
                NavigationViewModelInstances.Pop();
                Logger.LazyLogDebug(() => $"Navigate to previous page");
                Navigator.GoBack(NavigationViewModelInstances.Peek());
            }
            else if (Routes.TryGetValue(path, out var route))
            {
                Logger.LazyLogDebug(() => $"Create the ViewModel of type {route.Item2.Name}");
                var vm = Services.GetService(route.Item2);
                var pageType = route.Item1;
                Navigator.Navigate(pageType, vm);
                NavigationStack.Push(path);
                NavigationViewModelInstances.Push(vm);

#pragma warning disable CA1307, CA1310 // Specify StringComparison - ignore culture
                if (fullPath.StartsWith("/"))
#pragma warning restore CA1307, CA1310 // Specify StringComparison
                {
                    Navigator.Clear();
                }
            }
        }
    }
}
