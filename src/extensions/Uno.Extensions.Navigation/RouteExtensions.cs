﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
#else
using Microsoft.UI.Xaml;
#endif

namespace Uno.Extensions.Navigation;

public static class RouteExtensions
{
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
        // TODO: Refactor to improve scheme trimming
        return BuildRoute(route.Uri.OriginalString.TrimStartOnce(schemeToTrim), route.Data);
    }

    public static Route AppendScheme(this Route route, string schemeToAppend)
    {
        // TODO: Refactor to improve scheme appending
        return BuildRoute(schemeToAppend + route.Uri.OriginalString, route.Data);
    }

    public static Route BuildRoute(this Uri uri, object data = null)
    {
        var path = uri.OriginalString;
        return path.BuildRoute(data);
    }

    public static Route BuildRoute(this string path, object data = null)
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
        var schemeRegex = new Regex(@"([^a-zA-Z0-9])+");
        var schemeMatch = schemeRegex.Match(path);

        var scheme = string.Empty;
        if (schemeMatch.Success)
        {
            path = path.TrimStart(schemeMatch.Value);
            scheme = schemeMatch.Value;
        }

        var segments = path.Split('/');
        var routeBase = segments.FirstOrDefault();
        if (routeBase is { Length: > 0 })
        {
            if (path.Length > routeBase.Length + 1)
            {
                path = path.TrimStartOnce(routeBase + "/");
            }
            else
            {
                path = string.Empty;
            }
        }

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

}
