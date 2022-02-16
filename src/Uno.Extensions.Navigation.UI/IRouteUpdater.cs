namespace Uno.Extensions.Navigation;

internal interface IRouteUpdater
{
	void StartNavigation(IRegion region);

	void EndNavigation(IRegion region);
}

