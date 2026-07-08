using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

/// <summary>
/// Event args for when a route has changed
/// </summary>
public class RouteChangedEventArgs : EventArgs
{
	/// <summary>
	/// The root region where the route has changed
	/// </summary>
	public IRegion Region { get; }

	/// <summary>
	/// The navigator where the route changed (leaf region in hierarchy)
	/// </summary>
	public INavigator? Navigator { get; }

	/// <summary>
	/// The current route after the navigation completed, reconstructed from the region tree
	/// </summary>
	public Route? Route { get; }

	/// <summary>
	/// Constructs a new instance
	/// </summary>
	/// <param name="region">The root region where route was changed</param>
	/// <param name="navigator">The navigator that changed the route</param>
	[Obsolete("Use the overload that accepts a Route parameter.")]
	public RouteChangedEventArgs(IRegion region, INavigator? navigator)
		: this(region, navigator, route: null)
	{
	}

	/// <summary>
	/// Constructs a new instance
	/// </summary>
	/// <param name="region">The root region where route was changed</param>
	/// <param name="navigator">The navigator that changed the route</param>
	/// <param name="route">The current route after navigation</param>
	public RouteChangedEventArgs(IRegion region, INavigator? navigator, Route? route)
	{
		Region = region;
		Navigator = navigator;
		Route = route;
	}
}

