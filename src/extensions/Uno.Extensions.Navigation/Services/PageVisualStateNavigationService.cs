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

namespace Uno.Extensions.Navigation.Services;

public class PageVisualStateNavigationService : ControlNavigationService<Page>
{
    protected override string CurrentPath => CurrentVisualState;

    public PageVisualStateNavigationService(
        ILogger<PageVisualStateNavigationService> logger,
        IRegionNavigationService parent,
        IRegionNavigationServiceFactory serviceFactory,
        IScopedServiceProvider scopedServices,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, parent, serviceFactory, scopedServices, mappings, controlProvider.RegionControl as Page)
    {
    }

    private string CurrentVisualState { get; set; }

    protected override void Show(string path, Type view, object data)
    {
        CurrentVisualState = path;
        VisualStateManager.GoToState(Control, path, true);
    }
}
