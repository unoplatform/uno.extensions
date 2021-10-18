using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public abstract class NavigationService : INavigationService
{
    protected ILogger Logger { get; }

    private bool IsRoot => Region?.Parent is null;

    protected IRegion Region { get; }

    private IRegionNavigationServiceFactory ServiceFactory { get; }

    protected NavigationService(
        ILogger logger,
        IRegion region,
        IRegionNavigationServiceFactory serviceFactory)
    {
        Region = region;
        Logger = logger;
        ServiceFactory = serviceFactory;
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
                request = request with { Route = request.Route.TrimScheme(Schemes.Root).AppendScheme(Schemes.Nested) };
            }
        }

        // Handle parent navigations
        if (request?.Route?.IsParent ?? false)
        {
            request = request with { Route = request.Route.TrimScheme(Schemes.Parent) };
            return Region.Parent?.NavigateAsync(request);
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
        var dialogService = ServiceFactory.CreateService(Region.Services, request);

        var dialogResponse = await dialogService.NavigateAsync(request);

        return dialogResponse;
    }

    protected abstract Task<NavigationResponse> CoreNavigateAsync(NavigationRequest request);
}
