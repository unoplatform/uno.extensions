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
    public class RouteMessenger : IRouteMessenger
    {
        public IMessenger Messenger { get; }
        public RouteMessenger(IMessenger messenger)
        {
            Messenger = messenger;
        }
    
        public void Send<TMessage>(TMessage message) where TMessage : RoutingMessage
        {
            Messenger.Send< RoutingMessage>(message);
        }
    }
}
