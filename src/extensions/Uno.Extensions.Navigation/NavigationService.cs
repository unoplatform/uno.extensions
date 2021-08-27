namespace Uno.Extensions.Navigation
{
    public class NavigationService : INavigationService
    {
        private INavigationAdapter Adapter { get;  }

        public NavigationService(INavigationAdapter navigationAdapter)
        {
            Adapter = navigationAdapter;
        }

        public NavigationResult Navigate(NavigationRequest request)
        {
            return Adapter.Navigate(request);
        }
    }
}
