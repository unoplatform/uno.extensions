namespace Uno.Extensions.Navigation;

public interface INavigator
{
    Route? Route { get; }

	bool CanNavigate(Route route);

	Task<NavigationResponse?> NavigateAsync(NavigationRequest request);
}
