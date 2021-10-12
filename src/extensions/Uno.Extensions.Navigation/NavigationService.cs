using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;

namespace Uno.Extensions.Navigation;

public class NavigationService : INavigationService
{
    protected ILogger Logger { get; }

    protected bool IsRootService => Parent is null;

    private INavigationService Parent { get; }

    public NavigationService(ILogger logger, INavigationService parent)
    {
        Parent = parent;
        Logger = logger;
    }

    public Task<NavigationResponse> NavigateAsync(NavigationRequest request)
    {
        return CoreNavigateAsync(request);
    }

    protected virtual Task<NavigationResponse> CoreNavigateAsync(NavigationRequest request)
    {
        // Handle root navigations
        if (request?.Route?.IsRoot ?? false)
        {
            return Parent?.NavigateAsync(request);
        }

        // Handle parent navigations
        if (request?.Route?.IsParent ?? false)
        {
            request = request with { Route = request.Route.TrimScheme(Schemes.Parent) };
            return Parent?.NavigateAsync(request);
        }

        return Task.FromResult(default(NavigationResponse));
    }
}
