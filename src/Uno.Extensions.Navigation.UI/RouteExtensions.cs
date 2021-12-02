using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Uno.Extensions.Navigation;

public static class RouteExtensions
{
	private static Regex nonAlphaRegex = new Regex(@"([^a-zA-Z0-9])+");
	private static Regex alphaRegex = new Regex(@"([a-zA-Z0-9])+");

	public static bool EmptyScheme(this Route route) => string.IsNullOrWhiteSpace(route.Scheme);

	public static bool IsCurrent(this Route route) => route.Scheme == Schemes.Current;

	public static bool IsBackOrCloseNavigation(this Route route) =>
	route.Scheme
		.TrimStart(Schemes.Parent) // Handle eg ../-  which is still a back navigation
		.StartsWith(Schemes.NavigateBack);

	public static bool IsFrameNavigation(this Route route) =>
		route.Scheme.StartsWith(Schemes.NavigateForward) ||
		route.Scheme.StartsWith(Schemes.NavigateBack);

	public static bool IsRoot(this Route route) => route.Scheme.StartsWith(Schemes.Root);

	public static bool IsParent(this Route route) => route.Scheme.StartsWith(Schemes.Parent);

	public static bool IsNested(this Route route, bool checkIfLastScheme = false) => checkIfLastScheme ?
		route.Scheme == Schemes.Nested :
		route.Scheme.StartsWith(Schemes.Nested);

	public static bool IsDialog(this Route route) => route.Scheme.StartsWith(Schemes.Dialog);


	public static bool IsLast(this Route route) => string.IsNullOrWhiteSpace(route?.Path);

	public static bool IsEmpty(this Route route) => route is not null ?
		(route.Scheme == Schemes.Current || route.Scheme == Schemes.Nested) &&
		string.IsNullOrWhiteSpace(route.Base) :
		false;

	// eg -/NextPage
	public static bool FrameIsRooted(this Route route) => route?.Scheme.EndsWith(Schemes.Root + string.Empty) ?? false;

	private static int NumberOfGoBackInScheme(this Route route) => route.Scheme.TakeWhile(x => x + string.Empty == Schemes.NavigateBack).Count();

	public static int FrameNumberOfPagesToRemove(this Route route) =>
		route.FrameIsRooted() ?
			0 :
			(route.FrameIsBackNavigation() ?
				route.NumberOfGoBackInScheme() - 1 :
				route.NumberOfGoBackInScheme());

	// Only navigate back if there is no base. If a base is specified, we do a forward navigate and remove items from the backstack
	public static bool FrameIsBackNavigation(this Route route) =>
		route.Scheme.StartsWith(Schemes.NavigateBack) && route.Base?.Length == 0;

	public static bool FrameIsForwardNavigation(this Route route) => !route.FrameIsBackNavigation();

	public static Route[] ForwardNavigationSegments(this Route route, IMappings mappings)
	{
		if (route.IsEmpty() || route.FrameIsBackNavigation())
		{
			return new Route[] { };
		}

		var segments = new List<Route>() { route with { Scheme = Schemes.NavigateForward, Path = null, Data = (route.IsLastFrameRoute(mappings) ? route.Data : null) } };
		var nextRoute = route.Next();
		while (
			!nextRoute.IsEmpty() && (
			nextRoute.Scheme == Schemes.NavigateForward ||
			nextRoute.IsPageRoute(mappings)))
		{
			segments.Add(nextRoute with { Scheme = Schemes.NavigateForward, Path = null, Data = (nextRoute.IsLastFrameRoute(mappings) ? nextRoute.Data : null) });
			nextRoute = nextRoute.Next();
		}
		return segments.ToArray();
	}

	public static string[] ForwardNavigationSegments(this string path) =>
		path.Split(Schemes.NavigateForward.First()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

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

	public static Route TrimScheme(this Route route, string schemeToTrim)
	{
		return route with { Scheme = route.Scheme.TrimStartOnce(schemeToTrim) };
	}

	public static Route AppendScheme(this Route route, string scheme)
	{
		return route with { Scheme = $"{scheme}{route.Scheme}" };
	}

	public static Route Trim(this Route route, Route? handledRoute)
	{
		if (handledRoute is null)
		{
			return route;
		}

		while (route.Base == handledRoute.Base && !string.IsNullOrWhiteSpace(handledRoute.Base))
		{
			route = route.Next();
			handledRoute = handledRoute.Next();
		}

		return route;
	}

	public static Route Append(this Route route, string nestedPath)
	{
		return route.Append(Route.NestedRoute(nestedPath));
	}

	public static Route Append(this Route route, Route routeToAppend)
	{
		return route with { Path = route.Path + (routeToAppend.Scheme == Schemes.Nested ? Schemes.Separator : routeToAppend.Scheme) + routeToAppend.Base + routeToAppend.Path };
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
			Path = (routeToAppend.Scheme == Schemes.Nested ? Schemes.Separator : routeToAppend.Scheme) +
					routeToAppend.Base +
					routeToAppend.Path +
					(((route.Path?.StartsWith(Schemes.Separator) ?? false) || (route.Path?.StartsWith(Schemes.NavigateForward) ?? false)) ?
						string.Empty :
						Schemes.Separator) +
					route.Path
		};
		//return route with
		//{
		//    Scheme = routeToAppend.Scheme,
		//    Base = routeToAppend.Base,
		//    Path = routeToAppend.Path + (route.Scheme == Schemes.Nested ? Schemes.Separator : route.Scheme) + route.Base + route.Path
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
		var routeBase = path.ExtractBase(out var nextScheme, out var nextPath);
		if (nextScheme == Schemes.Root)
		{
			nextScheme = Schemes.Current;
		}
		return route with { Scheme = nextScheme, Base = routeBase, Path = nextPath };
	}

	public static bool IsPageRoute(this Route route, IMappings mappings)
	{
		return ((mappings.FindView(route))?.ViewType?.IsSubclassOf(typeof(Page)) ?? false);
	}

	public static bool IsLastFrameRoute(this Route route, IMappings mappings)
	{
		return route.IsLast() || !route.Next().IsPageRoute(mappings);
	}

	public static string? NextBase(this Route route)
	{
		return route.Path?.ExtractBase(out var nextScheme, out var nextPath);
		//return route.Path?.Split('/')?.FirstOrDefault();
	}

	public static string NextPath(this Route route)
	{
		route.Path.ExtractBase(out var nextScheme, out var nextPath);
		return nextPath;
	}
	public static string NextScheme(this Route route)
	{
		route.Path.ExtractBase(out var nextScheme, out var nextPath);
		return nextScheme;
	}

	private static string? ExtractBase(this string? path, out string nextScheme, out string nextPath)
	{
		nextPath = path ?? string.Empty;
		nextScheme = string.Empty;

		if (path is null ||
			string.IsNullOrWhiteSpace(path))
		{
			return default;
		}

		var schemeMatch = nonAlphaRegex.Match(path);
		if (schemeMatch.Success)
		{
			path = path.TrimStart(schemeMatch.Value);
			nextScheme = schemeMatch.Value;
		}

		schemeMatch = alphaRegex.Match(path);
		var routeBase = schemeMatch.Success ? schemeMatch.Value : String.Empty;
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

	public static string WithScheme(this string path, string scheme) => string.IsNullOrWhiteSpace(scheme) ? path : $"{scheme}{path}";

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

		var routeBase = ExtractBase(path, out var scheme, out path);

		var route = new Route(scheme, routeBase, path, paras);
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
		return $"{route.Scheme}{route.Base}{route.Path}";
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

		var separator = nextRoute.Scheme == Schemes.Current ? Schemes.Separator : string.Empty;


		var child = nextRoute;
		if (!string.IsNullOrWhiteSpace(regionName) && regionName != route.Base)
		{
			child = child with
			{
				Scheme = Schemes.Current,
				Base = regionName,
				Path = (string.IsNullOrWhiteSpace(child.Scheme) ?
							Schemes.Separator :
							child.Scheme) + child.Base + child.Path
			};
		}

		return route with
		{
			Path = route.Path + separator + child.FullPath(),
			Data = route.Data.Combine(child.Data)
		};
	}

	public static IDictionary<string, object> AsParameters(this IDictionary<string, object> data, ViewMap mapping)
	{
		if (data is null || mapping is null)
		{
			return new Dictionary<string, object>();
		}

		var mapDict = data;
		if (mapping?.UntypedBuildQuery is not null)
		{
			// TODO: Find nicer way to clone the dictionary
			mapDict = data.ToArray().ToDictionary(x => x.Key, x => x.Value);
			if (data.TryGetValue(string.Empty, out var paramData))
			{
				var qdict = mapping.UntypedBuildQuery(paramData);
				qdict.ForEach(qkvp => mapDict[qkvp.Key] = qkvp.Value);
			}
		}
		return mapDict;
	}

	public static Route? ApplyFrameRoute(this Route? currentRoute, IMappings mappings, Route frameRoute)
	{
		var scheme = frameRoute.Scheme;
		if (string.IsNullOrWhiteSpace(frameRoute.Scheme))
		{
			scheme = Schemes.NavigateForward;
		}
		if (currentRoute is null)
		{
			return frameRoute with { Scheme = Schemes.NavigateForward };
		}
		else
		{
			var segments = currentRoute.ForwardNavigationSegments(mappings).ToList();
			foreach (var schemeChar in scheme)
			{
				if (schemeChar + "" == Schemes.NavigateBack)
				{
					segments.RemoveAt(segments.Count - 1);
				}
				else if (schemeChar + "" == Schemes.Root)
				{
					segments.Clear();
				}
			}

			var newSegments = frameRoute.ForwardNavigationSegments(mappings);
			if (newSegments is not null)
			{
				segments.AddRange(newSegments);
			}

			var routeBase = segments.FirstOrDefault()?.Base;
			if (segments.Count > 0)
			{
				segments.RemoveAt(0);
			}

			var routePath = segments.Count > 0 ? string.Join("", segments.Select(x => $"{x.Scheme}{x.Base}")) : string.Empty;

			return new Route(Schemes.NavigateForward, routeBase, routePath, frameRoute.Data);
		}
	}
}
