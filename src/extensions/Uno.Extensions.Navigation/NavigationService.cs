using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class NavigationService : IRegionNavigationService
{
    public IRegion Region { get; set; }

    private IServiceProvider ScopedServices { get; }

    private ILogger Logger { get; }

    private bool IsRootService => Parent is null;

    private PendingContext PendingNavigation { get; set; }

    private IRegionNavigationService Parent { get; set; }

    private IDictionary<string, IRegionNavigationService> NestedServices { get; } = new Dictionary<string, IRegionNavigationService>();

    private int isNavigating = 0;

    public NavigationService(ILogger<NavigationService> logger, IServiceProvider services, IRegionNavigationService parent)
    {
        Logger = logger;
        ScopedServices = services;
        Parent = parent;
    }

    public Task Attach(string regionName, IRegionNavigationService childRegion)
    {
        var childService = childRegion as NavigationService;
        NestedServices[regionName + string.Empty] = childService;

        if (PendingNavigation is not null)
        {
            return RunPendingNavigation();
        }
        else
        {
            return childService.RunPendingNavigation();
        }
    }

    public void Detach(IRegionNavigationService childRegion)
    {
        NestedServices.Remove(kvp => kvp.Value == childRegion);
    }

    public NavigationResponse NavigateAsync(NavigationRequest request)
    {
        if (Interlocked.CompareExchange(ref isNavigating, 1, 0) == 1)
        {
            Logger.LazyLogWarning(() => $"Navigation already in progress. Unable to start navigation '{request.ToString()}'");
            return new NavigationResponse(request, Task.CompletedTask, null);
        }
        try
        {
            // Make sure that any navigation on the Root service is a nested request
            // (ie redirect to nested service by adding the ./ prefix to the Uri)
            if (IsRootService && !request.IsNestedRequest())
            {
                request = request.MakeNestedRequest();
            }

            if (request.IsParentRequest())
            {
                // Routing navigation request to parent
                return NavigateWithParentAsync(request);
            }

            // Create new context if there isn't a pending navigation
            if (PendingNavigation is null)
            {
                PendingNavigation = request.BuildNavigationContext(ScopedServices, new TaskCompletionSource<Options.Option>()).Pending();
            }

            var context = PendingNavigation.Context;

            Logger.LazyLogDebug(() => $"Invoking navigation with Navigation Context");
            var navTask = RunPendingNavigation();
            Logger.LazyLogDebug(() => $"Returning NavigationResponse");

            return new NavigationResponse(request, navTask, context.ResultCompletion.Task);
        }
        finally
        {
            Interlocked.Exchange(ref isNavigating, 0);
        }
    }

    private NavigationResponse NavigateWithParentAsync(NavigationRequest request)
    {
        Logger.LazyLogDebug(() => $"Redirecting navigation request to parent Navigation Service");

        var path = request.Route.Uri.OriginalString;
        var parentService = Parent;
        var parentPath = path.Length > NavigationConstants.RelativePath.ParentPath.Length ? path.Substring(NavigationConstants.RelativePath.ParentPath.Length) : string.Empty;

        var parentRequest = request.WithPath(parentPath);
        return parentService.NavigateAsync(parentRequest);
    }

    private async Task RunPendingNavigation()
    {
        try
        {
            var pending = PendingNavigation;
            if (pending is not null)
            {
                PendingNavigation = null;
                var navTask = pending.TaskCompletion;
                var navContext = pending.Context;
                var navRequest = navContext.Request;

                var residualRequest = navContext.ResidualRequest;
                if (navContext.IsForCurrentPath(Region))
                {
                    // Check for "./" prefix or the current path
                    // Returned request is trimmed to remove "./" or current path
                    residualRequest = navContext.TrimRequestForCurrentPath(Region);
                }
                else
                {
                    if (Region is null)
                    {
                        // If the there is no region, then
                        // push the context back to pending
                        // and await it
                        PendingNavigation = pending;
                        await navTask.Task;
                    }
                    else
                    {
                        await Region.NavigateAsync(navContext);
                    }
                }

                // At this point, any residual request needs to be handed
                // down to the appropriate nested service
                await RunNestedNavigation(residualRequest);

                navTask.TrySetResult(null);
            }
        }
        finally
        {
            Logger.LazyLogInformation(() => Root.ToString());
        }
    }

    private Task RunNestedNavigation(NavigationRequest nestedRequest)
    {
        if (nestedRequest is null)
        {
            return Task.CompletedTask;
        }

        var nestedRoute = nestedRequest.FirstRouteSegment;

        // Try to retrieve nested service based on route name
        var nested = Nested(nestedRoute) as NavigationService;
        if (nested is null)
        {
            // No match for named route, so grab any unnamed nested
            nested = Nested() as NavigationService;
        }
        else
        {
            // If we've been able to retrieve the nested service
            // we need to remove the route from the request path
            var nextRoute = nestedRequest.Route.Uri.OriginalString.TrimStart($"{nestedRoute}/");
            nestedRequest = nestedRequest.WithPath(nextRoute);
        }

        if (nested is not null)
        {
            // Send the navigation request to the nested service
            var nestedContext = nestedRequest.BuildNavigationContext(nested.ScopedServices, new TaskCompletionSource<Options.Option>());
            nested.PendingNavigation = nestedContext.Pending();
            return nested.RunPendingNavigation();
        }
        else
        {
            // Unable to retrieve the nested service, so put the
            // nested request into the pending context (we add
            // "./" to make sure it's handled as a nested navigation
            var pendingRoute = NavigationConstants.RelativePath.Nested + nestedRequest.Route.Uri.OriginalString;
            var pendingContext = nestedRequest.WithPath(pendingRoute).BuildNavigationContext(ScopedServices, new TaskCompletionSource<Options.Option>());

            PendingNavigation = pendingContext.Pending();
            return PendingNavigation.TaskCompletion.Task;
        }
    }

    private IRegionNavigationService Root
    {
        get
        {
            return (Parent as NavigationService)?.Root ?? this;
        }
    }

    private IRegionNavigationService Nested(string regionName = null)
    {
        return NestedServices.TryGetValue(regionName + string.Empty, out var service) ? service : null;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        PrintAllRegions(sb, this);
        return sb.ToString();
    }

    private void PrintAllRegions(StringBuilder builder, NavigationService nav, int indent = 0, string regionName = null)
    {
        if (nav.Region is null)
        {
            builder.AppendLine(string.Empty);
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

        foreach (var nested in nav.NestedServices)
        {
            PrintAllRegions(builder, nested.Value as NavigationService, indent + 1, nested.Key);
        }

        if (nav.Region is null)
        {
            builder.AppendLine("------------------------------------------------------------------------------------------------");
        }
    }
}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record PendingContext(TaskCompletionSource<object> TaskCompletion, NavigationContext Context)
{
}
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
