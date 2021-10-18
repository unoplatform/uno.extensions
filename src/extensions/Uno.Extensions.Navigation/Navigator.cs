using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class Navigator : INavigator
{
    protected ILogger Logger { get; }

    private bool IsRoot => Region?.Parent is null;

    protected IRegion Region { get; }

    public Navigator(
        ILogger<Navigator> logger,
        IRegion region)
    {
        Region = region;
        Logger = logger;
    }

    protected Navigator(
    ILogger logger,
    IRegion region)
    {
        Region = region;
        Logger = logger;
    }

    public Task<NavigationResponse> NavigateAsync(NavigationRequest request)
    {
        // Handle root navigations
        if (request?.Route?.IsRoot ?? false)
        {
            if (!IsRoot)
            {
                return Region.Parent?.NavigateAsync(request);
            }
            else
            {
                // This is the root nav service - need to pass the
                // request down to children by making the request nested
                request = request with { Route = request.Route.Root };
            }
        }

        if (request?.Route?.IsParent ?? false)
        {
            request = request with { Route = request.Route.TrimScheme(Schemes.Parent) };

            // Handle parent navigations
            if (request?.Route?.IsParent ?? false)
            {
                return Region.Parent?.NavigateAsync(request);
            }
        }

        // Run dialog requests
        if (request.Route.IsDialog)
        {
            request = request with { Route = request.Route.TrimScheme(Schemes.Dialog) };
            return DialogNavigateAsync(request);
        }

        return CoreNavigateAsync(request);
    }

    private async Task<NavigationResponse> DialogNavigateAsync(NavigationRequest request)
    {
        var dialogService = Region.NavigationFactory().CreateService(Region.Services, request);

        var dialogResponse = await dialogService.NavigateAsync(request);

        return dialogResponse;
    }

    //private bool Match(IRegion region, NavigationRequest request)
    //{
    //    return
    //            // eg  Scheme "./"  Base    ""  Path  "MainPage"  --> MainPage sent to all children
    //            request.Route.Base is { Length: 0 } ||
    //            // eg  Request sent through with no changes
    //            region.Name is { Length: 0 } ||
    //            // eg  Scheme ""    Base    "Feeds"     Path    "TweetsPage" --> TweetsPage sent to all children
    //            region.Name == request.Route.Base;
    //}

    private (IRegion, NavigationRequest) Match(IRegion region, NavigationRequest request)
    {
        if (
            // eg  Scheme "./"  Base    ""  Path  "MainPage"  --> MainPage sent to all children
            request.Route.Base is { Length: 0 } ||
            // eg  Scheme ""    Base    "Feeds"     Path    "TweetsPage" --> TweetsPage sent to all children
            region.Name == request.Route.Base
        )
        {
            return (region, request with { Route = request.Route.Next });
        }
        // eg  Request sent through with no changes
        if(region.Name is { Length: 0 })
        {
            return (region, request with { Route = request.Route with {  Scheme=Schemes.Current} });
        }

        return default;
    }

    protected virtual Task<NavigationResponse> CoreNavigateAsync(NavigationRequest request)
    {
        //if (Region.View is not null)
        //{
        //    var completion = new TaskCompletionSource<NavigationResponse>();
        //    Region.View.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
        //    {
        //        var response = await ChildrenNavigateAsync(request);
        //        completion.SetResult(response);
        //    });

        //    return completion.Task;
        //}
        //else
        //{
        return ChildrenNavigateAsync(request);
        //}
    }

    private async Task<NavigationResponse> ChildrenNavigateAsync(NavigationRequest request)
    {
        var route = request.Route.Base;

        var children = await Region.GetChildren(child => Match(child, request), !request.Route.IsLast);
        if (!(children?.Any() ?? false))
        {
            return null;
        }

        return await ChildrenNavigateAsync(children);
    }

    private async Task<NavigationResponse> ChildrenNavigateAsync(IEnumerable<(IRegion, NavigationRequest)> children) // IEnumerable<INavigationService> children, NavigationRequest request)
    {
        var tasks = new List<Task<NavigationResponse>>();
        foreach (var region in children)
        {
            tasks.Add(region.Item1.NavigateAsync(region.Item2));
        }

        await Task.WhenAll(tasks);
#pragma warning disable CA1849 // We've already waited all tasks at this point (see Task.WhenAll in line above)
        return tasks.First().Result;
#pragma warning restore CA1849
    }
}
