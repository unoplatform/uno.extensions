namespace Uno.Extensions.Navigation;

public static class RouteMapExtensions
{

	public static IEnumerable<RouteMap> Flatten(this RouteMap route)
	{
		if (route is null)
		{
			yield break;
		}

		yield return route;

		foreach (var subMap in route.Nested.Flatten())
		{
			yield return subMap;
		}
	}

	public static IEnumerable<RouteMap> Flatten(this IEnumerable<RouteMap> routes)
	{
		if (routes is null)
		{
			yield break;
		}

		foreach (var routeMap in routes)
		{
			yield return routeMap;

			foreach (var subMap in routeMap.Nested.Flatten())
			{
				yield return subMap;
			}
		}
	}
}
