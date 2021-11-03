using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
#if !WINDOWS_UWP && !WINUI
using Popup = Windows.UI.Xaml.Controls.Popup;
#endif
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.Extensions.Navigation;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
#endif

namespace Uno.Extensions.Navigation.Navigators;

public class PopupNavigator : ControlNavigator<Popup>
{
    protected override FrameworkElement CurrentView => Control;

    public PopupNavigator(
        ILogger<ContentControlNavigator> logger,
        IRegion region,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, region, mappings, controlProvider.RegionControl as Popup)
    {
    }


    public override void ControlInitialize()
    {
        base.ControlInitialize();

        Control.Closed += Control_Closed;
    }

    private void Control_Closed(object sender, object e)
    {
        Region.Navigator().NavigateToRouteAsync(sender, "hide");
    }

    protected override async Task<string> Show(string path, Type viewType, object data)
    {
        try
        {
            Control.IsOpen = string.Compare(path, RouteConstants.PopupShow, true) == 0;
            await (Control.Child as FrameworkElement).EnsureLoaded();
            return path;
        }
        catch (Exception ex)
        {
            Logger.LogErrorMessage($"Unable to create instance - {ex.Message}");
        }

        return default;
    }
}
