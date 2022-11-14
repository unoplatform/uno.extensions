namespace Uno.Extensions.Navigation;

/// <summary>
/// Implementation of navigation for a specific region type
/// </summary>
public interface INavigator
{
	/// <summary>
	/// Gets the current route of the navigator
	/// </summary>
	Route? Route { get; }

	/// <summary>
	/// Determines whether the navigator can navigate to the specified route
	/// </summary>
	/// <param name="route">The route to test whether navigation is possible</param>
	/// <returns>Awaitable value indicating whether navigation is possible</returns>
	Task<bool> CanNavigate(Route route);

	/// <summary>
	/// Navigates to a specific request
	/// </summary>
	/// <param name="request">The request to navigate to</param>
	/// <returns>The navigation response (awaitable)</returns>
	Task<NavigationResponse?> NavigateAsync(NavigationRequest request);
}

