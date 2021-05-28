using CommunityToolkit.Mvvm.Messaging;

namespace Uno.Extensions.Navigation
{
    public class RouteMessenger : IRouteMessenger
    {
        public RouteMessenger(IMessenger messenger)
        {
            Messenger = messenger;
        }

        public IMessenger Messenger { get; }

        public void Send<TMessage>(TMessage message)
            where TMessage : RoutingMessage
        {
            Messenger.Send<RoutingMessage>(message);
        }
    }
}
