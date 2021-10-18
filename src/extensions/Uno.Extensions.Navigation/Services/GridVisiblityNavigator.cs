using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.ViewModels;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
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

public class GridVisiblityNavigator : RegionNavigator<Grid>
{
    protected override object CurrentView => CurrentlyVisibleControl;

    protected override string CurrentPath => CurrentView?.NavigationRoute();

    public GridVisiblityNavigator(
        ILogger<GridVisiblityNavigator> logger,
        IRegion region,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, region, mappings, controlProvider.RegionControl as Grid)
    {
    }

    private UIElement CurrentlyVisibleControl { get; set; }

    protected override void Show(string path, Type viewType, object data)
    {
        var controlToShow = Control.FindName(path) as UIElement;
        if (controlToShow is null)
        {
            try
            {
                if (viewType is null)
                {
                    Logger.LazyLogError(() => "Missing view for navigation path '{path}'");
                    return;
                }

                Logger.LazyLogDebug(() => $"Creating instance of type '{viewType.Name}'");
                controlToShow = Activator.CreateInstance(viewType) as UIElement;
                if (controlToShow is FrameworkElement fe)
                {
                    fe.Name = path;
                }
                Control.Children.Add(controlToShow);
                Logger.LazyLogDebug(() => "Instance created");
            }
            catch (Exception ex)
            {
                Logger.LazyLogError(() => $"Unable to create instance - {ex.Message}");
            }
        }

        controlToShow.Visibility = Visibility.Visible;

        if (CurrentlyVisibleControl != null)
        {
            CurrentlyVisibleControl.Visibility = Visibility.Collapsed;
        }
        CurrentlyVisibleControl = controlToShow;
    }
}
