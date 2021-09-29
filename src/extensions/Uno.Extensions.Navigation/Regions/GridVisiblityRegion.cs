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

public class GridVisiblityRegion : SimpleRegion<Grid>
{
    public GridVisiblityRegion(
        ILogger<GridVisiblityRegion> logger,
        INavigationService navigation,
        IViewModelManager viewModelManager,
        IDialogFactory dialogFactory,
        RegionControlProvider controlProvider) : base(logger, navigation, viewModelManager, dialogFactory, controlProvider.RegionControl as Grid)
    {
    }

    private FrameworkElement CurrentlyVisibleControl { get; set; }

    protected override object InternalShow(string path, Type view, object data, object viewModel)
    {
        var controlToShow = Control.FindName(path) as FrameworkElement;
        if (controlToShow is null)
        {
            return null;
        }

        controlToShow.Visibility = Visibility.Visible;

        if (CurrentlyVisibleControl != null)
        {
            CurrentlyVisibleControl.Visibility = Visibility.Collapsed;
        }
        CurrentlyVisibleControl = controlToShow;
        return controlToShow;
    }
}
