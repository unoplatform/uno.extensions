namespace Uno.Extensions.Navigation;

public interface IRouteRegistry
{
	IEnumerable<RouteMap> Routes { get; }

	IRouteRegistry Register(Func<IViewResolver,RouteMap> buildRouteMap);
}

public interface IViewRegistry
{
	IEnumerable<ViewMap> Views { get; }
	IViewRegistry Register(params ViewMap[] view);
}
