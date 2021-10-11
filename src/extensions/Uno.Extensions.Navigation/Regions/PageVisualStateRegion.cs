using System;
using Microsoft.Extensions.Logging;
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
    protected override string CurrentPath => CurrentVisualState;

    public PageVisualStateRegion(
        ILogger<PageVisualStateRegion> logger,
        IServiceProvider scopedServices,
        INavigationService navigation,
        IViewModelManager viewModelManager,
        IRouteMappings mappings,
        RegionControlProvider controlProvider) : base(logger, scopedServices, navigation, viewModelManager, mappings, controlProvider.RegionControl as Page)
    {
    }

    private string CurrentVisualState { get; set; }

    protected override void Show(string path, Type view, object data)
    {
        CurrentVisualState = path;
        VisualStateManager.GoToState(Control, path, true);
    }
}
