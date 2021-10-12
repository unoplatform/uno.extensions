using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Controls;
using System.Text.RegularExpressions;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
#endif

namespace Uno.Extensions.Navigation;

public static class NavigationHelpers
{
    public static string NavigationPath(this object view, IRouteMappings mappings = null)
    {
        var map = mappings?.FindByView(view.GetType());
        if (map is not null)
        {
            return map.Path;
        }

        if (view is FrameworkElement fe)
        {
            var path = Navigation.Controls.Navigation.GetRoute(fe);
            if (string.IsNullOrWhiteSpace(path))
            {
                path = fe.Name;
            }

            return path;
        }

        return null;
    }

    //public static bool IsParentRequest(this NavigationRequest request)
    //{
    //    return request.Route.Uri.OriginalString.StartsWith(RouteConstants.RelativePath.ParentPath);
    //}

    //public static bool IsBackRequest(this NavigationRequest request)
    //{
    //    return request.Route.Uri.OriginalString.StartsWith(RouteConstants.RelativePath.BackPath);
    //}

    public static bool RequiresResponse(this NavigationRequest request)
    {
        return request.Result is not null;
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

    public static NavigationRequest WithPath(this NavigationRequest request, string path, string queryParameters = "")
    {
        return string.IsNullOrWhiteSpace(path) ? null : request with { Route = BuildRoute(new Uri(path + (!string.IsNullOrWhiteSpace(queryParameters) ? $"?{queryParameters}" : string.Empty), UriKind.Relative)) };
    }

    //public static RouteSegments Parse(this NavigationRequest request)
    //{
    //    var path = request.Route.Uri.OriginalString;

    //    var queryIdx = path.IndexOf('?');
    //    var query = string.Empty;
    //    if (queryIdx >= 0)
    //    {
    //        queryIdx++; // Step over the ?
    //        query = queryIdx < path.Length ? path.Substring(queryIdx) : string.Empty;
    //        path = path.Substring(0, queryIdx - 1);
    //    }

    //    var paras = ParseQueryParameters(query);
    //    if (request.Route.Data is not null)
    //    {
    //        if (request.Route.Data is IDictionary<string, object> paraDict)
    //        {
    //            paras.AddRange(paraDict);
    //        }
    //        else
    //        {
    //            paras[string.Empty] = request.Route.Data;
    //        }
    //    }

    //    var segments = path.Split('/');
    //    if (segments.Length <= 0)
    //    {
    //        return null;
    //    }

    //    var scheme = Schemes.All.Contains(segments.First()) ? segments.First() : null;
    //    if (scheme is null)
    //    {
    //        scheme = Schemes.Current;
    //    }
    //    else
    //    {
    //        segments = segments[1..];
    //    }

    //    return new RouteSegments(scheme, segments, paras);

    //    //var isRooted = path.StartsWith("/");

    //    //var segments = path.Split('/');
    //    //var numberOfPagesToRemove = 0;
    //    //var navPath = string.Empty;
    //    //var residualPath = path;
    //    //for (int i = 0; i < segments.Length; i++)
    //    //{
    //    //    var navSegment = segments[i];
    //    //    residualPath = residualPath.TrimStart(navSegment);
    //    //    if (residualPath.StartsWith("/"))
    //    //    {
    //    //        residualPath = residualPath.Substring(1);
    //    //    }

    //    //    if (string.IsNullOrWhiteSpace(navSegment))
    //    //    {
    //    //        continue;
    //    //    }
    //    //    if (segments[i] == RouteConstants.PreviousViewUri)
    //    //    {
    //    //        numberOfPagesToRemove++;
    //    //    }
    //    //    else
    //    //    {
    //    //        navPath = segments[i];
    //    //        break;
    //    //    }
    //    //}

    //    //if (navPath == string.Empty)
    //    //{
    //    //    navPath = RouteConstants.PreviousViewUri;
    //    //    numberOfPagesToRemove--;
    //    //}

    //    //var residualRequest = request.WithPath(residualPath, query);

    //    //var components = new RouteSegments(navPath, isRooted, numberOfPagesToRemove, paras, residualRequest);
    //    //return components;
    //}

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

    public static NavigationRequest AsRequest(this RouteMap map, object sender)
    {
        var request = new NavigationRequest(sender, BuildRoute(new Uri(map.Path, UriKind.Relative)));
        return request;
    }

    public static NavigationRequest AsRequest(this string uri, object sender, object data = null)
    {
        var request = new NavigationRequest(sender, BuildRoute(new Uri(uri, UriKind.Relative), data));
        return request;
    }

    public static NavigationContext BuildNavigationContext(this NavigationRequest request, IServiceProvider services)
    {
        var scopedServices = services.CloneNavigationScopedServices();
        var dataFactor = scopedServices.GetService<ViewModelDataProvider>();
        dataFactor.Parameters = request.Route.Data;

        var mapping = scopedServices.GetService<IRouteMappings>().FindByPath(request.Route.Base);

        var context = new NavigationContext(
                            scopedServices,
                            request,
                            (request.Cancellation is not null) ?
                                CancellationTokenSource.CreateLinkedTokenSource(request.Cancellation.Value) :
                                new CancellationTokenSource(),
                            mapping);
        return context;
    }

    public static IServiceProvider CloneNavigationScopedServices(this IServiceProvider services)
    {
        var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        scopedServices.GetService<RegionControlProvider>().RegionControl = services.GetService<RegionControlProvider>().RegionControl;
        scopedServices.AddInstance<IRegionNavigationService>(services.GetInstance<IRegionNavigationService>());
        var innerNavService = new InnerNavigationService(scopedServices.GetInstance<IRegionNavigationService>());
        scopedServices.AddInstance<INavigationService>(innerNavService);

        return scopedServices;
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

    public static IRegionFactory FindForControl(this IDictionary<Type, IRegionFactory> factories, object control)
    {
        var controlType = control.GetType();
        return factories.FindForControlType(controlType);
    }

    public static IRegionFactory FindForControlType(this IDictionary<Type, IRegionFactory> factories, Type controlType)
    {
        if (factories.TryGetValue(controlType, out var factory))
        {
            return factory;
        }

        var baseTypes = controlType.GetBaseTypes().ToArray();
        for (var i = 0; i < baseTypes.Length; i++)
        {
            if (factories.TryGetValue(baseTypes[i], out var baseFactory))
            {
                return baseFactory;
            }
        }

        return null;
    }

    public static void InjectServicesAndSetDataContext(this object view, IServiceProvider services, INavigationService navigation, object viewModel)
    {
        if (view is FrameworkElement fe)
        {
            fe.SetServiceProvider(services);

            if (viewModel is not null &&
                fe.DataContext != viewModel)
            {
                fe.DataContext = viewModel;
            }
        }

        if (view is IInjectable<INavigationService> navAware)
        {
            navAware.Inject(navigation);
        }

        if (view is IInjectable<IServiceProvider> spAware)
        {
            spAware.Inject(services);
        }
    }
}
