using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class RegionService : IRegionServiceContainer
{
    public IRegionManager Region { get; set; }

    private (TaskCompletionSource<object>, NavigationRequest)? PendingNavigation { get; set; }

    public IDictionary<string, IRegionServiceContainer> NestedRegions { get; } = new Dictionary<string, IRegionServiceContainer>();

    private ILogger Logger { get; }

    private IServiceProvider Services { get; }

    public RegionService(ILogger<RegionService> logger, IServiceProvider services, IRegionManager region)
    {
        Logger = logger;
        Services = services;
        Region = region;
    }

    public async Task NavigateAsync(NavigationContext context)
    {
        if (context.ResidualRequest is not null && !string.IsNullOrWhiteSpace(context.ResidualRequest.Route.Uri.OriginalString))
        {
            PendingNavigation = (new TaskCompletionSource<object>(), context.ResidualRequest);
        }


        if (Region?.CurrentContext?.Path == context.Path)
        {
            Logger.LazyLogWarning(() => $"Attempt to log to the same path '{context.Path}");
        }
        else if (Region is not null)
        {
            Logger.LazyLogDebug(() => $"Invoking region navigation");
            await Region.NavigateAsync(context);
            Logger.LazyLogDebug(() => $"Region Navigation complete");
        }
        else
        {
            PendingNavigation = (new TaskCompletionSource<object>(), context.Request);
            return;
        }

        await RunPendingNavigation();
        //var pending = PendingNavigation;
        //if (pending is not null && context.Request.Sender is not null)
        //{
        //    Logger.LazyLogDebug(() => $"Handling pending navigation");

        //    var nextNavigationTask = pending.Value.Item1;
        //    var nextNavigation = pending.Value.Item2;
        //    var nextPath = nextNavigation.FirstRouteSegment;

        //    // navPath is the current path on the region
        //    // Need to look at nested services to see if we
        //    // need to pick on, before passing down the residual
        //    // navigation request
        //    var nested = Nested(nextPath) as NavigationService;

        //    if (nested == null && NestedRegions.Any())
        //    {
        //        nested = NestedRegions.Values.First() as NavigationService;
        //    }
        //    else
        //    {
        //        var residualPath = nextNavigation.Route.Uri.OriginalString;
        //        residualPath = residualPath.TrimStart($"{nextPath}/");
        //        nextNavigation = nextNavigation.WithPath(residualPath, string.Empty);
        //    }

        //    if (nested is not null)
        //    {
        //        PendingNavigation = null;
        //        Logger.LazyLogDebug(() => $"Invoking pending navigation");
        //        await nested.NavigateAsync(nextNavigation);
        //        Logger.LazyLogDebug(() => $"Pending navigation complete");
        //        nextNavigationTask.SetResult(null);
        //    }
        //    else
        //    {
        //        Logger.LazyLogDebug(() => $"Unable to invoke pending navigation, so waiting for task to complete");
        //        await nextNavigationTask.Task;
        //        Logger.LazyLogDebug(() => $"Pending navigation task completed");
        //    }
        //}

    }


    public Task QueuePendingRequest(NavigationRequest request)
    {
        PendingNavigation = (new TaskCompletionSource<object>(), request);
        return PendingNavigation.Value.Item1.Task;
    }

    public IRegionServiceContainer Nested(string regionName = null)
    {
        return NestedRegions.TryGetValue(regionName + string.Empty, out var service) ? service : null;
    }

    public Task AddRegion(string regionName, IRegionServiceContainer childRegion)
    {
        NestedRegions[regionName + string.Empty] = childRegion;

        return RunPendingNavigation();
    }

    public Task RunPendingNavigation()
    {
        var pending = PendingNavigation;
        if (pending is not null)
        {
            var nextNavigationTask = pending.Value.Item1;
            var nestedRequest = pending.Value.Item2;

            var nestedRoute = nestedRequest.FirstRouteSegment;

            var nested = Nested(nestedRoute) as RegionService;
            if (nested is null)
            {
                nested = Nested() as RegionService;
            }
            else
            {
                var nextRoute = nestedRequest.Route.Uri.OriginalString.TrimStart($"{nestedRoute}/");
                nestedRequest = nestedRequest with { Route = nestedRequest.Route with { Uri = new Uri(nextRoute, UriKind.Relative) } };
            }

            if (nested is not null)
            {
                PendingNavigation = null;
            }
            else
            {
                return nextNavigationTask.Task;
            }

            return nested.Region.NavigateAsync(nestedRequest.BuildNavigationContext(nested.Services));
        }

        return Task.CompletedTask;
    }

    public void RemoveRegion(IRegionServiceContainer childRegion)
    {
        NestedRegions.Remove(kvp => kvp.Value == childRegion);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        PrintAllRegions(sb, this);
        return sb.ToString();
    }

    private void PrintAllRegions(StringBuilder builder, RegionService nav, int indent = 0, string regionName = null)
    {
        if (nav.Region is null)
        {
            builder.AppendLine("");
            builder.AppendLine("------------------------------------------------------------------------------------------------");
            builder.AppendLine($"ROOT");
        }
        else
        {
            var ans = nav;
            var prefix = string.Empty;
            if (indent > 0)
            {
                prefix = new string(' ', indent * 2) + "|-";
            }
            var reg = !string.IsNullOrWhiteSpace(regionName) ? $"({regionName}) " : null;
            builder.AppendLine($"{prefix}{reg}{ans.Region?.ToString()}");
        }

        foreach (var nested in nav.NestedRegions)
        {
            PrintAllRegions(builder, nested.Value as RegionService, indent + 1, nested.Key);
        }

        if (nav.Region is null)
        {
            builder.AppendLine("------------------------------------------------------------------------------------------------");
        }
    }
}

public class NavigationService : INavigationRegionService
{
    public INavigationService Parent { get; set; }

    public IRegionServiceContainer Region { get; set; }

    private INavigationMappings Mapping { get; }

    private IServiceProvider ScopedServices { get; }

    private ILogger Logger { get; }

    public NavigationService(ILogger<NavigationService> logger, IServiceProvider services, INavigationMappings mapping)//, IRegionServiceContainer region)
    {
        Logger = logger;
        ScopedServices = services;

        Mapping = mapping;
        //Region = region;
    }

    private int isNavigating = 0;

    public NavigationResponse NavigateAsync(NavigationRequest request)
    {
        if (Interlocked.CompareExchange(ref isNavigating, 1, 0) == 1)
        {
            Logger.LazyLogWarning(() => $"Navigation already in progress. Unable to start navigation '{request.ToString()}'");
            return new NavigationResponse(request, Task.CompletedTask, null);
        }
        try
        {
            var path = request.Route.Uri.OriginalString;
            if (path.StartsWith(NavigationConstants.RelativePath.ParentPath))
            {
                // Routing navigation request to parent
                return NavigateWithParentAsync(request);
            }

            if (path.StartsWith(NavigationConstants.RelativePath.Nested))
            {
                // Routing navigation request to nested
                return NavigateWithNestedAsync(request);
            }

            var context = request.BuildNavigationContext(ScopedServices);


            //if (Region is null)
            //{
            //    var pending = (new TaskCompletionSource<object>(), request);
            //    if (Parent is not null)
            //    {
            //        if ((Parent as NavigationService).PendingNavigation is null)
            //        {
            //            Logger.LazyLogDebug(() => $"Region hasn't been set, and Parent exists, so setting the navigation request as a pending navigation on parent");
            //            (Parent as NavigationService).PendingNavigation = pending;
            //        }
            //    }
            //    else
            //    {
            //        Logger.LazyLogDebug(() => $"Parent is null, which means this is the root navigation service. Set the pending navigation");
            //        // This should only be true for the first navigation in the app
            //        // which may occur before the first container is created
            //        PendingNavigation = pending;
            //    }
            //    return new NavigationResponse(request, pending.Item1.Task, null);
            //}




            Logger.LazyLogDebug(() => $"Invoking navigation with Navigation Context");
            var navTask = Region.NavigateAsync(context);
            Logger.LazyLogDebug(() => $"Returning NavigationResponse");

            return new NavigationResponse(request, navTask, context.ResultCompletion.Task);
        }
        finally
        {
            Logger.LazyLogInformation(() => Root.ToString());
            Interlocked.Exchange(ref isNavigating, 0);
        }
    }

    private NavigationResponse NavigateWithNestedAsync(NavigationRequest request)
    {
        var path = request.Route.Uri.OriginalString;
        Logger.LazyLogDebug(() => $"Redirecting navigation request to nested Navigation Service");
        var nestedPath = path.Length > 2 ? path.Substring(2) : string.Empty;
        var nestedRequest = request.WithPath(nestedPath);

        // This should only be true for the first navigation in the app
        // which may occur before the first container is created
        Region.QueuePendingRequest(nestedRequest);

        var pendingTask = Region.RunPendingNavigation();

        return new NavigationResponse(request, pendingTask, null);
    }

    private NavigationResponse NavigateWithParentAsync(NavigationRequest request)
    {
        var path = request.Route.Uri.OriginalString;
        Logger.LazyLogDebug(() => $"Redirecting navigation request to parent Navigation Service");
        var parentService = Parent as NavigationService;
        var parentPath = path.Length > 2 ? path.Substring(2) : string.Empty;

        var parentRequest = request.WithPath(parentPath);
        return parentService.NavigateAsync(parentRequest);
    }



    private NavigationService Root
    {
        get
        {
            return (Parent as NavigationService)?.Root ?? this;
        }
    }




}
