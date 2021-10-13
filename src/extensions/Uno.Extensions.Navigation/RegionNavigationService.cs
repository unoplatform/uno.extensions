using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Navigation;

public abstract class RegionNavigationService : CompositeNavigationService
{
    protected RegionNavigationService(
        ILogger logger,
        IRegionNavigationService parent,
        IRegionNavigationServiceFactory serviceFactory)
        : base(logger, parent, serviceFactory)
    {
    }

    protected async override Task<NavigationResponse> NestedNavigateAsync(NavigationRequest request)
    {
        var regionResponse = await RegionNavigateAsync(request);

        if (regionResponse is not null)
        {
            request = request.Route.NextRequest(request.Sender);
        }

        var baseResponse = await base.NestedNavigateAsync(request);
        return baseResponse ?? regionResponse;

    }

    private async Task<NavigationResponse> RegionNavigateAsync(NavigationRequest request)
    {
        if (request.Route.IsCurrent)
        {
            var taskCompletion = new TaskCompletionSource<Options.Option>();
            // Temporarily detach all nested services to prevent accidental
            // navigation to the wrong child
            // eg switching tabs, frame on tab1 won't get detached until some
            // time after navigating to tab2, meaning that the wrong nexted
            // child will be used for any subsequent navigations.
            var nested = NestedServices.ToArray();
            NestedServices.Clear();
            var regionTask = await ControlNavigateAsync(request);
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
                  },
                  TaskScheduler.Current);
            }
            return new NavigationResponse(request, taskCompletion.Task);
        }

        return null;
    }

    protected abstract Task<NavigationResponse> ControlNavigateAsync(NavigationRequest request);

    public override string ToString()
    {
        var sb = new StringBuilder();
        PrintAllRegions(sb, this);
        return sb.ToString();
    }

    protected override void PrintAllRegions(StringBuilder builder, IRegionNavigationService nav, int indent = 0, string regionName = null)
    {
    //    if (nav is RegionNavigationService rns)
    //    {
    //        if (rns.Region is null)
    //        {
    //            builder.AppendLine(string.Empty);
    //            builder.AppendLine("------------------------------------------------------------------------------------------------");
    //            builder.AppendLine($"ROOT");
    //        }
    //        else
    //        {
    //            var ans = nav;
    //            var prefix = string.Empty;
    //            if (indent > 0)
    //            {
    //                prefix = new string(' ', indent * 2) + "|-";
    //            }
    //            var reg = !string.IsNullOrWhiteSpace(regionName) ? $"({regionName}) " : null;
    //            builder.AppendLine($"{prefix}{reg}{rns.Region?.ToString()}");
    //        }
    //    }

    //    base.PrintAllRegions(builder, nav, indent, regionName);

    //    if (nav is RegionNavigationService rns2 &&
    //        rns2.Region is null)
    //    {
    //        builder.AppendLine("------------------------------------------------------------------------------------------------");
    //    }
    }
}
