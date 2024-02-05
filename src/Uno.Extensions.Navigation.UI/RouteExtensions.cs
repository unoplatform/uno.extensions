using System.Text.RegularExpressions;

namespace Uno.Extensions.Navigation;

public static class RouteExtensions
{
	// eg -/NextPage
	internal static bool FrameIsRooted(this Route route) => route?.Qualifier.EndsWith(Qualifiers.Root + string.Empty) ?? false;

	private static int NumberOfGoBackInQualifier(this Route route) => route.Qualifier.TakeWhile(x => x + string.Empty == Qualifiers.NavigateBack).Count();

	internal static int FrameNumberOfPagesToRemove(this Route route) =>
		route.FrameIsRooted() ?
			0 :
			(route.FrameIsBackNavigation() ?
				route.NumberOfGoBackInQualifier() - 1 :
				route.NumberOfGoBackInQualifier());

	// Only navigate back if there is no base. If a base is specified, we do a forward navigate and remove items from the backstack
	internal static bool FrameIsBackNavigation(this Route route) =>
		route.Qualifier.StartsWith(Qualifiers.NavigateBack) && route.Base?.Length == 0;

	internal static bool FrameIsForwardNavigation(this Route route) => !route.FrameIsBackNavigation();

	internal static RouteInfo[] ForwardSegments(
		this Route route,
		IRouteResolver resolver)
	{
		var segments = new List<RouteInfo>();
		var rm = !string.IsNullOrWhiteSpace(route.Base) ? resolver.FindByPath(route.Base) : default;
		while (rm is not null &&
			rm.IsPageRouteMap())
		{
			var dependsOn = rm.DependsOnSegments();
			var newOnly = dependsOn.Where(x => !segments.Contains(x)).ToArray();
			segments.AddRange(newOnly);

			route = route.Next();
			rm = !string.IsNullOrWhiteSpace(route.Base) ? resolver.FindByPath(route.Base) : default;
		}

		return segments.ToArray();
	}

	internal static RouteInfo[] ForwardSegments(
		this Route route,
		IRouteResolver resolver,
		INavigator navigator)
	{
		var isClear = route.IsClearBackstack();
		var segments = route.ForwardSegments(resolver);

		var navRoute = (navigator is IStackNavigator deepNav) ? deepNav.FullRoute : navigator.Route;
		if (!isClear && navRoute is not null && !navRoute.IsEmpty())
		{
			return segments.Where(x => !navRoute.Contains(x.Path)).ToArray();
		}
		return segments.ToArray();
	}

	internal static RouteInfo[] Segments(
		this Route route,
		IRouteResolver resolver)
	{
		var segments = new List<RouteInfo>();

		var rm = resolver.FindByPath(route.Base);
		while (rm is not null)
		{
			segments.Add(rm);
			route = route.Next();
			rm = resolver.FindByPath(route.Base);
		}

		return segments.ToArray();
	}



	public static Route AppendPage<TPage>(this Route route)
	{
		return route.Append(Route.PageRoute<TPage>());
	}

	public static Route AppendNested<TView>(this Route route)
	{
		return route.Append(Route.NestedRoute<TView>());
	}

	public static Route Insert(this Route route, string pathToInsert)
	{
		return route.Insert(Route.PageRoute(pathToInsert));
	}



	public static Route InsertPage<TPage>(this Route route)
	{
		return route.Insert(Route.PageRoute<TPage>());
	}

	public static bool IsPageRoute(this Route route, IRouteResolver mappings)
	{
		return mappings.FindByPath(route.Base).IsPageRouteMap();
	}

	public static bool IsPageRouteMap(this RouteInfo? rm)
	{
		return (rm?.RenderView?.IsSubclassOf(typeof(Page)) ?? false);
	}

	internal static bool IsLastFrameRoute(this Route route, IRouteResolver mappings)
	{
		return route.IsLast() || !route.Next().IsPageRoute(mappings);
	}



	internal static Route? ApplyFrameRoute(this Route? currentRoute, IRouteResolver resolver, Route frameRoute, INavigator navigator)
	{
		var qualifier = frameRoute.Qualifier;
		var segments = currentRoute?.Segments(resolver).ToList() ?? new();
		foreach (var qualifierChar in qualifier)
		{
			if (qualifierChar + "" == Qualifiers.NavigateBack && segments.Count > 0)
			{
				segments.RemoveAt(segments.Count - 1);
			}
			else if (qualifierChar + "" == Qualifiers.Root)
			{
				segments.Clear();
			}
		}

		var newSegments = frameRoute.ForwardSegments(resolver);
		if (newSegments is not null)
		{
			var newOnly = newSegments.Where(x => !segments.Contains(x)).ToArray();
			segments.AddRange(newOnly);
		}

		var routeBase = segments.FirstOrDefault()?.Path;
		if (segments.Count > 0)
		{
			segments.RemoveAt(0);
		}

		var routePath = segments.Count > 0 ? string.Join(Qualifiers.Separator, segments.Select(x => $"{x.Path}")) : string.Empty;

		return new Route(Qualifiers.None, routeBase, routePath, frameRoute.Data);
	}
}
