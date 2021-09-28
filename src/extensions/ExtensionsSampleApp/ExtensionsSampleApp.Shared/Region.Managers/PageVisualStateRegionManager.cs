using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Extensions.Navigation.ViewModels;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.Regions.Managers;

namespace ExtensionsSampleApp.Region.Managers
{
    public class PageVisualStateRegionManager : SimpleRegionManager<Page>
    {
        public PageVisualStateRegionManager(
            ILogger<PageVisualStateRegionManager> logger,
            INavigationService navigation,
        IViewModelManager viewModelManager,
        IDialogFactory dialogFactory,
        RegionControlProvider controlProvider) : base(logger, navigation, viewModelManager, dialogFactory, controlProvider.RegionControl as Page)
        {
        }

        protected override object InternalShow(string path, Type view, object data, object viewModel)
        {
            VisualStateManager.GoToState(Control, path, true);
            return null;
        }
    }
}
