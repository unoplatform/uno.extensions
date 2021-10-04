using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface INavigationService
{
    Task<NavigationResponse> NavigateAsync(NavigationRequest request);
}
