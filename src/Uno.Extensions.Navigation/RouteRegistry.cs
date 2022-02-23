namespace Uno.Extensions.Navigation;

public class RouteRegistry : IRouteRegistry
{
	private IServiceCollection Services { get; }

	private readonly IList<RouteMap> _routes = new List<RouteMap>();

	public IEnumerable<RouteMap> Routes => _routes;

	private readonly IViewRegistry _views;

	public RouteRegistry(IServiceCollection services, IViewRegistry views)
	{
		_views = views;
		Services = services;
	}

	public IRouteRegistry Register(Func<IViewResolver, RouteMap> buildRouteMap)
	{
		var route = buildRouteMap(new ViewResolver(_views));
		_routes.Add(route);

		var flattened = route.Flatten();
		foreach (var r in flattened)
		{
			r.RegisterTypes(Services);
		}
		return this;
	}
}



public class ViewRegistry : IViewRegistry
{
	private IList<ViewMap> _views = new List<ViewMap>();
	public IEnumerable<ViewMap> Views => _views;

	public IViewRegistry Register(params ViewMap[] views)
	{
		foreach (var view in views)
		{
			_views.Add(view);
		}
		return this;
	}
}
