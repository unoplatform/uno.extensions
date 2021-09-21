using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class NavigationService : INavigationService
{
    public INavigationManager Navigation { get; }

    public INavigationMapping Mapping { get; }

    public IRegionManager Region { get; set; }

    public INavigationService Parent { get; }

    public (TaskCompletionSource<object>, NavigationRequest)? PendingNavigation { get; set; }

    private IServiceProvider ScopedServices { get; }

    private ILogger Logger { get; }

    public NavigationService(ILogger<NavigationService> logger, INavigationManager manager, IServiceProvider services, INavigationMapping mapping, INavigationService parent)
    {
        Logger = logger;
        Navigation = manager;
        ScopedServices = services;

        // Prevent recursion when this is the root nav service
        if (parent is not null)
        {
            var navWrapper = ScopedServices.GetService<NavigationServiceProvider>();
            navWrapper.Navigation = this;
            Parent = parent;
        }

        Mapping = mapping;
    }

    public IDictionary<string, INavigationService> NestedRegions { get; } = new Dictionary<string, INavigationService>();

    public INavigationService Nested(string regionName = null)
    {
        return NestedRegions.TryGetValue(regionName + string.Empty, out var service) ? service : null;
    }

    private int isNavigating = 0;

    public NavigationResponse NavigateAsync(NavigationRequest request)
    {
        if (Interlocked.CompareExchange(ref isNavigating, 1, 0) == 1)
        {
            return new NavigationResponse(request, Task.CompletedTask, null);
        }
        try
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

            if (path.StartsWith("//"))
            {
                var parentService = Parent as NavigationService;
                path = path.Length > 2 ? path.Substring(2) : string.Empty;

                var parentRequest = request.WithPath(path, query);
                return parentService.NavigateAsync(parentRequest);
            }

            if (path.StartsWith("./"))
            {
                path = path.Length > 2 ? path.Substring(2) : string.Empty;
                var nestedRequest = request.WithPath(path, query);
                var nestedRoute = nestedRequest.FirstRouteSegment;

                var nested = Nested(nestedRoute) as NavigationService;
                if (nested is null)
                {
                    nested = Nested() as NavigationService;
                }
                else
                {
                    path = path.TrimStart($"{nestedRoute}/");
                    nestedRequest = request.WithPath(path, query);
                }

                if (nested is null)
                {
                    // This should only be true for the first navigation in the app
                    // which may occur before the first container is created
                    PendingNavigation = (new TaskCompletionSource<object>(), nestedRequest);
                    return new NavigationResponse(request, PendingNavigation.Value.Item1.Task, null);
                }

                return nested.NavigateAsync(nestedRequest);
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

            var residualRequest = request.WithPath(residualPath, query); // with { Route = request.Route with { Path = new Uri(residualPath, UriKind.Relative) } };
            if (Region is null)
            {
                var pending = (new TaskCompletionSource<object>(), request);
                if (Parent is not null)
                {
                    (Parent as NavigationService).PendingNavigation = pending;
                }
                else
                {
                    // This should only be true for the first navigation in the app
                    // which may occur before the first container is created
                    PendingNavigation = pending;
                }
                return new NavigationResponse(request, pending.Item1.Task, null);
            }

            if (!string.IsNullOrWhiteSpace(residualPath))
            {
                PendingNavigation = (new TaskCompletionSource<object>(), residualRequest);
            }

            var scope = ScopedServices.CreateScope();
            var services = scope.ServiceProvider;
            var dataFactor = services.GetService<ViewModelDataProvider>();
            dataFactor.Parameters = paras;
            var navWrapper = services.GetService<NavigationServiceProvider>();
            navWrapper.Navigation = this;

            var mapping = Mapping.LookupByPath(navPath);

            var context = new NavigationContext(
                                services,
                                request,
                                navPath,
                                isRooted,
                                numberOfPagesToRemove,
                                paras,
                                (request.Cancellation is not null) ?
                                    CancellationTokenSource.CreateLinkedTokenSource(request.Cancellation.Value) :
                                    new CancellationTokenSource(),
                                new TaskCompletionSource<Options.Option>(),
                                Mapping: mapping);
            var navTask = RegionNavigateAsync(context);

            return new NavigationResponse(request, navTask, context.ResultCompletion.Task);
        }
        finally
        {
            Interlocked.Exchange(ref isNavigating, 0);
        }
    }

    private async Task RegionNavigateAsync(NavigationContext context)
    {
        try
        {
            await Region.NavigateAsync(context);

            var pending = PendingNavigation;
            if (pending is not null && context.Request.Sender is not null)
            {
                var nextNavigationTask = pending.Value.Item1;
                var nextNavigation = pending.Value.Item2;
                var nextPath = nextNavigation.FirstRouteSegment;

                // navPath is the current path on the region
                // Need to look at nested services to see if we
                // need to pick on, before passing down the residual
                // navigation request
                var nested = Nested(nextPath) as NavigationService;

                if (nested == null && NestedRegions.Any())
                {
                    nested = NestedRegions.Values.First() as NavigationService;
                }
                else
                {
                    var residualPath = nextNavigation.Route.Uri.OriginalString;
                    residualPath = residualPath.TrimStart($"{nextPath}/");
                    nextNavigation = nextNavigation.WithPath(residualPath, string.Empty);
                }

                if (nested is not null)
                {
                    PendingNavigation = null;
                    await nested.NavigateAsync(nextNavigation);
                    nextNavigationTask.SetResult(null);
                }
                else
                {
                    await nextNavigationTask.Task;
                }
            }
        }
        finally
        {
            Logger.LazyLogInformation(()=> Navigation.ToString());
        }
    }

    private IDictionary<string, object> ParseQueryParameters(string queryString)
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
