using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.Regions;

public interface IRegion
{
    Task<NavigationResponse> NavigateAsync(NavigationRequest context);
}
