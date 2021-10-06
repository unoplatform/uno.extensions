using System;
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
