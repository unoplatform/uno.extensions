using Uno.Extensions.Navigation.Messages;

namespace Uno.Extensions.Navigation
{
    public interface IRouter
    {
        void Receive(RoutingMessage message);
    }
}
