using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;

namespace Uno.Extensions.Navigation;

public class NavigationService : INavigationService
{
    public Region Region { get; set; }

    private IServiceProvider ScopedServices { get; }

    private ILogger Logger { get; }

    private bool IsRootService => Region.Parent is null;

    public PendingContext PendingNavigation { get; set; }

    public NavigationService(ILogger<NavigationService> logger, IServiceProvider services)
    {
        Logger = logger;
        ScopedServices = services;
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
            Logger.LazyLogInformation(() => Root.Region.ToString());
        }
    }

    private NavigationResponse NavigateWithParentAsync(NavigationRequest request)
    {
        Logger.LazyLogDebug(() => $"Redirecting navigation request to parent Navigation Service");

        var path = request.Route.Uri.OriginalString;
        var parentService = Region.Parent.Navigation;
        var parentPath = path.Length > NavigationConstants.RelativePath.ParentPath.Length ? path.Substring(NavigationConstants.RelativePath.ParentPath.Length) : string.Empty;

        var parentRequest = request.WithPath(parentPath);
        return parentService.NavigateAsync(parentRequest);
    }

    //public async Task NavigateAsync(NavigationContext context)
    //{
    //    if (PendingNavigation is null)
    //    {
    //        PendingNavigation = context.Pending();
    //    }

    //    await RunPendingNavigation();
    //}




    public async Task RunPendingNavigation()
    {
        var pending = PendingNavigation;
        if (pending is not null)
        {
            PendingNavigation = null;
            var navTask = pending.TaskCompletion;
            var navContext = pending.Context;

            var navResult = await Region.RunRegionNavigation(navContext);

            if (navResult.Item1)
            {
                if (navResult.Item2 is not null)
                {
                    //var nestedContext = navResult.Item2;
                    var nestedRequest = navResult.Item2;// nestedContext.Request;
                    var nestedRoute = nestedRequest.FirstRouteSegment;

                    var nested = Region.Nested(nestedRoute)?.Navigation as NavigationService;
                    if (nested is null)
                    {
                        nested = Region.Nested()?.Navigation as NavigationService;
                    }
                    else
                    {
                        var nextRoute = nestedRequest.Route.Uri.OriginalString.TrimStart($"{nestedRoute}/");
                        nestedRequest = nestedRequest.WithPath(nextRoute);//.BuildNavigationContext(nested.Services, new TaskCompletionSource<Options.Option>());
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

    private NavigationService Root
    {
        get
        {
            return (Region.Parent?.Navigation as NavigationService)?.Root ?? this;
        }
    }
}
