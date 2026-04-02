namespace Uno.Extensions.Navigation;

public class RouteRegistry : Registry<RouteMap>, IRouteRegistry
{
	public RouteRegistry(IServiceCollection services) : base(services)
	{
	}

	protected override void InsertItem(RouteMap item)
	{
		base.InsertItem(item);
		
		// Register types for the ViewMap associated with this RouteMap
		item.View?.RegisterTypes(Services);
		
		// Recursively register types for nested RouteMaps
		if (item.Nested is not null)
		{
			foreach (var nestedRoute in item.Nested)
			{
				InsertItem(nestedRoute);
			}
		}
	}
}
