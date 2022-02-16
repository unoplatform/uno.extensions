using System.Text.RegularExpressions;

namespace Uno.Extensions.Navigation;

public static class RouteExtensions
{
	private static Regex nonAlphaRegex = new Regex(@"([^a-zA-Z0-9])+");
	private static Regex alphaRegex = new Regex(@"([a-zA-Z0-9])+");

	public static bool IsBackOrCloseNavigation(this Route route) =>
		route.Qualifier
			.StartsWith(Qualifiers.NavigateBack);

	public static bool IsFrameNavigation(this Route route) =>
		// We want to make forward navigation between frames simple, so don't require +
		route.Qualifier == Qualifiers.None || 
		route.Qualifier.StartsWith(Qualifiers.NavigateBack);

	public static bool IsInternal(this Route route) => route.IsInternal;

	public static bool IsRoot(this Route route) => route.Qualifier.StartsWith(Qualifiers.Root);

	public static bool IsChangeContent(this Route route) =>
		(route.Qualifier.StartsWith(Qualifiers.ChangeContent) && !route.IsParent()) ||
		(route.Qualifier==Qualifiers.None && route.IsInternal);

	public static bool IsParent(this Route route) => route.Qualifier.StartsWith(Qualifiers.Parent);

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
		(route.Qualifier == Qualifiers.None || route.Qualifier == Qualifiers.ChangeContent || route.Qualifier == Qualifiers.Nested) &&
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

	public static Route[] ForwardNavigationSegments(this Route route, IRouteResolver mappings)
	{
		if (route.IsEmpty() || route.FrameIsBackNavigation())
		{
			return new Route[] { };
		}

		var segments = new List<Route>() { route with { Qualifier = Qualifiers.None, Path = null, Data = (route.IsLastFrameRoute(mappings) ? route.Data : null) } };
		var nextRoute = route.Next();
		while (
			!nextRoute.IsEmpty() && 
			nextRoute.IsPageRoute(mappings))
		{
			segments.Add(nextRoute with { Qualifier = Qualifiers.None, Path = null, Data = (nextRoute.IsLastFrameRoute(mappings) ? nextRoute.Data : null) });
			nextRoute = nextRoute.Next();
		}
		return segments.ToArray();
	}

	public static string[] ForwardNavigationSegments(this string path) =>
		path.Split(Qualifiers.Separator.First()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

	public static object? ResponseData(this Route route) =>
		(route?.Data?.TryGetValue(string.Empty, out var result) ?? false) ? result : null;

	public static string TrimStartOnce(this string text, string textToTrim)
	{
		if (text.StartsWith(textToTrim))
		{
			if (text.Length == textToTrim.Length)
			{
				return string.Empty;
			}

			return text.Substring(textToTrim.Length);
		}

		return text;
	}

	public static Route TrimQualifier(this Route route, string qualifierToTrim)
	{
		return route with { Qualifier = route.Qualifier.TrimStartOnce(qualifierToTrim) };
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

		if(route.IsNested() && !handledRoute.IsNested())
		{
			route = route.TrimQualifier(Qualifiers.Nested);
		}

		while (route.Base == handledRoute.Base && !string.IsNullOrWhiteSpace(handledRoute.Base))
		{
			route = route.Next();
			handledRoute = handledRoute.Next();
		}

		if(route.Qualifier==Qualifiers.NavigateBack && route.Qualifier == handledRoute.Qualifier)
		{
			route = route with { Qualifier = Qualifiers.None };
		}

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
		return route.Insert(Route.NestedRoute(pathToInsert));
	}

	public static Route Insert(this Route route, Route routeToAppend)
	{
		return route with
		{
			Path = (routeToAppend.Qualifier == Qualifiers.Nested ? Qualifiers.Separator : routeToAppend.Qualifier) +
					routeToAppend.Base +
					routeToAppend.Path +
					(((route.Path?.StartsWith(Qualifiers.Separator) ?? false) ) ?
						string.Empty :
						Qualifiers.Separator) +
					route.Path
		};
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
		if (nextQualifier == Qualifiers.Root)
		{
			nextQualifier = Qualifiers.None;
		}
		return route with { Qualifier = nextQualifier, Base = routeBase, Path = nextPath };
	}

	public static bool IsPageRoute(this Route route, IRouteResolver mappings)
	{
		return ((mappings.Find(route))?.View?.IsSubclassOf(typeof(Page)) ?? false);
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

	public static bool HasQualifier(this string path)
	{
		var _ = ExtractBase(path, out var qualifier, out var _);
		return !string.IsNullOrWhiteSpace(qualifier);
	}
	public static string? ExtractBase(this string? path, out string nextQualifier, out string nextPath)
	{
		nextPath = path ?? string.Empty;
		nextQualifier = string.Empty;

		if (path is null ||
			string.IsNullOrWhiteSpace(path))
		{
			return default;
		}

		var qualifierMatch = nonAlphaRegex.Match(path);
		if (qualifierMatch.Success)
		{
			path = path.TrimStart(qualifierMatch.Value);
			nextQualifier = qualifierMatch.Value;
		}

		qualifierMatch = alphaRegex.Match(path);
		var routeBase = qualifierMatch.Success ? qualifierMatch.Value : String.Empty;
		if (routeBase is { Length: > 0 })
		{
			if (path.Length > routeBase.Length + 1)
			{
				nextPath = path.TrimStartOnce(routeBase);
			}
			else
			{
				nextPath = string.Empty;
			}
		}
		else
		{
			nextPath = path;
		}
		return routeBase;
	}

	public static string WithQualifier(this string path, string qualifier) => string.IsNullOrWhiteSpace(qualifier) ? path : $"{qualifier}{path}";

	public static Route AsRoute(this Uri uri, object? data = null)
	{
		var path = uri.OriginalString;
		return path.AsRoute(data);
	}

	public static Route AsRoute(this string path, object? data = null)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return Route.Empty;
		}

		var queryIdx = path.IndexOf('?');
		var query = string.Empty;
		if (queryIdx >= 0)
		{
			queryIdx++; // Step over the ?
			query = queryIdx < path.Length ? path.Substring(queryIdx) : string.Empty;
			path = path.Substring(0, queryIdx - 1);
		}

		var paras = ParseQueryParameters(query);
		if (data is not null)
		{
			if (data is IDictionary<string, object> paraDict)
			{
				paras.AddRange(paraDict);
			}
			else
			{
				paras[string.Empty] = data;
			}
		}

		var routeBase = ExtractBase(path, out var qualifier, out path);

		var route = new Route(qualifier, routeBase, path, paras);
		if (route.IsBackOrCloseNavigation() &&
			data is not null &&
			data is not IOption)
		{
			data = Option.Some<object>(data);
			paras[string.Empty] = data;
			route = route with { Data = paras };
		}
		return route;
	}

	private static IDictionary<string, object> ParseQueryParameters(this string queryString)
	{
		return (from pair in (queryString + string.Empty).Split('&')
				where pair is not null
				let bits = pair.Split('=')
				where bits.Length == 2
				let key = bits[0]
				let val = bits[1]
				where key is not null && val is not null
				select new { key, val })
				.ToDictionary(x => x.key, x => (object)x.val);
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

	public static IDictionary<string, object> AsParameters(this IDictionary<string, object> data, RouteMap mapping)
	{
		if (data is null || mapping is null)
		{
			return new Dictionary<string, object>();
		}

		var mapDict = data;
		if (mapping?.UntypedToQuery is not null)
		{
			// TODO: Find nicer way to clone the dictionary
			mapDict = data.ToArray().ToDictionary(x => x.Key, x => x.Value);
			if (data.TryGetValue(string.Empty, out var paramData))
			{
				var qdict = mapping.UntypedToQuery(paramData);
				qdict.ForEach(qkvp => mapDict[qkvp.Key] = qkvp.Value);
			}
		}
		return mapDict;
	}

	public static Route? ApplyFrameRoute(this Route? currentRoute, IRouteResolver routeResolver, Route frameRoute)
	{
		var qualifier = frameRoute.Qualifier;
		if (currentRoute is null)
		{
			return frameRoute with { Qualifier = Qualifiers.None };
		}
		else
		{
			var segments = currentRoute.ForwardNavigationSegments(routeResolver).ToList();
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

			var newSegments = frameRoute.ForwardNavigationSegments(routeResolver);
			if (newSegments is not null)
			{
				segments.AddRange(newSegments);
			}

			var routeBase = segments.FirstOrDefault()?.Base;
			if (segments.Count > 0)
			{
				segments.RemoveAt(0);
			}

			var routePath = segments.Count > 0 ? string.Join(Qualifiers.Separator, segments.Select(x => $"{x.Base}")) : string.Empty;

			return new Route(Qualifiers.None, routeBase, routePath, frameRoute.Data);
		}
	}
}
