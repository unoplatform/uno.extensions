using System.Collections.Generic;

namespace Uno.Extensions.Navigation;

public record ActiveNavigationService(NavigationService Navigation, INavigationAdapter Adapter, ActiveNavigationService ParentAdapter = null) : INavigationService
{
    public IDictionary<string, ActiveNavigationService> NestedAdapters { get; } =   new Dictionary<string, ActiveNavigationService>();

    public NavigationResponse Navigate(NavigationRequest request)
    {
        return Navigation.NavigateWithAdapter(request, this);
    }

    public INavigationService ParentNavigation()
    {
        return ParentAdapter;
    }

    public INavigationService NestedNavigation(string routeName = null)
    {
        return NestedAdapters.TryGetValue(routeName + string.Empty, out var service) ? service : null;
    }
}
