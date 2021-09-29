using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
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

public class TabRegion : SimpleRegion<TabView>
{
    public TabRegion(
        ILogger<TabRegion> logger,
        INavigationService navigation,
        IViewModelManager viewModelManager,
        IDialogFactory dialogFactory,
        RegionControlProvider controlProvider) : base(logger, navigation, viewModelManager, dialogFactory, controlProvider.RegionControl as TabView)
    {
        Control.SelectionChanged += Tabs_SelectionChanged;
    }

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Logger.LazyLogDebug(() => $"Tab changed");
        var tvi = e.AddedItems?.FirstOrDefault() as TabViewItem;
        var tabName = tvi.Name;
        Logger.LazyLogDebug(() => $"Navigating to path {tabName}");
        Navigation.NavigateByPathAsync(null, tabName);
    }

    private TabViewItem FindByName(string tabName)
    {
        Logger.LazyLogDebug(() => $"Looking for tab with name '{tabName}'");
        return (from t in Control.TabItems.OfType<TabViewItem>()
                where t.Name == tabName
                select t).FirstOrDefault();
    }

    protected override object InternalShow(string path, Type view, object data, object viewModel)
    {
        try
        {
            var tab = FindByName(path);
            if (tab is not null)
            {
                Logger.LazyLogDebug(() => $"Selecting tab '{path}'");
                Control.SelectionChanged -= Tabs_SelectionChanged;
                Control.SelectedItem = tab;
                Control.SelectionChanged += Tabs_SelectionChanged;
                Logger.LazyLogDebug(() => $"Tab '{path}' selected");
            }
            else
            {
                Logger.LazyLogWarning(() => $"Tab '{path}' not found");
            }

            return tab?.Content;
        }
        catch (Exception ex)
        {
            Logger.LazyLogError(() => $"Unable to show tab - {ex.Message}");
            return null;
        }
    }
}
