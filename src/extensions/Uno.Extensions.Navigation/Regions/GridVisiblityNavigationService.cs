using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
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

public class GridVisiblityNavigationService : ControlNavigationService<Grid>
{
    protected override object CurrentView => CurrentlyVisibleControl;

    protected override string CurrentPath => CurrentView?.NavigationRoute();

    public GridVisiblityNavigationService(
        ILogger<GridVisiblityNavigationService> logger,
        IRegionNavigationService parent,
        IRegionNavigationServiceFactory serviceFactory,
        IScopedServiceProvider scopedServices,
        IViewModelManager viewModelManager,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, parent, serviceFactory, scopedServices, viewModelManager, mappings, controlProvider.RegionControl as Grid)
    {
    }

    private FrameworkElement CurrentlyVisibleControl { get; set; }

    protected override void Show(string path, Type view, object data)
    {
        var controlToShow = Control.FindName(path) as FrameworkElement;
        if (controlToShow is null)
        {
            return;
        }

        controlToShow.Visibility = Visibility.Visible;

        if (CurrentlyVisibleControl != null)
        {
            CurrentlyVisibleControl.Visibility = Visibility.Collapsed;
        }
        CurrentlyVisibleControl = controlToShow;
    }
}
