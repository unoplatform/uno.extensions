using System.IO;
using System.Text.RegularExpressions;

namespace Uno.Extensions.Navigation;

public static class RouteExtensions
{
	private static Regex nonAlphaRegex = new Regex(@"([^a-zA-Z0-9])+");
	private static Regex alphaRegex = new Regex(@"([a-zA-Z0-9])+");

	public static bool IsBackOrCloseNavigation(this Route route) =>
		route.Qualifier
			.StartsWith(Qualifiers.NavigateBack);

	public static bool IsClearBackstack(this Route route) =>
		route.Qualifier
			.StartsWith(Qualifiers.ClearBackStack);
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

	public static string WithQualifier(this string path, string? qualifier) => (qualifier is null || string.IsNullOrWhiteSpace(qualifier)) ? path : $"{qualifier}{path}";

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
		if (data is not null)
		{
			if (data is IDictionary<string, object> paraDict)
			{
				foreach (var p in paraDict)
				{
					paras.Add(p);
				}
			}
			else
			{
				paras[string.Empty] = data;
			}
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
		if ((route.IsBackOrCloseNavigation() && !route.IsClearBackstack()) &&
			data is not null &&
			data is not IOption)
		{
			data = Option.Some<object>(data);
			paras[string.Empty] = data;
			route = route with { Data = paras };
		}
		return route;
	}

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

	public static bool IsAncestorRoute(this RouteInfo? rm, RouteInfo? route)
	{
		return (rm is not null && route is not null) &&
				(
					(rm?.DependsOn == route.Path) ||
					(rm?.DependsOnRoute.IsAncestorRoute(route)?? false) ||
					(rm?.Parent?.Path == route.Path) ||
					// This checks to see if there's a match for "route" anywhere in the ancestors of "rm"
					(rm?.Parent?.IsAncestorRoute(route) ?? false)
					//||
					//// This checks to see if there's a match for "route.Parent" anywhere in the ancestors of "rm"
					//rm.IsAncestorRoute(route.Parent)
				);
	}

	public static RouteInfo? SelectMapFromAncestor(this RouteInfo[] maps, RouteInfo? ancestorRoute)
	{
		if(ancestorRoute is null)
		{
			return default;
		}
		foreach (var map in maps)
		{
			if (map?.IsAncestorRoute(ancestorRoute) ?? false)
			{
				return map;
			}
		}
		return default;
	}
}
