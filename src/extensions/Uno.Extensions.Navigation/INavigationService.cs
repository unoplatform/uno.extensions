namespace Uno.Extensions.Navigation;

public interface INavigationService
{
    INavigationService Parent { get; set; }

    IRegionService Region { get; set; }

    NavigationResponse NavigateAsync(NavigationRequest request);
}
