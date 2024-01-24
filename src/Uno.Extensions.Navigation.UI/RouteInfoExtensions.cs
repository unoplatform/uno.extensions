namespace Uno.Extensions.Navigation;

internal static class RouteInfoExtensions
{
	internal static RouteInfo[] Ancestors(this RouteInfo routeInfo, IRouteResolver resolver)
	{
		var routes = new List<RouteInfo>();
		routeInfo.NavigatorAncestors(resolver, routes);
		return routes.ToArray();
	}

	private static void NavigatorAncestors(this RouteInfo routeInfo, IRouteResolver resolver, IList<RouteInfo> routes)
	{
		routes.Insert(0, routeInfo);

		while (routeInfo?.DependsOnRoute is { } dependee)
		{
			routes.Insert(0, dependee);
			routeInfo = dependee;
		}

		if (routeInfo?.Parent is { } parent)
		{
			parent.NavigatorAncestors(resolver, routes);
		}
	}

	internal static RouteInfo[] Ancestors(this INavigator navigator, IRouteResolver resolver)
	{
		var routes = new List<RouteInfo>();
		navigator.NavigatorAncestors(resolver, routes);
		return routes.ToArray();
	}

	private static void NavigatorAncestors(this INavigator navigator, IRouteResolver resolver, IList<RouteInfo> routes)
	{
		var route = (navigator is IStackNavigator deepNav) ? deepNav.FullRoute : navigator?.Route;
		while (!(route?.IsEmpty() ?? true))
		{
			var info = resolver.FindByPath(route.Base);
			if (info is not null)
			{
				routes.Insert(0, info);
			}
			route = route.Next();
		}

		if (navigator?.GetParent() is { } parent)
		{
			parent.NavigatorAncestors(resolver, routes);
		}
	}
}
