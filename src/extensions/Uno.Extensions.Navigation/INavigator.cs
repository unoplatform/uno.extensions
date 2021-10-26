using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface INavigator
{
    Route CurrentRoute { get; }
    Task<NavigationResponse> NavigateAsync(NavigationRequest request);
}
