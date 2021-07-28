using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public Stack<string> NavigationStack { get; protected set; } = new Stack<string>();

        public Stack<object> NavigationViewModelInstances { get; protected set; } = new Stack<object>();

        public async void Receive(RoutingMessage message)
        {
            if (message == null)
            {
                Logger.LazyLogDebug(() => $"Received null message - exiting handler");
                return;
            }

            message = await Preprocess(message);

            Logger.LazyLogDebug(() => $"Received message of type {message.GetType().Name} - {message}");
            var fullPath = Redirection?.Invoke(NavigationStack.ToArray(), message.Path, message.Args) ?? message?.Path + string.Empty;
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
                var oldVM = NavigationViewModelInstances.Pop();
                await ((oldVM as ILifecycleStop)?.Stop(true) ?? Task.CompletedTask);

                Logger.LazyLogDebug(() => $"Navigate to previous page");
                var vm = NavigationViewModelInstances.Peek();
                Navigator.GoBack(vm);
                await ((vm as ILifecycleStart)?.Start(false) ?? Task.CompletedTask);
            }
            else if (Routes.TryGetValue(path, out var route))
            {
                if (NavigationViewModelInstances.Count > 0)
                {
                    var oldVM = NavigationViewModelInstances.Peek();
                    await ((oldVM as ILifecycleStop)?.Stop(false) ?? Task.CompletedTask);
                }

                Logger.LazyLogDebug(() => $"Create the ViewModel of type {route.Item2.Name}");
                var vm = Services.GetService(route.Item2);
                await ((vm as IInitialise)?.Initialize(message.Args) ?? Task.CompletedTask);

                var pageType = route.Item1;
                Navigator.Navigate(pageType, vm);
                NavigationStack.Push(path);
                NavigationViewModelInstances.Push(vm);

                await ((vm as ILifecycleStart)?.Start(true) ?? Task.CompletedTask);

#pragma warning disable CA1307, CA1310 // Specify StringComparison - ignore culture
                if (fullPath.StartsWith("/"))
#pragma warning restore CA1307, CA1310 // Specify StringComparison
                {
                    Navigator.Clear();
                }
            }
        }

        protected virtual Task<RoutingMessage> Preprocess(RoutingMessage message)
        {
            return Task.FromResult(message);
        }
    }

    public interface IInitialise
    {
        Task Initialize(IDictionary<string, object> args);
    }

    public interface ILifecycleStart
    {
        Task Start(bool create);
    }

    public interface ILifecycleStop
    {
        Task Stop(bool cleanup);
    }
}
