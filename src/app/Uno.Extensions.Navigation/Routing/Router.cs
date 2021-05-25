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
    public class Router : IRouter, IRecipient<BaseRoutingMessage>
    {
        public INavigator Navigator { get; }
        public IReadOnlyDictionary<string, (Type, Action<IServiceCollection>, Func<IServiceProvider, object>)> Routes { get; }
        public IReadOnlyDictionary<string, Func<IServiceProvider, string[], string, string>> Redirections { get; }
        public IServiceProvider Services { get; }
        public Stack<string> NavigationStack { get; } = new Stack<string>();
        public Router(
            INavigator navigator,
            IMessenger messenger,
            IRouteDefinitions routeDefinitions,
            IServiceProvider services
            )
        {
            Navigator = navigator;
            Routes = routeDefinitions.Routes;
            Redirections = routeDefinitions.Redirections;
            Services = services;
            messenger.RegisterAll(this);
        }

        public void Receive(BaseRoutingMessage message)
        {
            var fullPath = (message.path + "");
            var routeSegments = fullPath.Split('/');

            var path = routeSegments.Last();
            if (Redirections.TryGetValue(path, out var redirection))
            {
                path = redirection(Services, NavigationStack.ToArray(), path);
            }

            if(path=="..")
            {
                Navigator.GoBack();
            }
            else if (Routes.TryGetValue(path, out var route))
            {
                var vm = route.Item3?.Invoke(Services);
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
