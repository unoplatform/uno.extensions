using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface INavigator
{
    // TODO: Discuss alternatives for how to prevent default routes being executed if navigation in progress (eg deeplink)
    Task WaitForPendingNavigation();

    Task<NavigationResponse> NavigateAsync(NavigationRequest request);
}
