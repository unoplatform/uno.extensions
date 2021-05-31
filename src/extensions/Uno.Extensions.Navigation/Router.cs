using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;

namespace Uno.Extensions.Navigation
{
    public class Router : IRouter, IRecipient<RoutingMessage>
    {
        public Router(
            INavigator navigator,
            IMessenger messenger,
            IRouteDefinitions routeDefinitions,
            IServiceProvider services,
            IRouteRedirection redirection = default
            )
        {
            Navigator = navigator;
            Routes = routeDefinitions?.Routes;
            Redirection = redirection?.Redirection;
            Services = services;
            messenger.RegisterAll(this);
        }

        public INavigator Navigator { get; }

        public IReadOnlyDictionary<string, (Type, Type)> Routes { get; }

        public Func<string[], string, IDictionary<string, object>, string> Redirection { get; }

        public IServiceProvider Services { get; }

        public Stack<string> NavigationStack { get; } = new Stack<string>();

        public Stack<object> NavigationViewModelInstances { get; } = new Stack<object>();

        public void Receive(RoutingMessage message)
        {
            var fullPath = Redirection?.Invoke(NavigationStack.ToArray(), message?.Path, message?.Args) ?? message?.Path + string.Empty;
#pragma warning disable CA1304 // Specify CultureInfo - no culture required
            var routeSegments = fullPath.Split('/').Select(x => (x + string.Empty).ToLower()).ToArray();
#pragma warning restore CA1304 // Specify CultureInfo

            var path = routeSegments.Last();

            if (path == "..")
            {
                NavigationStack.Pop();
                NavigationViewModelInstances.Pop();
                Navigator.GoBack(NavigationViewModelInstances.Peek());
            }
            else if (Routes.TryGetValue(path, out var route))
            {
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
