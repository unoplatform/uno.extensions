using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class RegionService : IRegionService
{
    public IRegionManager Region { get; set; }

    private PendingContext PendingNavigation { get; set; }

    private IDictionary<string, RegionService> NestedRegions { get; } = new Dictionary<string, RegionService>();

    private ILogger Logger { get; }

    private IServiceProvider Services { get; }

    public RegionService(ILogger<RegionService> logger, IServiceProvider services)
    {
        Logger = logger;
        Services = services;
    }

    public Task AddRegion(string regionName, IRegionService childRegion)
    {
        var childService = childRegion as RegionService;
        NestedRegions[regionName + string.Empty] = childService;

        if (this.PendingNavigation is not null)
        {
            return RunPendingNavigation();
        }
        else
        {
            return childService.RunPendingNavigation();
        }
    }

    public void RemoveRegion(IRegionService childRegion)
    {
        NestedRegions.Remove(kvp => kvp.Value == childRegion);
    }

    public async Task NavigateAsync(NavigationContext context)
    {
        if (PendingNavigation is null)
        {
            PendingNavigation = context.Pending();
        }

        await RunPendingNavigation();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        PrintAllRegions(sb, this);
        return sb.ToString();
    }

    private RegionService Nested(string regionName = null)
    {
        return NestedRegions.TryGetValue(regionName + string.Empty, out var service) ? service : null;
    }

    private async Task<(bool, NavigationContext)> RunRegionNavigation(NavigationContext context)
    {
        var request = context.Request;
        var firstRoute = request.FirstRouteSegment;

        if (firstRoute == Region?.CurrentContext?.Path ||
            (firstRoute + "/") == NavigationConstants.RelativePath.Nested)
        {
            Logger.LazyLogWarning(() => $"Attempt to log to the same path '{firstRoute}");
            if (context.Path == Region?.CurrentContext?.Path)
            {
                return (true, null);
            }
            var nextRoute = request.Route.Uri.OriginalString.TrimStart($"{firstRoute}/");
            var residualRequest = request.WithPath(nextRoute).BuildNavigationContext(Services, new TaskCompletionSource<Options.Option>());
            return (true, residualRequest);
        }
        else if (Region is null)
        {
            return (false, default);
        }
        else
        {
            //var context = request.BuildNavigationContext(Services, new TaskCompletionSource<Options.Option>());
            Logger.LazyLogDebug(() => $"Invoking region navigation");
            await Region.NavigateAsync(context);
            Logger.LazyLogDebug(() => $"Region Navigation complete");
            if (context.ResidualRequest is not null &&
                !string.IsNullOrWhiteSpace(context.ResidualRequest.Route.Uri.OriginalString))
            {
                var residualRequest = context.ResidualRequest.BuildNavigationContext(Services, new TaskCompletionSource<Options.Option>());
                return (true, residualRequest);
            }
            return (true, default);
        }
    }

    private async Task RunPendingNavigation()
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
                    var nestedContext = navResult.Item2;
                    var nestedRequest = nestedContext.Request;
                    var nestedRoute = nestedRequest.FirstRouteSegment;

                    var nested = Nested(nestedRoute);
                    if (nested is null)
                    {
                        nested = Nested();
                    }
                    else
                    {
                        var nextRoute = nestedRequest.Route.Uri.OriginalString.TrimStart($"{nestedRoute}/");
                        nestedContext = nestedRequest.WithPath(nextRoute).BuildNavigationContext(nested.Services, new TaskCompletionSource<Options.Option>());
                    }

                    if (nested is not null)
                    {
                        nested.PendingNavigation = nestedContext.Pending();
                        await nested.RunPendingNavigation();
                    }
                    else
                    {
                        var pendingRoute = NavigationConstants.RelativePath.Nested + nestedRequest.Route.Uri.OriginalString;
                        var pendingRequest = nestedRequest.WithPath(pendingRoute);
                        var pendingContext = nestedContext with { Request = pendingRequest };
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

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record PendingContext(TaskCompletionSource<object> TaskCompletion, NavigationContext Context)
{
}
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
