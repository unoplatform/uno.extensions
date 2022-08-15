namespace Uno.Extensions.Navigation.Regions;

public static class RegionExtensions
{
	public static INavigator? Navigator(this IRegion region) => region.Services?.GetRequiredService<INavigator>();

	public static Task<bool> CanNavigate(this IRegion region, Route route)
	{
		var navigator = region.Navigator();
		return navigator is not null ? navigator.CanNavigate(route) : Task.FromResult(false);
	}

	public static Task<NavigationResponse?> NavigateAsync(this IRegion region, NavigationRequest request) => (region.Navigator()?.NavigateAsync(request)) ?? Task.FromResult<NavigationResponse?>(default);

	public static bool IsUnnamed(this IRegion region, Route? parentRoute=null) =>
		string.IsNullOrEmpty(region.Name) ||
		(region.Name == parentRoute?.Base);  // Where an un-named region is nested, the name is updated to the current route

	public static IRegion Root(this IRegion region)
	{
		return region.Parent is not null ? region.Parent.Root() : region;
	}

	internal static (Route?, IRegion, INavigator?)[] Ancestors(
		this IRegion region,
		bool includeRegion = true,
		IList<(Route?, IRegion, INavigator?)>? regions = default)
	{
		if (regions is null)
		{
			regions = new List<(Route?, IRegion, INavigator?)>();
		}
		if (includeRegion)
		{
			var nav = region.Navigator();
			var route = (nav is IDeepRouteNavigator deepNav) ? deepNav.FullRoute : nav?.Route;
			regions.Insert(0, (route, region, nav));
		}

		if (region.Parent is not null)
		{
			region.Parent.Ancestors(includeRegion: true, regions);
		}

		return regions.ToArray();
	}

	public static Route? GetRoute(this IRegion region)
	{
		var regionRoute = region.Navigator()?.Route;
		return regionRoute.Merge(
							region?.Children
								.Select(x => (x.Name, CurrentRoute: x.GetRoute())));
	}

	public static IEnumerable<IRegion> FindChildren(this IRegion region, Func<IRegion, bool> predicate)
	{
		foreach (var child in region.Children)
		{
			if (predicate(child))
			{
				yield return child;
			}

			foreach (var nested in child.FindChildren(predicate))
			{
				yield return nested;
			}
		}
	}
}
