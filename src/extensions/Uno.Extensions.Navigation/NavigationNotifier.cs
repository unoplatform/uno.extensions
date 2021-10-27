using CommunityToolkit.Mvvm.Messaging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class NavigationNotifier : INavigationNotifier
{
    private IMessenger Messenger { get; }
    public NavigationNotifier(IMessenger messenger)
    {
        Messenger = messenger;
    }

    public void Register<TMessageHandler>(TMessageHandler handler)
        where TMessageHandler : IRecipient<RegionUpdatedMessage>
    {
        Messenger.Register(handler);
    }

    public void Update(IRegion region)
    {
        Messenger.Send(new RegionUpdatedMessage(region));
    }
}
