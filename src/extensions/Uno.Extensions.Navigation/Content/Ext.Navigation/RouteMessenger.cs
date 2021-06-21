using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Messages;

namespace Uno.Extensions.Navigation
{
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter - exception for records
    public record RouteMessenger(ILogger<RouteMessenger> Logger, IMessenger Messenger) : IRouteMessenger
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter - exception for records
    {
        public void Send<TMessage>(TMessage message)
            where TMessage : RoutingMessage
        {
            Logger.LazyLogDebug(() => $"Sending message to trigger navigation of {typeof(TMessage).Name}");
            Messenger.Send<RoutingMessage>(message);
        }
    }
}
