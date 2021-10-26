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

    public static bool IsFrameNavigation(this Route route) =>
        route.Scheme.StartsWith(Schemes.NavigateForward) ||
        route.Scheme.StartsWith(Schemes.NavigateBack);

    public static bool IsRoot(this Route route) => route.Scheme.StartsWith(Schemes.Root);

    public static bool IsParent(this Route route) => route.Scheme.StartsWith(Schemes.Parent);

    public static bool IsNested(this Route route) =>
        route.Scheme.StartsWith(Schemes.Nested) &&
        !string.IsNullOrWhiteSpace(route.Base);

    public static bool IsDialog(this Route route) => route.Scheme.StartsWith(Schemes.Dialog);

    public static bool IsLast(this Route route) => route.Path is not { Length: > 0 };

    public static bool IsEmpty(this Route route) => route.Base is not { Length: > 0 };

    // eg -/NextPage
    public static bool FrameIsRooted(this Route route) => route.Scheme.EndsWith(Schemes.Root + string.Empty);

    private static int NumberOfGoBackInScheme(this Route route) => route.Scheme.TakeWhile(x => x + string.Empty == Schemes.NavigateBack).Count();

    public static int FrameNumberOfPagesToRemove(this Route route) =>
        route.FrameIsRooted() ?
            0 :
            (route.FrameIsBackNavigation() ?
                route.NumberOfGoBackInScheme() - 1 :
                route.NumberOfGoBackInScheme());

    // Only navigate back if there is no base. If a base is specified, we do a forward navigate and remove items from the backstack
    public static bool FrameIsBackNavigation(this Route route) =>
        route.Scheme.StartsWith(Schemes.NavigateBack) && route.Base.Length == 0;

    public static bool FrameIsForwardNavigation(this Route route) => !route.FrameIsBackNavigation();

    public static Route[] ForwardNavigationSegments(this Route route)
    {
        if (route.IsEmpty())
        {
            return default;
        }

        var segments = new List<Route>() { route with { Scheme=Schemes.NavigateForward, Path = null } };
        var nextRoute = route.NextRoute();
        while (nextRoute.Scheme == Schemes.NavigateForward)
        {
            segments.Add(nextRoute with { Scheme = Schemes.NavigateForward, Path = null });
            nextRoute = nextRoute.NextRoute();
        }
        return segments.ToArray();
    }
    public static string[] ForwardNavigationSegments(this string path) => path.Split(Schemes.NavigateForward).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

    public static string Query(this Route route) => (route.Data?.Where(x => x.Key != string.Empty)?.Any() ?? false) ?
        "?" + string.Join("&", route.Data.Where(x => x.Key != string.Empty).Select(kvp => $"{kvp.Key}={kvp.Value}")) :
        null;

    public static object ResponseData(this Route route) =>
        route.Data.TryGetValue(string.Empty, out var result) ? result : null;

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

    public static Route Trim(this Route route, Route handledRoute)
    {
        while (route.Base == handledRoute.Base && !string.IsNullOrWhiteSpace(handledRoute.Base))
        {
            route = route.NextRoute();
            handledRoute = handledRoute.NextRoute();
        }

        return route;
    }

    public static Route Append(this Route route, Route routeToAppend)
    {
        return route with { Path = route.Path + routeToAppend.Scheme + routeToAppend.Base + routeToAppend.Path };
    }

    public static Route NextRoute(this Route route)
    {
        var routeBase = route.Path.ExtractBase(out var nextScheme, out var nextPath);
        if (nextScheme == Schemes.Root)
        {
            nextScheme = Schemes.Current;
        }
        return route with { Scheme = nextScheme, Base = routeBase, Path = nextPath };
    }

    public static string NextBase(this Route route)
    {
        return route.Path.ExtractBase(out var nextScheme, out var nextPath);
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

    private static string ExtractBase(this string path, out string nextScheme, out string nextPath)
    {
        nextPath = path;
        nextScheme = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
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
        return routeBase;
    }

    public static string WithScheme(this string path, string scheme) => string.IsNullOrWhiteSpace(scheme) ? path : $"{scheme}{path}";

    public static Route AsRoute(this Uri uri, object data = null)
    {
        var path = uri.OriginalString;
        return path.AsRoute(data);
    }

    public static Route AsRoute(this string path, object data = null)
    {
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

    public static IDictionary<string, object> Combine(this IDictionary<string, object> data, IDictionary<string, object> childData)
    {
        childData.ToArray().ForEach(x => data[x.Key] = x.Value);
        return data;
    }

    public static Route Merge(this Route route, IEnumerable<Route> childRoutes)
    {
        // TODO: Handle multiple children!
        var childRoute = childRoutes.FirstOrDefault();
        if (childRoute is null)
        {
            return route;
        }

        if (route is null)
        {
            return childRoute;
        }

        var separator = childRoute.Scheme == Schemes.Current ? Schemes.Separator : string.Empty;

        return route with
        {
            Path = route.Path + separator + childRoute.FullPath(),
            Data = route.Data.Combine(childRoute.Data)
        };
    }

    public static IDictionary<string, object> AsParameters(this IDictionary<string, object> data, RouteMap mapping)
    {
        var mapDict = data;
        if (mapping?.BuildQueryParameters is not null)
        {
            // TODO: Find nicer way to clone the dictionary
            mapDict = data.ToArray().ToDictionary(x => x.Key, x => x.Value);
            data.ForEach((KeyValuePair<string, object> kvp) =>
            {
                var qdict = mapping.BuildQueryParameters(kvp.Value);
                qdict.ForEach(qkvp => mapDict[qkvp.Key] = qkvp.Value);
            });
        }
        return mapDict;
    }
}
