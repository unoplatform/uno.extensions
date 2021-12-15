namespace Uno.Extensions.Navigation;

public interface IRouteRegistry
{
	IEnumerable<RouteMap> Routes { get; }

	IRouteRegistry Register(RouteMap route);
}
