using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;

namespace Uno.Extensions.Navigation;

public class NavigationService : INavigationService
{
    protected ILogger Logger { get; }

    private bool IsRoot => Parent is null;

    private INavigationService Parent { get; }

    protected NavigationService(
        ILogger logger,
        INavigationService parent)
    {
        Parent = parent;
        Logger = logger;
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

        return CoreNavigateAsync(request);
    }

    protected virtual Task<NavigationResponse> CoreNavigateAsync(NavigationRequest request)
    {
        return Task.FromResult(default(NavigationResponse));
    }
}
