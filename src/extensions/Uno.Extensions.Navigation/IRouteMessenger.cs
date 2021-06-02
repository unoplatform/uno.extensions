using Uno.Extensions.Navigation.Messages;

namespace Uno.Extensions.Navigation
{
    public interface IRouteMessenger
    {
        void Send<TMessage>(TMessage message)
            where TMessage : RoutingMessage;
    }
}
