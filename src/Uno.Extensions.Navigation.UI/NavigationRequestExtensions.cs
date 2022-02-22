using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public static class NavigationRequestExtensions
{
    public static object? RouteResourceView(this NavigationRequest request, IRegion region)
    {
        object resource;
        if ((request.Sender is FrameworkElement senderElement &&
            senderElement.Resources.TryGetValue(request.Route.Base, out resource)) ||

            (region.View is FrameworkElement regionElement &&
            regionElement.Resources.TryGetValue(request.Route.Base, out resource)) ||

            (Application.Current.Resources.TryGetValue(request.Route.Base, out resource)))
        {
            return resource;

        }

        return null;
    }
}
