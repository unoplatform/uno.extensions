using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;

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

public class TabViewNavigator : ControlNavigator<TabView>
{
    protected override FrameworkElement CurrentView => (Control.SelectedItem as TabViewItem)?.Content as FrameworkElement;

    public TabViewNavigator(
        ILogger<TabViewNavigator> logger,
        IRegion region,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, region, mappings, controlProvider.RegionControl as TabView)
    {
    }

    public override void ControlInitialize()
    {
        Control.SelectionChanged += Tabs_SelectionChanged;
    }

    private async void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Logger.LogDebugMessage($"Tab changed");
        var tvi = e.AddedItems?.FirstOrDefault() as TabViewItem;
        if (tvi is null)
        {
            return;
        }
        await tvi.EnsureLoaded();
        var tabName = (tvi.Content as FrameworkElement).GetName() ?? tvi.Name;
        await Region.Navigator().NavigateToRouteAsync(tvi, tabName);
    }

    private TabViewItem FindByName(string tabName)
    {
        Logger.LogDebugMessage($"Looking for tab with name '{tabName}'");
        return (from t in Control.TabItems.OfType<TabViewItem>()
                where t.Name == tabName
                select t).FirstOrDefault();
    }

    protected override async Task Show(string path, Type viewType, object data)
    {
        try
        {
            var tab = FindByName(path);
            if (tab is not null)
            {
                Logger.LogDebugMessage($"Selecting tab '{path}'");
                Control.SelectionChanged -= Tabs_SelectionChanged;
                Control.SelectedItem = tab;
                await (tab.Content as FrameworkElement).EnsureLoaded();
                Control.SelectionChanged += Tabs_SelectionChanged;
                Logger.LogDebugMessage($"Tab '{path}' selected");
            }
            else
            {
                Logger.LogWarningMessage($"Tab '{path}' not found");
            }
        }
        catch (Exception ex)
        {
            Logger.LogErrorMessage($"Unable to show tab - {ex.Message}");
        }
    }
}
