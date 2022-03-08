namespace Uno.Extensions.Navigation.Regions;

public static class RegionExtensions
{
    public static INavigator? Navigator(this IRegion region) => region.Services?.GetRequiredService<INavigator>();

    public static Task<NavigationResponse?> NavigateAsync(this IRegion region, NavigationRequest request) => (region.Navigator()?.NavigateAsync(request)) ?? Task.FromResult<NavigationResponse?>(default);

    public static bool IsNamed(this IRegion region) => region.Name is not null;

    public static IRegion Root(this IRegion region)
    {
        return region.Parent is not null ? region.Parent.Root() : region;
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
