using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface INavigator
{
    Task<NavigationResponse> NavigateAsync(NavigationRequest request);
}
