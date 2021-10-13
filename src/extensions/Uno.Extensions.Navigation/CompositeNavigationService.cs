using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Navigation;

public class CompositeNavigationService : NavigationService, IRegionNavigationService
{
    private IList<(string, IRegionNavigationService)> Children { get; } = new List<(string, IRegionNavigationService)>();

    private AsyncAutoResetEvent NestedServiceWaiter { get; } = new AsyncAutoResetEvent(false);

    public CompositeNavigationService(
        ILogger logger,
        IRegionNavigationService parent,
        IRegionNavigationServiceFactory serviceFactory)
        : base(logger, parent, serviceFactory)
    {
    }

    public void Attach(IRegionNavigationService childRegion, string regionName)
    {
        var childService = childRegion;
        Children.Add((regionName + string.Empty, childService));
        NestedServiceWaiter.Set();
    }

    public void Detach(IRegionNavigationService childRegion)
    {
        Children.Remove(kvp => kvp.Item2 == childRegion);
    }

    protected void AttachAll(IEnumerable<(string, IRegionNavigationService)> children)
    {
        children.ForEach(n => Children[n.Key] = n.Value);
    }

    protected IEnumerable<(string, IRegionNavigationService)> DetachAll()
    {
        var children = Children.ToArray();
        Children.Clear();
        return children;
    }

    protected async override Task<NavigationResponse> CoreNavigateAsync(NavigationRequest request)
    {
        // At this point, any residual request needs to be handed
        // down to the appropriate nested service
        return await ChildrenNavigateAsync(request);
    }

    private async Task<NavigationResponse> ChildrenNavigateAsync(NavigationRequest request)
    {
        if (!(request?.Route?.IsNested ?? false))
        {
            return null;
        }

        var route = request.Route.Base;

        // TODO: Find a better way - this can potentially block in endless loop if no nested region is added
        while (true)
        {
            // Attempt to navigate using the route name
            var namedChildren = Children.Where(kvp => kvp.Item1 == route).Select(x => x.Item2);
            if (namedChildren.Any())
            {
                var childRequest = request with
                {
                    Route = request.Route with
                    {
                        Scheme = request.Route.Scheme.TrimStartOnce(Schemes.Nested),
                        Base = request.Route.NextBase(),
                        Path = request.Route.NextPath()
                    }
                };

                return await ChildrenNavigateAsync(namedChildren, childRequest);
            }

            // Attempt to navigate using empty route
            var unnamedChildren = Children.Where(kvp => string.IsNullOrWhiteSpace(kvp.Item1)).Select(x => x.Item2);
            if (unnamedChildren.Any())
            {
                var childRequest = request with
                {
                    Route = request.Route with
                    {
                        Scheme = request.Route.Scheme.TrimStartOnce(Schemes.Nested)
                    }
                };
                return await ChildrenNavigateAsync(unnamedChildren, childRequest);
            }

            // There aren't any nested regions registered, so need
            // to block until they are added. The Attach method will
            // signal on the NestedServicerWaitier when a region is
            // added
            await NestedServiceWaiter.Wait();
        }
    }

    private async Task<NavigationResponse> ChildrenNavigateAsync(IEnumerable<INavigationService> children, NavigationRequest request)
    {
        var tasks = new List<Task<NavigationResponse>>();
        foreach (var region in children)
        {
            tasks.Add(region.NavigateAsync(request));
        }

        await Task.WhenAll(tasks);
#pragma warning disable CA1849 // We've already waited all tasks at this point (see Task.WhenAll in line above)
        return tasks.First().Result;
#pragma warning restore CA1849
    }

    protected virtual void PrintAllRegions(StringBuilder builder, IRegionNavigationService nav, int indent = 0, string regionName = null)
    {
        if (nav is CompositeNavigationService comp)
        {
            foreach (var nested in comp.Children)
            {
                PrintAllRegions(builder, nested.Item2 as IRegionNavigationService, indent + 1, nested.Item1);
            }
        }
    }
}
