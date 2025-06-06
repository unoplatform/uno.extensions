using System.IO;
using System.Text.RegularExpressions;

namespace Uno.Extensions.Navigation;

public static class RouteExtensions
{
	private static Regex nonAlphaRegex = new Regex(@"([^a-zA-Z0-9])+");
	private static Regex alphaRegex = new Regex(@"([a-zA-Z0-9])+");

	/// <summary>
	/// Gets the navigation data from the route.
	/// </summary>
	/// <param name="route">The route instance.</param>
	/// <returns>The navigation data, or null if not available.</returns>
	public static object? NavigationData(this Route route) =>
		(route?.Data?.TryGetValue(string.Empty, out var navData) ?? false) ? navData : default;

	/// <summary>
	/// Determines if the route represents a back navigation by its Qualifier.
	/// </summary>
	/// <param name="route">The route instance.</param>
	/// <returns>True if the route is a back or close navigation; otherwise, false.</returns>
	public static bool IsBackOrCloseNavigation(this Route route) =>
		route.Qualifier.StartsWith(Qualifiers.NavigateBack);

	/// <summary>
	/// Determines if the route clears the BackStack.
	/// </summary>
	/// <param name="route">The route instance.</param>
	/// <returns>True if the route clears the BackStack; otherwise, false.</returns>
	public static bool IsClearBackStack(this Route route) =>
		route.Qualifier
			.StartsWith(Qualifiers.ClearBackStack);

	/// <summary>
	/// Extracts the base path, qualifier, and next path from the given path.
	/// </summary>
	/// <param name="path">The full path.</param>
	/// <param name="nextQualifier">The extracted qualifier.</param>
	/// <param name="nextPath">The remaining path after the base is extracted.</param>
	/// <returns>The base path, or null if not available.</returns>
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
		if (qualifierMatch.Success && qualifierMatch.Index == 0)
		{
			path = path.TrimStart(qualifierMatch.Value);
			nextQualifier = qualifierMatch.Value;
		}

		var routeBase = path.Split("/").FirstOrDefault();
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

	/// <summary>
	/// Trims the specified text from the start of the string once.
	/// </summary>
	/// <param name="text">The original string.</param>
	/// <param name="textToTrim">The text to trim.</param>
	/// <returns>The trimmed string.</returns>
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

	/// <summary>
	/// Removes the qualifier from the given path.
	/// </summary>
	/// <param name="path">The path with a qualifier.</param>
	/// <returns>The path without the qualifier.</returns>
	public static string? WithoutQualifier(this string? path)
	{
		if(path is null ||
			string.IsNullOrWhiteSpace(path))
		{
			return path;
		}

		var qualifierMatch = nonAlphaRegex.Match(path);
		if (qualifierMatch.Success && qualifierMatch.Index == 0)
		{
			return path.TrimStart(qualifierMatch.Value);
		}
		return path;
	}

	/// <summary>
	/// Adds a qualifier to the given path.
	/// </summary>
	/// <param name="path">The original path.</param>
	/// <param name="qualifier">The qualifier to add.</param>
	/// <returns>The path with the qualifier.</returns>
	public static string WithQualifier(this string path, string? qualifier) => (qualifier is null || string.IsNullOrWhiteSpace(qualifier)) ? path : $"{qualifier}{path}";

	/// <summary>
	/// Converts a RouteInfo instance to a Route.
	/// </summary>
	/// <param name="map">The RouteInfo instance.</param>
	/// <returns>The corresponding Route.</returns>
	public static Route AsRoute(this RouteInfo map)
	{
		return new Route(Qualifiers.None, map.Path);
	}

	/// <summary>
	/// Converts a URI to a Route.
	/// </summary>
	/// <param name="uri">The URI to convert.</param>
	/// <param name="data">Optional data to include in the route.</param>
	/// <param name="resolver">Optional route resolver.</param>
	/// <returns>The corresponding Route.</returns>
	public static Route AsRoute(this Uri uri, object? data = null, IRouteResolver? resolver = null)
	{
		var path = uri.OriginalString;
		return path.AsRoute(data, resolver);
	}

	public static Route AsRoute(this string? path, object? data = null, IRouteResolver? resolver = null)
	{
		if (path is null ||
			string.IsNullOrWhiteSpace(path))
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

		if(data is { } routeData)
		{
			var routeDataDictionary = data as IDictionary<string, object>;
			routeDataDictionary ??= new Dictionary<string, object> { { string.Empty, data } };
			paras = Combine(paras, routeDataDictionary);
		}

		var routeBase = ExtractBase(path, out var qualifier, out path);

		if (resolver is not null &&
			!string.IsNullOrWhiteSpace(routeBase) &&
			string.IsNullOrWhiteSpace(qualifier))
		{
			var map = resolver.FindByPath(routeBase);
			if (map?.IsDialogViewType?.Invoke() ?? false)
			{
				qualifier = Qualifiers.Dialog;
			}
		}

		var route = new Route(qualifier, routeBase, path, paras);
		if ((route.IsBackOrCloseNavigation() && !route.IsClearBackStack()) &&
			data is not null &&
			data is not IOption)
		{
			data = Option.Some<object>(data);
			paras[string.Empty] = data;
			route = route with { Data = paras };
		}
		return route;
	}

	/// <summary>
	/// Parses query parameters from a query string.
	/// </summary>
	/// <param name="queryString">The query string.</param>
	/// <returns>A dictionary of query parameters.</returns>
	public static IDictionary<string, object> ParseQueryParameters(this string queryString)
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

	public static string? Query(this Route route) =>
	((route?.Data?.Where(x => x.Key != string.Empty)?.Any()) ?? false) ?
	"?" + string.Join("&", route.Data.Where(x => x.Key != string.Empty).Select(kvp => $"{kvp.Key}={kvp.Value}")) :
	null;

	/// <summary>
	/// Determines if the route is a frame navigation.
	/// </summary>
	/// <param name="route">The route instance.</param>
	/// <returns>True if the route is a frame navigation; otherwise, false.</returns>
	public static bool IsFrameNavigation(this Route route) =>
	// We want to make forward navigation between frames simple, so don't require +
	route.Qualifier == Qualifiers.None ||
	route.Qualifier.StartsWith(Qualifiers.NavigateBack);

	/// <summary>
	/// Determines if the route is internal.
	/// </summary>
	/// <param name="route">The route instance.</param>
	/// <returns>True if the route is internal; otherwise, false.</returns>
	public static bool IsInternal(this Route route) => route.IsInternal;

	/// <summary>
	/// Determines if the route is the root route.
	/// </summary>
	/// <param name="route">The route instance.</param>
	/// <returns>True if the route is the root route; otherwise, false.</returns>
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

	public static object? ResponseData(this Route route) => route.NavigationData();


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

		return route with
		{
			Path = string.IsNullOrWhiteSpace(route.Path) ?
						routeToAppend.Base + (!string.IsNullOrWhiteSpace(routeToAppend.Base) && !string.IsNullOrWhiteSpace(routeToAppend.Path) ? Qualifiers.Separator : "") + routeToAppend.Path :
						route.Path + ((routeToAppend.Qualifier == Qualifiers.Nested || routeToAppend.Qualifier == Qualifiers.None) ? Qualifiers.Separator : routeToAppend.Qualifier) + routeToAppend.Base + routeToAppend.Path
		};
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
		return route.Base == path || (route.Path?.Split('/').Any(x => x == path) ?? false);
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
		var result = new Dictionary<string, object>();

		if (data is not null)
		{
			foreach (var x in data)
			{
				result[x.Key] = x.Value;
			}
		}

		if (childData is not null)
		{
			foreach (var x in childData)
			{
				result[x.Key] = x.Value;
			}
		}

		return result;
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

	public static Route AsInternal(this Route route)
	{
		return route with { IsInternal = true };
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
				foreach (var qkvp in qdict)
				{
					mapDict[qkvp.Key] = qkvp.Value;
				}
			}
		}
		return mapDict;
	}

	public static RouteInfo[] DependsOnSegments(this RouteInfo? rm)
	{

		var segments = new List<RouteInfo>();

		while (rm is not null)
		{
			segments.Insert(0, rm);
			rm = rm.DependsOnRoute;
		}

		return segments.ToArray();
	}


	private static string TrimStart(this string source, string trimText)
	{
		if (!string.IsNullOrEmpty(trimText) && source.StartsWith(trimText, StringComparison.Ordinal))
		{
			return source.Substring(trimText.Length).TrimStart(trimText, StringComparison.Ordinal);
		}

		return source;
	}

	private static string TrimStart(this string source, string trimText, StringComparison comparisonType)
	{
		if (!string.IsNullOrEmpty(trimText) && source.StartsWith(trimText, comparisonType))
		{
			return source.Substring(trimText.Length).TrimStart(trimText, comparisonType);
		}
		else
		{
			return source;
		}
	}
}
