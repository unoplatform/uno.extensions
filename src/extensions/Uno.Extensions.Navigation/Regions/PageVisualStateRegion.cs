using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.ViewModels;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Regions;

public class PageVisualStateRegion : SimpleRegion<Page>
{
    public PageVisualStateRegion(
        ILogger<PageVisualStateRegion> logger,
        IServiceProvider scopedServices,
        INavigationService navigation,
        IViewModelManager viewModelManager,

        RegionControlProvider controlProvider) : base(logger, scopedServices, navigation, viewModelManager, controlProvider.RegionControl as Page)
    {
    }

    protected override void Show(string path, Type view, object data)
    {
        VisualStateManager.GoToState(Control, path, true);
    }
}
