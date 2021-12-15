namespace Uno.Extensions.Navigation;

public class RouteRegistry : IRouteRegistry
{
	private IServiceCollection Services { get; }

	private IList<RouteMap> _routes = new List<RouteMap>();

	public IEnumerable<RouteMap> Routes => _routes;

	public RouteRegistry(IServiceCollection services)
	{
		Services = services;
	}

	public IRouteRegistry Register(RouteMap route)
	{
		_routes.Add(route);

		route.Flatten().ForEach(r => r.RegisterTypes(Services));
		return this;
	}
}
