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

namespace Uno.Extensions.Navigation
{
    public class Router : IRouter, IRecipient<BaseRoutingMessage>
    {
        public INavigator Navigator { get; }
        public IReadOnlyDictionary<Type, IRoute> Routes { get; }
        public IReadOnlyDictionary<Type, Type> Registrations { get; }
        public Router(
            INavigator navigator,
            IMessenger messenger,
            IRouteDefinitions routeDefinitions
            )
        {
            Navigator = navigator;
            Routes = routeDefinitions.Routes;
            Registrations = routeDefinitions.ViewModelMappings;
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
                        Navigator.Navigate(pageType, vm);
                        break;
                }
            }
        }
    }
}
