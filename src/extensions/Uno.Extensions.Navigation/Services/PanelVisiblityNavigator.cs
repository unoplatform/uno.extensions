using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.ViewModels;
using Uno.Extensions.Logging;
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

public class PanelVisiblityNavigator : ControlNavigator<Panel>
{
    public const string NavigatorName = "Visibility";

    protected override FrameworkElement CurrentView => CurrentlyVisibleControl;

    protected override string CurrentPath => CurrentView?.NavigationRoute();

    public PanelVisiblityNavigator(
        ILogger<PanelVisiblityNavigator> logger,
        IRegion region,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, region, mappings, controlProvider.RegionControl as Grid)
    {
    }

    private FrameworkElement CurrentlyVisibleControl { get; set; }

    protected override async Task Show(string path, Type viewType, object data)
    {
        var controlToShow = Control.FindName(path) as FrameworkElement;
        if (controlToShow is null)
        {
            try
            {
                if (viewType is null)
                {
                    Logger.LogErrorMessage("Missing view for navigation path '{path}'");
                    return;
                }

                Logger.LogDebugMessage($"Creating instance of type '{viewType.Name}'");
                controlToShow = Activator.CreateInstance(viewType) as FrameworkElement;
                if (controlToShow is FrameworkElement fe)
                {
                    fe.Name = path;
                }
                Control.Children.Add(controlToShow);
                Logger.LogDebugMessage("Instance created");
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage($"Unable to create instance - {ex.Message}");
            }
        }

        controlToShow.Visibility = Visibility.Visible;

        if (CurrentlyVisibleControl != null)
        {
            CurrentlyVisibleControl.Visibility = Visibility.Collapsed;
        }
        CurrentlyVisibleControl = controlToShow;

        await (controlToShow as FrameworkElement).EnsureLoaded();
    }
}
