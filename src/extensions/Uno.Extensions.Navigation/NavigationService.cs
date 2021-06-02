using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Messages;

namespace Uno.Extensions.Navigation
{
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter - exception for records
    public record NavigationService<TMessage>(ILogger<NavigationService<TMessage>> Logger, IRouter Router, IMessenger Messenger) : IHostedService // IRouter required so it get instantiated before messages are sent
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
        where TMessage : RoutingMessage, new()
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Starting navigation service");
            _ = Messenger.Send<RoutingMessage>(new TMessage());
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
