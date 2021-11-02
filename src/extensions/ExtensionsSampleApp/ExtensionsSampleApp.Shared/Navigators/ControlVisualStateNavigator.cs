using System;
using Microsoft.Extensions.Logging;

using Uno.Extensions.Navigation.Regions;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Services;
using Uno.Extensions.Navigation;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace ExtensionsSampleApp.Navigators;

public class ControlVisualStateNavigator : ControlNavigator<Control>
{
    public const string NavigatorName = "VisualState";

    public ControlVisualStateNavigator(
        ILogger<ControlVisualStateNavigator> logger,
        IRegion region,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, region, mappings, (controlProvider.RegionControl as DependencyObject).ServiceForControl(true, entity => entity as Control))
    {
    }

    protected override async Task Show(string path, Type viewType, object data)
    {
        VisualStateManager.GoToState(Control, path, true);
    }
}
