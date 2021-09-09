namespace Uno.Extensions.Navigation
{
    public record ActiveNavigationService(NavigationService Navigation, INavigationAdapter Adapter) : INavigationService
    {
        public NavigationResponse Navigate(NavigationRequest request)
        {
            return Navigation.NavigateWithAdapter(request, Adapter);
        }

        public INavigationService ParentNavigation()
        {
            return Navigation.ParentNavigation(Adapter);
        }

        public INavigationService ChildNavigation(string adapterName = null)
        {
            return Navigation.ChildNavigation(Adapter, adapterName);
        }

    }
}
