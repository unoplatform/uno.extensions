using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface INavigator
{
    Route Route { get; }

    Task<NavigationResponse> NavigateAsync(NavigationRequest request);
}
