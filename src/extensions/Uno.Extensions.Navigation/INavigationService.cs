namespace Uno.Extensions.Navigation;

public interface INavigationService
{
    IRegionService Region { get; set; }

    NavigationResponse NavigateAsync(NavigationRequest request);
}
