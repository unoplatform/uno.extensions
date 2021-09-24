using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;

namespace Uno.Extensions.Navigation;

public class NavigationService : INavigationRegionService
{
    public INavigationService Parent { get; set; }

    public IRegionService Region { get; set; }

    private IServiceProvider ScopedServices { get; }

    private ILogger Logger { get; }

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
            await Region.NavigateAsync(context);
        }
        finally
        {
            Logger.LazyLogInformation(() => Root.Region.ToString());
        }
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
