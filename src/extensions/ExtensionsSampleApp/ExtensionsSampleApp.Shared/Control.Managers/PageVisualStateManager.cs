using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ExtensionsSampleApp.Control.Managers
{
    public class PageVisualStateManager : BaseControlManager<Page>
    {
        public PageVisualStateManager(ILogger<PageVisualStateManager> logger, INavigationService navigation, RegionControlProvider controlProvider) : base(logger, navigation, controlProvider.RegionControl as Page)
        {
        }

        protected override object InternalShow(string path, Type view, object data, object viewModel)
        {
            VisualStateManager.GoToState(Control, path, true);
            return null;
        }
    }
}
