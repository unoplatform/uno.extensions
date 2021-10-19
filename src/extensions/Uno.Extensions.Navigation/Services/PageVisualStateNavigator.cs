using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.ViewModels;
using Uno.Extensions.Navigation.Regions;
using System.Threading.Tasks;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Services;

public class PageVisualStateNavigator : ControlNavigator<Page>
{
    protected override string CurrentPath => CurrentVisualState;

    public PageVisualStateNavigator(
        ILogger<PageVisualStateNavigator> logger,
        IRegion region,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, region, mappings, controlProvider.RegionControl as Page)
    {
    }

    private string CurrentVisualState { get; set; }

    protected override async Task Show(string path, Type view, object data)
    {
        CurrentVisualState = path;
        VisualStateManager.GoToState(Control, path, true);
    }
}
