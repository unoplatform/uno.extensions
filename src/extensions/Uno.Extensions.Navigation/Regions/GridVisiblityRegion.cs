﻿using System;
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
    protected override object CurrentView => CurrentlyVisibleControl;

    protected override string CurrentPath => CurrentView?.NavigationPath();

    public GridVisiblityRegion(
        ILogger<GridVisiblityRegion> logger,
        IServiceProvider scopedServices,
        INavigationService navigation,
        IViewModelManager viewModelManager,
        INavigationMappings mappings,
        RegionControlProvider controlProvider) : base(logger, scopedServices, navigation, viewModelManager, mappings, controlProvider.RegionControl as Grid)
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
