using System.Diagnostics;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Controls;
#if WINDOWS_UWP 
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Adapters
{
    public class TabNavigationAdapter : INavigationAdapter<TabView>
    {
        private ITabWrapper Tabs { get; }
        public void Inject(TabView control)
        {
            Tabs.Inject(control);
        }
        public TabNavigationAdapter(ITabWrapper tabWrapper)
        {
            Tabs = tabWrapper;
        }

        public bool CanNavigate(NavigationRequest request)
        {
            var path = request.Route.Path.OriginalString;
            return Tabs.ContainsTab(path);
        }

        public NavigationResult Navigate(NavigationRequest request)
        {
            var path = request.Route.Path.OriginalString;
            Debug.WriteLine("Navigation: " + path);

            Tabs.ActivateTab(path);   

            return new NavigationResult(request, Task.CompletedTask);
        }
    }
}
