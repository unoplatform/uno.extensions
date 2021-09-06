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
    public class TabNavigationAdapter : BaseNavigationAdapter<TabView>
    {
        private ITabWrapper Tabs => ControlWrapper as ITabWrapper;

        public TabNavigationAdapter(
            // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
            IServiceProvider services,
            INavigationMapping navigationMapping,
            ITabWrapper tabWrapper):base(services,navigationMapping,tabWrapper)
        {
        }

        public override NavigationResult Navigate(NavigationContext context)
        {
            var request = context.Request;
            var path = context.Path;
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
