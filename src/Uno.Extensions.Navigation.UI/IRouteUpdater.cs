namespace Uno.Extensions.Navigation;

internal interface IRouteUpdater
{
	void StartNavigation(INavigator navigator, IRegion region, NavigationRequest request);

	void EndNavigation(INavigator navigator, IRegion region, NavigationRequest request, NavigationResponse? response);
}

