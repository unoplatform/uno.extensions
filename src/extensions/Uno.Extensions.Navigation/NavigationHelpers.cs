using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation;

public static class NavigationHelpers
{
    public static PendingContext Pending(this NavigationContext context)
    {
        return new PendingContext(new TaskCompletionSource<object>(), context);
    }

    public static object ViewModel(this NavigationContext context)
    {
        var mapping = context.Mapping;
        if (mapping?.ViewModel is not null)
        {
            var services = context.Services;
            return services.GetService(mapping.ViewModel);
        }

        return null;
    }

    public static NavigationRequest WithPath(this NavigationRequest request, string path, string queryParameters = "")
    {
        return string.IsNullOrWhiteSpace(path) ? null : request with { Route = request.Route with { Uri = new Uri(path + (!string.IsNullOrWhiteSpace(queryParameters) ? $"?{queryParameters}" : string.Empty), UriKind.Relative) } };
    }

    public static NavigationContext BuildNavigationContext(this NavigationRequest request, IServiceProvider services, TaskCompletionSource<Options.Option> completion)
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

        var isRooted = path.StartsWith("/");

        var segments = path.Split('/');
        var numberOfPagesToRemove = 0;
        var navPath = string.Empty;
        var residualPath = path;
        var nextPath = string.Empty;
        for (int i = 0; i < segments.Length; i++)
        {
            var navSegment = segments[i];
            residualPath = residualPath.TrimStart(navSegment);
            if (residualPath.StartsWith("/"))
            {
                residualPath = residualPath.Substring(1);
            }
            nextPath = i < segments.Length - 1 ? segments[i + 1] : string.Empty;

            if (string.IsNullOrWhiteSpace(navSegment))
            {
                continue;
            }
            if (segments[i] == NavigationConstants.PreviousViewUri)
            {
                numberOfPagesToRemove++;
            }
            else
            {
                navPath = segments[i];
                break;
            }
        }

        if (navPath == string.Empty)
        {
            navPath = NavigationConstants.PreviousViewUri;
            numberOfPagesToRemove--;
        }

        var residualRequest = request.WithPath(residualPath, query);

        var scopedServices = services.CloneNavigationScopedServices();
        var dataFactor = scopedServices.GetService<ViewModelDataProvider>();
        dataFactor.Parameters = paras;

        var mapping = scopedServices.GetService<INavigationMappings>().LookupByPath(navPath);

        var context = new NavigationContext(
                            scopedServices,
                            request,
                            navPath,
                            isRooted,
                            numberOfPagesToRemove,
                            paras,
                            residualRequest,
                            (request.Cancellation is not null) ?
                                CancellationTokenSource.CreateLinkedTokenSource(request.Cancellation.Value) :
                                new CancellationTokenSource(),
                            completion,
                            mapping);
        return context;
    }

    private static IServiceProvider CloneNavigationScopedServices(this IServiceProvider services)
    {
        var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        scopedServices.GetService<RegionControlProvider>().RegionControl = services.GetService<RegionControlProvider>().RegionControl;
        scopedServices.GetService<ScopedServiceHost<IRegionService>>().Service = services.GetService<ScopedServiceHost<IRegionService>>().Service;
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
