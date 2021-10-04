using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Regions;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
#endif

namespace Uno.Extensions.Navigation;

public static class NavigationHelpers
{
    public static PendingRequest Pending(this NavigationRequest request, TaskCompletionSource<Options.Option> resultCompletion = default)
    {
        return new PendingRequest(request, new TaskCompletionSource<object>(), resultCompletion ?? new TaskCompletionSource<Options.Option>());
    }

    public static string NavigationPath(this object view, IRouteMappings mappings = null)
    {
        var map = mappings?.LookupByView(view.GetType());
        if (map is not null)
        {
            return map.Path;
        }

        if (view is FrameworkElement fe)
        {
            var path = Navigation.Controls.Navigation.GetPath(fe);
            if (string.IsNullOrWhiteSpace(path))
            {
                path = fe.Name;
            }

            return path;
        }

        return null;
    }

    public static bool IsParentRequest(this NavigationRequest request)
    {
        return request.Route.Uri.OriginalString.StartsWith(RouteConstants.RelativePath.ParentPath);
    }

    public static bool IsBackRequest(this NavigationRequest request)
    {
        return request.Route.Uri.OriginalString.StartsWith(RouteConstants.RelativePath.BackPath);
    }

    public static bool RequiresResponse(this NavigationRequest request)
    {
        return request.Result is not null;
    }

    public static bool IsNestedRequest(this NavigationRequest request)
    {
        return request.Route.Uri.OriginalString.StartsWith(RouteConstants.RelativePath.Nested);
    }

    public static NavigationRequest MakeNestedRequest(this NavigationRequest request)
    {
        return request.WithPath(RouteConstants.RelativePath.Nested + request.Route.Uri.OriginalString);
    }

    public static NavigationRequest WithPath(this NavigationRequest request, string path, string queryParameters = "")
    {
        return string.IsNullOrWhiteSpace(path) ? null : request with { Route = request.Route with { Uri = new Uri(path + (!string.IsNullOrWhiteSpace(queryParameters) ? $"?{queryParameters}" : string.Empty), UriKind.Relative) } };
    }

    public static RouteSegments Parse(this NavigationRequest request)
    {
        var path = request.Route.Uri.OriginalString;

        var queryIdx = path.IndexOf('?');
        var query = string.Empty;
        if (queryIdx >= 0)
        {
            queryIdx++; // Step over the ?
            query = queryIdx < path.Length ? path.Substring(queryIdx) : string.Empty;
            path = path.Substring(0, queryIdx - 1);
        }

        var paras = ParseQueryParameters(query);
        if (request.Route.Data is not null)
        {
            if (request.Route.Data is IDictionary<string, object> paraDict)
            {
                paras.AddRange(paraDict);
            }
            else
            {
                paras[string.Empty] = request.Route.Data;
            }
        }

        var segments = path.Split('/');
        return new RouteSegments(segments, paras);

        //var isRooted = path.StartsWith("/");

        //var segments = path.Split('/');
        //var numberOfPagesToRemove = 0;
        //var navPath = string.Empty;
        //var residualPath = path;
        //for (int i = 0; i < segments.Length; i++)
        //{
        //    var navSegment = segments[i];
        //    residualPath = residualPath.TrimStart(navSegment);
        //    if (residualPath.StartsWith("/"))
        //    {
        //        residualPath = residualPath.Substring(1);
        //    }

        //    if (string.IsNullOrWhiteSpace(navSegment))
        //    {
        //        continue;
        //    }
        //    if (segments[i] == RouteConstants.PreviousViewUri)
        //    {
        //        numberOfPagesToRemove++;
        //    }
        //    else
        //    {
        //        navPath = segments[i];
        //        break;
        //    }
        //}

        //if (navPath == string.Empty)
        //{
        //    navPath = RouteConstants.PreviousViewUri;
        //    numberOfPagesToRemove--;
        //}

        //var residualRequest = request.WithPath(residualPath, query);

        //var components = new RouteSegments(navPath, isRooted, numberOfPagesToRemove, paras, residualRequest);
        //return components;
    }

    public static NavigationRequest AsRequest(this RouteMap map, object sender)
    {
        var request = new NavigationRequest(sender, new Route(new Uri(map.Path, UriKind.Relative)));
        return request;
    }

    public static NavigationRequest AsRequest(this string uri, object sender, object data = null)
    {
        var request = new NavigationRequest(sender, new Route(new Uri(uri, UriKind.Relative), data));
        return request;
    }

    public static NavigationContext BuildNavigationContext(this NavigationRequest request, IServiceProvider services)
    {
        var components = request.Parse();

        var scopedServices = services.CloneNavigationScopedServices();
        var dataFactor = scopedServices.GetService<ViewModelDataProvider>();
        dataFactor.Parameters = components.Parameters;

        var mapping = scopedServices.GetService<IRouteMappings>().LookupByPath(components.NavigationPath);

        var context = new NavigationContext(
                            scopedServices,
                            request,
                            components,
                            (request.Cancellation is not null) ?
                                CancellationTokenSource.CreateLinkedTokenSource(request.Cancellation.Value) :
                                new CancellationTokenSource(),
                            mapping);
        return context;
    }



    private static IServiceProvider CloneNavigationScopedServices(this IServiceProvider services)
    {
        var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        scopedServices.GetService<RegionControlProvider>().RegionControl = services.GetService<RegionControlProvider>().RegionControl;
        scopedServices.GetService<ScopedServiceHost<IRegionNavigationService>>().Service = services.GetService<ScopedServiceHost<IRegionNavigationService>>().Service;
        scopedServices.GetService<ScopedServiceHost<INavigationService>>().Service = services.GetService<ScopedServiceHost<INavigationService>>().Service;

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
}
