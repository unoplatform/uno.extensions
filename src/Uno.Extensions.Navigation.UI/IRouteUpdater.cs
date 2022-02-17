namespace Uno.Extensions.Navigation;

internal interface IRouteUpdater
{
	Guid StartNavigation(IRegion region);

	void EndNavigation(Guid regionUpdateId);
}

