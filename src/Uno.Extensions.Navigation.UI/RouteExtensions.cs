using System.Text.RegularExpressions;

namespace Uno.Extensions.Navigation;

public static class RouteExtensions
{
	public static bool IsFrameNavigation(this Route route) =>
		// We want to make forward navigation between frames simple, so don't require +
		route.Qualifier == Qualifiers.None ||
		route.Qualifier.StartsWith(Qualifiers.NavigateBack);

	public static bool IsInternal(this Route route) => route.IsInternal;

	public static bool IsRoot(this Route route) => route.Qualifier.StartsWith(Qualifiers.Root);

	// Note: Disabling parent routing - leaving this code in case parent routing is required
	//public static bool IsParent(this Route route) => route.Qualifier.StartsWith(Qualifiers.Parent);

	public static bool IsNested(this Route route) => route.Qualifier.StartsWith(Qualifiers.Nested);

	public static bool IsDialog(this Route route) => route.Qualifier.StartsWith(Qualifiers.Dialog);

	public static bool IsLast(this Route route) => route.Next().IsEmpty();

	public static Route Last(this Route route)
	{
		var next = route.Next();
		while (!next.IsEmpty())
		{
			route = next;
			next = route.Next();
		}

		return route;
	}

	public static bool IsEmpty(this Route route) => route is not null ?
		(route.Qualifier == Qualifiers.None || route.Qualifier == Qualifiers.Nested) &&
		string.IsNullOrWhiteSpace(route.Base) :
		true;

	// eg -/NextPage
	public static bool FrameIsRooted(this Route route) => route?.Qualifier.EndsWith(Qualifiers.Root + string.Empty) ?? false;

	private static int NumberOfGoBackInQualifier(this Route route) => route.Qualifier.TakeWhile(x => x + string.Empty == Qualifiers.NavigateBack).Count();

	public static int FrameNumberOfPagesToRemove(this Route route) =>
		route.FrameIsRooted() ?
			0 :
			(route.FrameIsBackNavigation() ?
				route.NumberOfGoBackInQualifier() - 1 :
				route.NumberOfGoBackInQualifier());

	// Only navigate back if there is no base. If a base is specified, we do a forward navigate and remove items from the backstack
	public static bool FrameIsBackNavigation(this Route route) =>
		route.Qualifier.StartsWith(Qualifiers.NavigateBack) && route.Base?.Length == 0;

	public static bool FrameIsForwardNavigation(this Route route) => !route.FrameIsBackNavigation();

	/// <returns>
	/// Route - a single segment in the route (ie has Base but no Path)
	/// RouteMap - the corresponding route map (if it exists)
	/// bool - whether or not this is a dependson route (ie one that needs to exist in backstack prior to this route)
	/// </returns>
	public static (Route, RouteInfo?, bool)[] ForwardNavigationSegments(
		this Route route,
		IRouteResolver mappings,
		IRegion region)
	{
		// Here we're interested in the actual page navigation segments.
		// Start with an empty list, and progressively add routes that
		// correspond to page navigation segments
		var segs = new List<(Route, RouteInfo?, bool)>();

		if (route.IsEmpty() || route.FrameIsBackNavigation())
		{
			return segs.ToArray();
		}

		// For routes that have a depends on, we need to ensure that
		// the dependson segments are added to the segments list
		var r = route.RootDependsOn(mappings, region, false);
		var map = mappings.FindByPath(r.Base);
		var originalRoute = false;
		while (
			!r.IsEmpty() &&
			(
				r.IsBackOrCloseNavigation() ||
				(
					map is not null &&
					// Checks that there is a View specified and that it inherits from Page
					map.IsPageRouteMap()
				)
			)
		)
		{
			// Check if we've found the origianl route yet (if not, we're still processin dependson routes)
			originalRoute = originalRoute || r.Base == route.Base;
			segs.Add((r with { Qualifier = Qualifiers.None, Path = null, Data = null }, map, !originalRoute));
			r = r.Next();
			map = mappings.FindByPath(r.Base);
		}

		if (segs.Any())
		{
			var last = segs[segs.Count - 1];
			segs[segs.Count - 1] = last with { Item1 = last.Item1 with { Data = route.Data } };
		}

		return segs.ToArray();
	}

	public static object? ResponseData(this Route route) =>
		(route?.Data?.TryGetValue(string.Empty, out var result) ?? false) ? result : null;


	public static Route TrimQualifier(this Route route, string qualifierToTrim)
	{
		return route with { Qualifier = route.Qualifier.TrimStart(qualifierToTrim) };
	}

	public static Route AppendQualifier(this Route route, string qualifier)
	{
		return route with { Qualifier = $"{qualifier}{route.Qualifier}" };
	}

	public static Route Trim(this Route route, Route? handledRoute)
	{
		if (handledRoute is null)
		{
			return route;
		}

		if (route.IsNested() && !handledRoute.IsNested())
		{
			route = route.TrimQualifier(Qualifiers.Nested);
		}

		while (route.Base == handledRoute.Base && !string.IsNullOrWhiteSpace(handledRoute.Base))
		{
			route = route.Next();
			handledRoute = handledRoute.Next();
		}

		if (route.Qualifier == Qualifiers.NavigateBack && route.Qualifier == handledRoute.Qualifier)
		{
			route = route with { Qualifier = Qualifiers.None };
		}

		route = route with
		{
			Base = string.IsNullOrWhiteSpace(route.Base) ? null : route.Base,
			Path = string.IsNullOrWhiteSpace(route.Path) ? null : route.Path
		};

		return route;
	}

	public static Route Append(this Route route, string nestedPath)
	{
		return route.Append(Route.NestedRoute(nestedPath));
	}

	public static Route Append(this Route route, Route routeToAppend)
	{
		if (route.IsEmpty())
		{
			return route with { Base = routeToAppend.Base };
		}
		return route with { Path = route.Path + (routeToAppend.Qualifier == Qualifiers.Nested ? Qualifiers.Separator : routeToAppend.Qualifier) + routeToAppend.Base + routeToAppend.Path };
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

	public static Route Insert(this Route route, Route routeToInsert)
	{
		return routeToInsert.Append(route);
		//return route with
		//{
		//	Path = (routeToAppend.Qualifier == Qualifiers.Nested ? Qualifiers.Separator : routeToAppend.Qualifier) +
		//			routeToAppend.Base +
		//			routeToAppend.Path +
		//			(((route.Path?.StartsWith(Qualifiers.Separator) ?? false)) ?
		//				string.Empty :
		//				Qualifiers.Separator) +
		//			route.Path
		//};
		//return route with
		//{
		//    Qualifier = routeToAppend.Qualifier,
		//    Base = routeToAppend.Base,
		//    Path = routeToAppend.Path + (route.Qualifier == Qualifiers.Nested ? Qualifiers.Separator : route.Qualifier) + route.Base + route.Path
		//};
	}

	public static Route InsertPage<TPage>(this Route route)
	{
		return route.Insert(Route.PageRoute<TPage>());
	}

	public static bool ContainsView<TView>(this Route route)
	{
		return route.ContainsView(typeof(TView));
	}

	public static bool ContainsView(this Route route, Type viewType)
	{
		return route.Contains(viewType.Name);
	}

	public static bool Contains(this Route route, string path)
	{
		return route.Base == path || (route.Path?.Contains(path, StringComparison.InvariantCultureIgnoreCase) ?? false);
	}

	public static Route Next(this Route route)
	{
		var path = route.Path ?? string.Empty;
		var routeBase = path.ExtractBase(out var nextQualifier, out var nextPath);
		if (nextQualifier.StartsWith(Qualifiers.Root))
		{
			nextQualifier = nextQualifier.TrimStartOnce(Qualifiers.Root);
		}
		return route with { Qualifier = nextQualifier, Base = routeBase, Path = nextPath };
	}

	public static bool IsPageRoute(this Route route, IRouteResolver mappings)
	{
		return mappings.FindByPath(route.Base).IsPageRouteMap();
	}

	public static bool IsPageRouteMap(this RouteInfo? rm)
	{
		return (rm?.RenderView?.IsSubclassOf(typeof(Page)) ?? false);
	}

	public static bool IsLastFrameRoute(this Route route, IRouteResolver mappings)
	{
		return route.IsLast() || !route.Next().IsPageRoute(mappings);
	}

	public static string? NextBase(this Route route)
	{
		return route.Path?.ExtractBase(out var nextQualifier, out var nextPath);
		//return route.Path?.Split('/')?.FirstOrDefault();
	}

	public static string NextPath(this Route route)
	{
		route.Path.ExtractBase(out var _, out var nextPath);
		return nextPath;
	}
	public static string NextQualifier(this Route route)
	{
		route.Path.ExtractBase(out var nextQualifier, out var _);
		return nextQualifier;
	}

	public static bool HasQualifier(this string? path)
	{
		var _ = path.ExtractBase(out var qualifier, out var _);
		return !string.IsNullOrWhiteSpace(qualifier);
	}


	public static string FullPath(this Route route)
	{
		return $"{route.Qualifier}{route.Base}{route.Path}";
	}

	public static IDictionary<string, object> Combine(this IDictionary<string, object>? data, IDictionary<string, object>? childData)
	{
		if (data is null)
		{
			return childData ?? new Dictionary<string, object>();
		}

		if (childData is not null)
		{
			childData.ToArray().ForEach(x => data[x.Key] = x.Value);
		}

		return data;
	}

	public static Route? Merge(this Route? route, IEnumerable<(string?, Route?)>? childRoutes)
	{
		if (childRoutes is null)
		{
			return route;
		}

		var deepestChild = childRoutes.ToArray().OrderByDescending(x => x.Item2?.ToString().Length ?? 0).FirstOrDefault();

		if (route is null || route.IsEmpty())
		{
			return deepestChild.Item2;
		}

		var (regionName, nextRoute) = deepestChild;
		if (nextRoute is null)
		{
			return route;
		}

		if (nextRoute.IsEmpty())
		{
			return route;
		}

		var separator = nextRoute.Qualifier == Qualifiers.None ? Qualifiers.Separator : string.Empty;


		var child = nextRoute;
		if (!string.IsNullOrWhiteSpace(regionName) && regionName != route.Base)
		{
			child = child with
			{
				Qualifier = Qualifiers.None,
				Base = regionName,
				Path = (string.IsNullOrWhiteSpace(child.Qualifier) ?
							Qualifiers.Separator :
							child.Qualifier) + child.Base + child.Path
			};
		}

		return route with
		{
			Path = route.Path + separator + child.FullPath(),
			Data = route.Data.Combine(child.Data)
		};
	}

	public static IDictionary<string, object> AsParameters(this IDictionary<string, object> data, RouteInfo mapping)
	{
		if (data is null || mapping is null)
		{
			return new Dictionary<string, object>();
		}

		var mapDict = data;
		if (mapping?.ToQuery is not null)
		{
			// TODO: Find nicer way to clone the dictionary
			mapDict = data.ToArray().ToDictionary(x => x.Key, x => x.Value);
			if (data.TryGetValue(string.Empty, out var paramData))
			{
				var qdict = mapping.ToQuery(paramData);
				qdict.ForEach(qkvp => mapDict[qkvp.Key] = qkvp.Value);
			}
		}
		return mapDict;
	}

	public static Route? ApplyFrameRoute(this Route? currentRoute, IRouteResolver resolver, Route frameRoute, IRegion region)
	{
		var qualifier = frameRoute.Qualifier;
		if (currentRoute is null)
		{
			return frameRoute with { Qualifier = Qualifiers.None };
		}
		else
		{
			var segments = currentRoute.ForwardNavigationSegments(resolver, region).ToList();
			foreach (var qualifierChar in qualifier)
			{
				if (qualifierChar + "" == Qualifiers.NavigateBack)
				{
					segments.RemoveAt(segments.Count - 1);
				}
				else if (qualifierChar + "" == Qualifiers.Root)
				{
					segments.Clear();
				}
			}

			var newSegments = frameRoute.ForwardNavigationSegments(resolver, region);
			if (newSegments is not null)
			{
				newSegments = (from seg in newSegments
							   where !(seg.Item3 && segments.Any(x => x.Item1.Base == seg.Item1.Base))
							   select seg).ToArray();
				segments.AddRange(newSegments);
			}

			var routeBase = segments.FirstOrDefault().Item1?.Base;
			if (segments.Count > 0)
			{
				segments.RemoveAt(0);
			}

			var routePath = segments.Count > 0 ? string.Join(Qualifiers.Separator, segments.Select(x => $"{x.Item1.Base}")) : string.Empty;

			return new Route(Qualifiers.None, routeBase, routePath, frameRoute.Data);
		}
	}

	public static Route RootDependsOn(this Route currentRoute, IRouteResolver resolver, IRegion region, bool includeCurrentRegion)
	{
		var rm = resolver.FindByPath(currentRoute.Base);
		//if ((rm is null ||
		//	string.IsNullOrEmpty(rm.DependsOn)) &&
		//	region.Navigator()?.Route?.Base == currentRoute.Base &&
		//	!includeCurrentRegion)
		//{
		//	return Route.Empty;
		//}

		while (rm is not null &&
			!string.IsNullOrEmpty(rm.DependsOn))
		{
			var ancestors = region.Ancestors(true);
			if (ancestors.Any(x => x.Item1?.Base == rm.DependsOn))
			{
				// In the scenario where we're testing to see if we can navigate to the currentRoute
				// the root Route needs to include the route for the current region
				// In the scenario where we're navigating to the currentRoute, we don't
				// want to include the route for the current region (since the region
				// is already at that route)
				if (includeCurrentRegion &&
					region.Navigator()?.Route?.Base == rm.DependsOn)
				{
					currentRoute = currentRoute.Insert(rm.DependsOn);
				}

				return currentRoute;
			}

			currentRoute = currentRoute.Insert(rm.DependsOn);


			rm = resolver.FindByPath(rm.DependsOn);
		}

		return currentRoute;

	}
}
