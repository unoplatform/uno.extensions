using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
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

        public override bool IsCurrentPath(string path)
        {
            return Tabs.CurrentTabName == path;
        }

        public TabNavigationAdapter(
            // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
            IServiceProvider services,
            INavigationMapping navigationMapping,
            ITabWrapper tabWrapper) : base(services, navigationMapping, tabWrapper)
        {
        }
    }
}
