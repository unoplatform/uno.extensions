using System.Diagnostics;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Controls;
using System;
using Microsoft.Extensions.DependencyInjection;
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
        private INavigationMapping Mapping { get; }

        private IServiceProvider Services { get; }

        private ITabWrapper Tabs { get; }

        private INavigationService Navigation { get; }

        public void Inject(TabView control)
        {
            Tabs.Inject(control);
        }

        public TabNavigationAdapter(
            INavigationService navigation,
            IServiceProvider services,
            INavigationMapping navigationMapping,
            ITabWrapper tabWrapper)
        {
            Services = services.CreateScope().ServiceProvider;
            Mapping = navigationMapping;
            Tabs = tabWrapper;
        }

        public bool CanNavigate(NavigationContext context)
        {
            var request = context.Request;
            var path = request.Route.Path.OriginalString;
            return Tabs.ContainsTab(path);
        }

        public NavigationResult Navigate(NavigationContext context)
        {
            var request = context.Request;
            var path = request.Route.Path.OriginalString;
            Debug.WriteLine("Navigation: " + path);

            var map = Mapping.LookupByPath(path);

            var vm =  map?.ViewModel is not null ? Services.GetService(map.ViewModel) : null;

            var view = Tabs.ActivateTab(path, vm);

            if(view is INavigationAware navAware)
            {
                navAware.Navigation = Navigation;
            }

            return new NavigationResult(request, Task.CompletedTask, null);
        }
    }
}
