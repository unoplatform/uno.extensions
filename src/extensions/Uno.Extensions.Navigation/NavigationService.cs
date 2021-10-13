using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;

namespace Uno.Extensions.Navigation;

public abstract class NavigationService : INavigationService
{
    protected ILogger Logger { get; }

    private bool IsRoot => Parent is null;

    private INavigationService Parent { get; }

    private IRegionNavigationServiceFactory ServiceFactory { get; }

    protected NavigationService(
        ILogger logger,
        INavigationService parent,
        IRegionNavigationServiceFactory serviceFactory)
    {
        Parent = parent;
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
                return Parent?.NavigateAsync(request);
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
            return Parent?.NavigateAsync(request);
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
        var dialogService = ServiceFactory.CreateService(request);

        var dialogResponse = await dialogService.NavigateAsync(request);

        return dialogResponse;
    }

    protected abstract Task<NavigationResponse> CoreNavigateAsync(NavigationRequest request);
}
