using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation;

public class RouteBuilder : IRouteRegistry
{
	private IServiceCollection Services { get; }

	private IList<RouteMap> _routes = new List<RouteMap>();

	public IEnumerable<RouteMap> Routes => _routes;

	public RouteBuilder(IServiceCollection services)
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
