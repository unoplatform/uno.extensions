using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class NavigationService : INavigationService
{
    public ILogger Logger { get; }

    protected bool IsRootService => Parent is null;

    private IRegionNavigationService Parent { get; set; }

    protected IDialogFactory DialogFactory { get; }

    public NavigationService(ILogger logger, IRegionNavigationService parent, IDialogFactory dialogFactory)
    {
        Logger = logger;
        Parent = parent;
        DialogFactory = dialogFactory;
    }

    public virtual async Task<NavigationResponse> NavigateAsync(NavigationRequest request)
    {
        if (request is not null && request.Segments.IsParent)
        {
            // Routing navigation request to parent
            return await NavigateWithParentAsync(request);
        }

        return null;
    }

    private Task<NavigationResponse> NavigateWithParentAsync(NavigationRequest request)
    {
        Logger.LazyLogDebug(() => $"Redirecting navigation request to parent Navigation Service");

        var path = request.Route.Uri.OriginalString;
        var parentService = Parent;
        var parentPath = path.Length > (RouteConstants.Schemes.Parent+"/").Length ? path.Substring((RouteConstants.Schemes.Parent + "/").Length) : string.Empty;

        var parentRequest = request.WithPath(parentPath);
        return parentService.NavigateAsync(parentRequest);
    }

    protected NavigationService Root
    {
        get
        {
            return (Parent as NavigationService)?.Root ?? this;
        }
    }

}

public class CompositeNavigationService : NavigationService, IRegionNavigationService
{
    protected IDictionary<string, IRegionNavigationService> NestedServices { get; } = new Dictionary<string, IRegionNavigationService>();

    private AsyncAutoResetEvent NestedServiceWaiter { get; } = new AsyncAutoResetEvent(false);

    public CompositeNavigationService(ILogger logger, IRegionNavigationService parent, IDialogFactory dialogFactory) : base(logger, parent, dialogFactory)
    {
    }

    public void Attach(string regionName, IRegionNavigationService childRegion)
    {
        var childService = childRegion as RegionNavigationService;
        NestedServices[regionName + string.Empty] = childService;
        NestedServiceWaiter.Set();
    }

    public void Detach(IRegionNavigationService childRegion)
    {
        NestedServices.Remove(kvp => kvp.Value == childRegion);
    }

    public async override Task<NavigationResponse> NavigateAsync(NavigationRequest request)
    {

        // At this point, any residual request needs to be handed
        // down to the appropriate nested service
        var nestedNavigationResponse = await RunNestedNavigation(request);

        var baseNavigationResponse = await base.NavigateAsync(request);

        return baseNavigationResponse ?? nestedNavigationResponse;
    }

    private IRegionNavigationService Nested(string regionName = null)
    {
        return NestedServices.TryGetValue(regionName + string.Empty, out var service) ? service : null;
    }

    protected async Task<NavigationResponse> RunNestedNavigation(NavigationRequest request)
    {
        var nestedRequest = request;
        if (nestedRequest is null || !nestedRequest.Segments.IsNested)
        {
            await Task.CompletedTask;
            return null;
        }

        var nestedRoute = nestedRequest.Segments.Base;

        RegionNavigationService nested = null;
        while (nested is null)
        {
            // Try to retrieve nested service based on route name
            nested = Nested(nestedRoute) as RegionNavigationService;
            if (nested is null)
            {
                // No match for named route, so grab any unnamed nested
                nested = Nested() as RegionNavigationService;
            }
            else
            {
                // If we've been able to retrieve the nested service
                // we need to remove the route from the request path
                var nextRoute = nestedRequest.Route.Uri.OriginalString.TrimStart($"{nestedRoute}/");
                nestedRequest = nestedRequest.WithPath(nextRoute);
            }

            if (nested is null)
            {
                await NestedServiceWaiter.Wait();
            }
        }

        var response = await nested.NavigateAsync(nestedRequest.MakeCurrentRequest());
        return response;
    }
}
public class RegionNavigationService : CompositeNavigationService
{
    public IRegion Region { get; set; }

    private int isNavigating = 0;

    public RegionNavigationService(ILogger<RegionNavigationService> logger, IRegionNavigationService parent, IDialogFactory dialogFactory) : base(logger, parent, dialogFactory)
    {
    }

    public async override Task<NavigationResponse> NavigateAsync(NavigationRequest request)
    {
        if (Interlocked.CompareExchange(ref isNavigating, 1, 0) == 1)
        {
            Logger.LazyLogWarning(() => $"Navigation already in progress. Unable to start navigation '{request.ToString()}'");
            return await Task.FromResult(default(NavigationResponse));
        }
        try
        {
            var isDialogNavigation = DialogFactory.IsDialogNavigation(request);
            if (isDialogNavigation)
            {
                // This will skip navigation in this region (ie with the "./" nested prefix)
                // The DialogPrefix will cause the Nested method to return a new nested region specifically for this navigation
                request = request.WithPath(RouteConstants.Schemes.Current + "/" + RouteConstants.RelativePath.DialogPrefix + "/" + request.Route.Uri.OriginalString);
                return await NavigateWithRootAsync(request);
            }

            var regionResponse = await RunRegionNavigation(request);

            if (regionResponse is not null)
            {
                request = request.Segments.NextRequest(request.Sender);
            }

            var baseResponse = await base.NavigateAsync(request);
            return baseResponse ?? regionResponse;
        }
        finally
        {
            Interlocked.Exchange(ref isNavigating, 0);
        }
    }

    private Task<NavigationResponse> NavigateWithRootAsync(NavigationRequest request)
    {
        Logger.LazyLogDebug(() => $"Redirecting navigation request to root Navigation Service");

        return Root.NavigateAsync(request);
    }

    private async Task<NavigationResponse> RunRegionNavigation(NavigationRequest request)
    {
        try
        {
            if (request.Segments.IsCurrent)
            {
                if (Region is not null)
                {
                    var taskCompletion = new TaskCompletionSource<Options.Option>();
                    // Temporarily detach all nested services to prevent accidental
                    // navigation to the wrong child
                    // eg switching tabs, frame on tab1 won't get detached until some
                    // time after navigating to tab2, meaning that the wrong nexted
                    // child will be used for any subsequent navigations.
                    var nested = NestedServices.ToArray();
                    NestedServices.Clear();
                    var regionTask = await Region.NavigateAsync(request);
                    if (regionTask is null)
                    {
                        // If a null result task was returned, then no
                        // navigation took place, so just reattach the existing
                        // nav services
                        nested.ForEach(n => NestedServices[n.Key] = n.Value);
                    }
                    else
                    {
                        _ = regionTask.Result?.ContinueWith((Task<Options.Option> t) =>
                          {
                              if (t.Status == TaskStatus.RanToCompletion)
                              {
                                  taskCompletion.TrySetResult(t.Result);
                              }
                              else
                              {
                                  taskCompletion.TrySetResult(Options.Option.None<object>());
                              }
                          });
                    }
                    return new NavigationResponse(request, taskCompletion.Task);
                }
            }
        }
        finally
        {
            Logger.LazyLogInformation(() => Root.ToString());
        }

        return null;
    }




    public override string ToString()
    {
        var sb = new StringBuilder();
        PrintAllRegions(sb, this);
        return sb.ToString();
    }

    private void PrintAllRegions(StringBuilder builder, RegionNavigationService nav, int indent = 0, string regionName = null)
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
            PrintAllRegions(builder, nested.Value as RegionNavigationService, indent + 1, nested.Key);
        }

        if (nav.Region is null)
        {
            builder.AppendLine("------------------------------------------------------------------------------------------------");
        }
    }


}

public class AsyncAutoResetEvent
{
    private readonly AutoResetEvent _event;

    public AsyncAutoResetEvent(bool initialState)
    {
        _event = new AutoResetEvent(initialState);
    }

    public Task<bool> Wait(TimeSpan? timeout = null)
    {
        return Task.Run(() =>
        {
            if (timeout.HasValue)
            {
                return _event.WaitOne(timeout.Value);
            }
            return _event.WaitOne();
        });
    }

    public void Set()
    {
        _event.Set();
    }
}
