using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;

namespace Uno.Extensions.Navigation;

public class NavigationService : INavigationService
{
    public ILogger Logger { get; }

    protected bool IsRootService => Parent is null;

    private IRegionNavigationService Parent { get; set; }

    protected IDialogFactory DialogFactory { get; }

    public NavigationService(ILogger logger, IRegionNavigationService parent, IDialogFactory dialogFactory)
    {
        Logger = logger;
        Parent = parent;
        DialogFactory = dialogFactory;
    }

    public virtual async Task<NavigationResponse> NavigateAsync(NavigationRequest request)
    {
        if (request is not null && request.Segments.IsParent)
        {
            // Routing navigation request to parent
            return await NavigateWithParentAsync(request);
        }

        return null;
    }

    private Task<NavigationResponse> NavigateWithParentAsync(NavigationRequest request)
    {
        Logger.LazyLogDebug(() => $"Redirecting navigation request to parent Navigation Service");

        var path = request.Route.Uri.OriginalString;
        var parentService = Parent;
        var parentPath = path.Length > (RouteConstants.Schemes.Parent+"/").Length ? path.Substring((RouteConstants.Schemes.Parent + "/").Length) : string.Empty;

        var parentRequest = request.WithPath(parentPath);
        return parentService.NavigateAsync(parentRequest);
    }

    protected NavigationService Root
    {
        get
        {
            return (Parent as NavigationService)?.Root ?? this;
        }
    }

}
