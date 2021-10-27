using CommunityToolkit.Mvvm.Messaging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public interface INavigationNotifier
{
    void Update(IRegion region);

    void Register<TMessageHandler>(TMessageHandler handler)
        where TMessageHandler : IRecipient<RegionUpdatedMessage>;
}
