using System;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
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
}

