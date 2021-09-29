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
    public IRegion Manager { get; set; }

    private IServiceProvider ScopedServices { get; }

    private ILogger Logger { get; }

    private bool IsRootService => Parent is null;

    private PendingContext PendingNavigation { get; set; }

    private IRegionNavigationService Parent { get; set; }

    private IDictionary<string, IRegionNavigationService> NestedRegions { get; } = new Dictionary<string, IRegionNavigationService>();

    public NavigationService(ILogger<NavigationService> logger, IServiceProvider services, IRegionNavigationService parent)
    {
        Logger = logger;
        ScopedServices = services;
        Parent = parent;
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
            if (IsRootService && !path.StartsWith(NavigationConstants.RelativePath.Nested))
            {
                request = request.WithPath(NavigationConstants.RelativePath.Nested + path);
            }

            if (path.StartsWith(NavigationConstants.RelativePath.ParentPath))
            {
                // Routing navigation request to parent
                return NavigateWithParentAsync(request);
            }

            var context = request.BuildNavigationContext(ScopedServices, new TaskCompletionSource<Options.Option>());

            Logger.LazyLogDebug(() => $"Invoking navigation with Navigation Context");
            var navTask = NavigateInRegionAsync(context);
            Logger.LazyLogDebug(() => $"Returning NavigationResponse");

            return new NavigationResponse(request, navTask, context.ResultCompletion.Task);
        }
        finally
        {
            Interlocked.Exchange(ref isNavigating, 0);
        }
    }

    private async Task NavigateInRegionAsync(NavigationContext context)
    {
        try
        {
            //await Region.NavigateAsync(context);
            if (PendingNavigation is null)
            {
                PendingNavigation = context.Pending();
            }

            await RunPendingNavigation();
        }
        finally
        {
            Logger.LazyLogInformation(() => Root.ToString());
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

    public async Task RunPendingNavigation()
    {
        var pending = PendingNavigation;
        if (pending is not null)
        {
            PendingNavigation = null;
            var navTask = pending.TaskCompletion;
            var navContext = pending.Context;

            var navResult = await RunRegionNavigation(navContext);

            if (navResult.Item1)
            {
                if (navResult.Item2 is not null)
                {
                    var nestedRequest = navResult.Item2;
                    var nestedRoute = nestedRequest.FirstRouteSegment;

                    var nested = Nested(nestedRoute) as NavigationService;
                    if (nested is null)
                    {
                        nested = Nested() as NavigationService;
                    }
                    else
                    {
                        var nextRoute = nestedRequest.Route.Uri.OriginalString.TrimStart($"{nestedRoute}/");
                        nestedRequest = nestedRequest.WithPath(nextRoute);
                    }

                    if (nested is not null)
                    {
                        var nestedContext = nestedRequest.BuildNavigationContext(nested.ScopedServices, new TaskCompletionSource<Options.Option>());
                        nested.PendingNavigation = nestedContext.Pending();
                        await nested.RunPendingNavigation();
                    }
                    else
                    {
                        var pendingRoute = NavigationConstants.RelativePath.Nested + nestedRequest.Route.Uri.OriginalString;
                        var pendingContext = nestedRequest.WithPath(pendingRoute).BuildNavigationContext(ScopedServices, new TaskCompletionSource<Options.Option>());

                        PendingNavigation = pendingContext.Pending();
                        await PendingNavigation.TaskCompletion.Task;
                    }
                }

                navTask.TrySetResult(null);
            }
            else
            {
                PendingNavigation = pending;
                await navTask.Task;
            }
        }
    }

    private IRegionNavigationService Root
    {
        get
        {
            return (Parent as NavigationService)?.Root ?? this;
        }
    }

    public Task AddRegion(string regionName, IRegionNavigationService childRegion)
    {
        var childService = childRegion as NavigationService;
        NestedRegions[regionName + string.Empty] = childService;

        if (PendingNavigation is not null)
        {
            return RunPendingNavigation();
        }
        else
        {
            return childService.RunPendingNavigation();
        }
    }

    public void RemoveRegion(IRegionNavigationService childRegion)
    {
        NestedRegions.Remove(kvp => kvp.Value == childRegion);
    }

    private IRegionNavigationService Nested(string regionName = null)
    {
        return NestedRegions.TryGetValue(regionName + string.Empty, out var service) ? service : null;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        PrintAllRegions(sb, this);
        return sb.ToString();
    }

    private async Task<(bool, NavigationRequest)> RunRegionNavigation(NavigationContext context)
    {
        var request = context.Request;
        var firstRoute = request.FirstRouteSegment;

        if (firstRoute == Manager?.CurrentContext?.Path ||
            (firstRoute + "/") == NavigationConstants.RelativePath.Nested)
        {
            Logger.LazyLogWarning(() => $"Attempt to log to the same path '{firstRoute}");
            if (context.Path == Manager?.CurrentContext?.Path)
            {
                return (true, null);
            }
            var nextRoute = request.Route.Uri.OriginalString.TrimStart($"{firstRoute}/");
            var residualRequest = request.WithPath(nextRoute);
            return (true, residualRequest);
        }
        else if (Manager is null)
        {
            return (false, default);
        }
        else
        {
            Logger.LazyLogDebug(() => $"Invoking region navigation");
            await Manager.NavigateAsync(context);
            Logger.LazyLogDebug(() => $"Region Navigation complete");
            if (context.ResidualRequest is not null &&
                !string.IsNullOrWhiteSpace(context.ResidualRequest.Route.Uri.OriginalString))
            {
                var residualRequest = context.ResidualRequest;
                return (true, residualRequest);
            }
            return (true, default);
        }
    }

    private void PrintAllRegions(StringBuilder builder, NavigationService nav, int indent = 0, string regionName = null)
    {
        if (nav.Manager is null)
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
            builder.AppendLine($"{prefix}{reg}{ans.Manager?.ToString()}");
        }

        foreach (var nested in nav.NestedRegions)
        {
            PrintAllRegions(builder, nested.Value as NavigationService, indent + 1, nested.Key);
        }

        if (nav.Manager is null)
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
