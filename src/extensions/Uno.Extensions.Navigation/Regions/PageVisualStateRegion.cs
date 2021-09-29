using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.ViewModels;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.Regions.Managers;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Regions.Managers;

public class PageVisualStateRegion : SimpleRegion<Page>
{
    public PageVisualStateRegion(
        ILogger<PageVisualStateRegion> logger,
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
