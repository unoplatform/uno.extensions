using System;
using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation.Adapters
{
    public class TabNavigationAdapter : BaseNavigationAdapter
    {
        private ITabWrapper Tabs => ControlWrapper as ITabWrapper;

        public override bool IsCurrentPath(string path)
        {
            return Tabs.CurrentTabName == path;
        }

        public TabNavigationAdapter(
            // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
            IServiceProvider services,
            ITabWrapper tabWrapper) : base(services, tabWrapper)
        {
        }
    }
}
