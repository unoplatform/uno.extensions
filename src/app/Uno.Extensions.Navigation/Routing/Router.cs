using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
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

namespace Uno.Extensions.Navigation
{
    public class Router : IRouter, IRecipient<RoutingMessage>
    {
        public INavigator Navigator { get; }
        public IReadOnlyDictionary<string, (Type, Type)> Routes { get; }
        public Func<string[], string,IDictionary<string,object>, string> Redirection { get; }
        public IServiceProvider Services { get; }
        public Stack<string> NavigationStack { get; } = new Stack<string>();
        public Router(
            INavigator navigator,
            IMessenger messenger,
            IRouteDefinitions routeDefinitions,
            IServiceProvider services,
            IRouteRedirection redirection=default
            )
        {
            Navigator = navigator;
            Routes = routeDefinitions.Routes;
            Redirection = redirection?.Redirection;
            Services = services;
            messenger.RegisterAll(this);
        }

        public void Receive(RoutingMessage message)
        {
            var fullPath = (Redirection?.Invoke(NavigationStack.ToArray(), message.path, message.args)?? message.path + "");
            var routeSegments = fullPath.Split('/').Select(x => (x+"").ToLower()).ToArray();

            var path = routeSegments.Last();
            if(path=="..")
            {
                Navigator.GoBack();
            }
            else if (Routes.TryGetValue(path, out var route))
            {
                var vm = Services.GetService(route.Item2);
                Type pageType = route.Item1;
                Navigator.Navigate(pageType, vm);

                if(fullPath.StartsWith("/"))
                {
                    Navigator.Clear();
                }
            }
        }
    }
}
