using System;
using System.Linq;
#if WINDOWS_UWP
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Controls;

public class TabManager : BaseControlManager<TabView>, IViewManager<TabView>
{
    private INavigationService Navigation { get; }

    public TabManager(INavigationService navigation)
    {
        Navigation = navigation;
    }

    public override void Inject(object control)
    {
        base.Inject(control);
        Control.TabItemsChanged += Tabs_TabItemsChanged;
    }

    private void Tabs_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
    {
        if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemInserted)
        {
            var tvi = Control.TabItems[(int)args.Index] as TabViewItem;
            var tabName = tvi.Name;
            Navigation.NavigateByPathAsync(null, tabName);
        }
    }

    private TabViewItem FindByName(string tabName)
    {
        return (from t in Control.TabItems.OfType<TabViewItem>()
                where t.Name == tabName
                select t).FirstOrDefault();
    }

    public string CurrentTabName
    {
        get
        {
            var active = Control.SelectedItem as TabViewItem;
            return active?.Name;
        }
    }

    public bool ContainsTab(string tabName)
    {
        return FindByName(tabName) is not null;
    }

    public void ChangeView(INavigationService navigation, string path, Type view, bool isBackNavigation, object data, object viewModel, bool setFocus)
    {
        var tab = FindByName(path);
        if (tab is not null)
        {
            InitialiseView(tab, navigation, viewModel);

            // Only set the selected tab if there's a valid sender
            // otherwise, we're just setting up the viewmodel etc
            // for tabs when they're created
            if (setFocus)
            {
                Control.SelectedItem = tab;
            }
        }
    }
}
