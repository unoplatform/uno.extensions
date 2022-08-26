namespace Uno.Extensions.Navigation;

public interface INavigator
{
    Route? Route { get; }

	Task<bool> CanNavigate(Route route);

	Task<NavigationResponse?> NavigateAsync(NavigationRequest request);
}

