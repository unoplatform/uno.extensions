namespace Uno.Extensions.Navigation;

internal interface IRouteUpdater
{
	Guid StartNavigation(IRegion region);

	Task EndNavigation(Guid regionUpdateId);
}

