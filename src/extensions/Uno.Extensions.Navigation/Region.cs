using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class Region : IRegionNavigationService
{
    public IRegionManager Manager { get; set; }

    private NavigationService navigation;
    public INavigationService Navigation => navigation;

    public IRegionNavigationService Parent { get; private set; }

    private IDictionary<string, IRegionNavigationService> NestedRegions { get; } = new Dictionary<string, IRegionNavigationService>();

    private ILogger Logger { get; }

    public Region(ILogger<Region> logger, IRegionNavigationService parent, NavigationService navigation)
    {
        Logger = logger;
        Parent = parent;
        this.navigation = navigation;
        navigation.Region = this;
    }

    public Task AddRegion(string regionName, IRegionNavigationService childRegion)
    {
        var childService = childRegion as Region;
        NestedRegions[regionName + string.Empty] = childService;

        if (navigation.PendingNavigation is not null)
        {
            return navigation.RunPendingNavigation();
        }
        else
        {
            return childService.navigation.RunPendingNavigation();
        }
    }

    public void RemoveRegion(IRegionNavigationService childRegion)
    {
        NestedRegions.Remove(kvp => kvp.Value == childRegion);
    }

    public IRegionNavigationService Nested(string regionName = null)
    {
        return NestedRegions.TryGetValue(regionName + string.Empty, out var service) ? service : null;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        PrintAllRegions(sb, this);
        return sb.ToString();
    }


    public async Task<(bool, NavigationRequest)> RunRegionNavigation(NavigationContext context)
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
            var residualRequest = request.WithPath(nextRoute);//.BuildNavigationContext(Services, new TaskCompletionSource<Options.Option>());
            return (true, residualRequest);
        }
        else if (Manager is null)
        {
            return (false, default);
        }
        else
        {
            //var context = request.BuildNavigationContext(Services, new TaskCompletionSource<Options.Option>());
            Logger.LazyLogDebug(() => $"Invoking region navigation");
            await Manager.NavigateAsync(context);
            Logger.LazyLogDebug(() => $"Region Navigation complete");
            if (context.ResidualRequest is not null &&
                !string.IsNullOrWhiteSpace(context.ResidualRequest.Route.Uri.OriginalString))
            {
                var residualRequest = context.ResidualRequest;//.BuildNavigationContext(Services, new TaskCompletionSource<Options.Option>());
                return (true, residualRequest);
            }
            return (true, default);
        }
    }


    private void PrintAllRegions(StringBuilder builder, Region nav, int indent = 0, string regionName = null)
    {
        if (nav.Manager is null)
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
            builder.AppendLine($"{prefix}{reg}{ans.Manager?.ToString()}");
        }

        foreach (var nested in nav.NestedRegions)
        {
            PrintAllRegions(builder, nested.Value as Region, indent + 1, nested.Key);
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
