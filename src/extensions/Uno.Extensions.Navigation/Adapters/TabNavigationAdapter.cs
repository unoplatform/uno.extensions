using System.Diagnostics;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation.Adapters
{
    public class TabNavigationAdapter : INavigationAdapter
    {

        private ITabWrapper NavigationTabs { get; }

        private INavigationMapping Mapping { get; }

        public TabNavigationAdapter(ITabWrapper tabWrapper, INavigationMapping navigationMapping)
        {
            NavigationTabs = tabWrapper;
            Mapping = navigationMapping;
        }

        public NavigationResult Navigate(NavigationRequest request)
        {
            var path = request.Route.Path.OriginalString;
            Debug.WriteLine("Navigation: " + path);

            NavigationTabs.ActivateTab(path);   

            return new NavigationResult(request, Task.CompletedTask);
        }
    }
}
