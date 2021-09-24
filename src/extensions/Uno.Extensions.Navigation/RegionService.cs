using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class RegionService : IRegionServiceContainer, IRegionService
{
    private IRegionManager Region { get; }

    private (TaskCompletionSource<object>, NavigationRequest)? PendingNavigation { get; set; }

    private IDictionary<string, RegionService> NestedRegions { get; } = new Dictionary<string, RegionService>();

    private ILogger Logger { get; }

    private IServiceProvider Services { get; }

    public RegionService(ILogger<RegionService> logger, IServiceProvider services, IRegionManager region)
    {
        Logger = logger;
        Services = services;
        Region = region;
    }

    public Task AddRegion(string regionName, IRegionServiceContainer childRegion)
    {
        NestedRegions[regionName + string.Empty] = childRegion as RegionService;

        return RunPendingNavigation();
    }

    public void RemoveRegion(IRegionServiceContainer childRegion)
    {
        NestedRegions.Remove(kvp => kvp.Value == childRegion);
    }


    public async Task NavigateAsync(NavigationContext context)
    {
        if (Region is null)
        {
            if (PendingNavigation is null)
            {
                PendingNavigation = (new TaskCompletionSource<object>(), context.Request);
            }

            await PendingNavigation.Value.Item1.Task;
            return;
        }

        if (context.Path == Region?.CurrentContext?.Path ||
            (context.Path + "/") == NavigationConstants.RelativePath.Nested)
        {
            Logger.LazyLogWarning(() => $"Attempt to log to the same path '{context.Path}");
        }
        else
        {
            Logger.LazyLogDebug(() => $"Invoking region navigation");
            await Region.NavigateAsync(context);
            Logger.LazyLogDebug(() => $"Region Navigation complete");
        }

        if (context.ResidualRequest is not null &&
            !string.IsNullOrWhiteSpace(context.ResidualRequest.Route.Uri.OriginalString))
        {
            PendingNavigation = (new TaskCompletionSource<object>(), context.ResidualRequest);
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

    private async Task RunPendingNavigation()
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
                await nested.NavigateAsync(nestedRequest.BuildNavigationContext(nested.Services, new TaskCompletionSource<Options.Option>()));
            }
            else
            {
                await nextNavigationTask.Task;
            }

            nextNavigationTask.TrySetResult(null);
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
