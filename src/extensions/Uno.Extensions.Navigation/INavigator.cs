using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface INavigator
{
    Task WaitForPendingNavigation();

    Task<NavigationResponse> NavigateAsync(NavigationRequest request);
}
