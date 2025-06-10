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

	public static bool IsUnnamed(this IRegion region, Route? parentRoute = null) =>
		string.IsNullOrEmpty(region.Name) ||
		(region.Name == parentRoute?.Base);  // Where an un-named region is nested, the name is updated to the current route

	/// <summary>
	/// Returns the root region at the top of the region hierarchy
	/// </summary>
	/// <param name="region">The start point of the hierarchy search</param>
	/// <returns>The root region</returns>
	public static IRegion Root(this IRegion region) => region.Parent?.Root() ?? region;

	/// <summary>
	/// Returns all of the ancestor regions for the  specified region
	/// </summary>
	/// <param name="region">The start region</param>
	/// <param name="includeRegion">Whether to include the start region</param>
	/// <param name="regions">The list to add ancestor regions to</param>
	/// <returns>The array of ancestor regions - first region is either the start region or the first parent</returns>
	internal static (Route? Route, IRegion Region, INavigator? Navigator)[] Ancestors(
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
			var route = (nav is IStackNavigator deepNav) ? deepNav.FullRoute : nav?.Route;
			regions.Add((route, region, nav));
		}

		if (region.Parent is not null)
		{
			region.Parent.Ancestors(includeRegion: true, regions);
		}

		return regions.ToArray();
	}

	public static Route? GetRoute(this IRegion region)
	{
		var navigator = region.Navigator();
		var regionRoute = navigator?.Route;

		IEnumerable<(string? Name, Route? CurrentRoute)>? children;

		if (navigator is PanelVisiblityNavigator pvn)
		{
			children = region?.Children
						.FirstOrDefault(x => x.Navigator()?.Route == pvn.CurrentRoute) is { } childRegion
						? new[] { (childRegion.Name, CurrentRoute: childRegion.GetRoute()) }
						: null;
		}
		else
		{
			children = region?.Children.Select(x => (x.Name, CurrentRoute: x.GetRoute()));
		}

		return regionRoute.Merge(children);
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
