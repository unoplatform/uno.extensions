namespace Uno.Extensions.Navigation;

public class RouteRegistry : Registry<RouteMap>, IRouteRegistry
{
	public RouteRegistry(IServiceCollection services) : base(services)
	{
	}
}
