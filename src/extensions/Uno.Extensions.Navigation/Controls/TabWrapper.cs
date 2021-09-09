using System;
#if WINDOWS_UWP
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

using System.Linq;

namespace Uno.Extensions.Navigation.Controls;

public class TabWrapper : BaseWrapper<TabView>, ITabWrapper
{
    private TabView Tabs => Control;

    public override NavigationContext CurrentContext => (Tabs.SelectedItem as TabViewItem).GetContext(); 

    private TabViewItem FindByName(string tabName)
    {
        return (from t in Tabs.TabItems.OfType<TabViewItem>()
                where t.Name == tabName
                select t).FirstOrDefault();
    }

    public string CurrentTabName
    {
        get
        {
            var active = Tabs.SelectedItem as TabViewItem;
            return active?.Name;
        }
    }

    public bool ContainsTab(string tabName)
    {
        return FindByName(tabName) is not null;
    }

    public object ActivateTab(NavigationContext context, string tabName, object viewModel)
    {
        var tab = FindByName(tabName);
        if (tab is not null)
        {
            if (tab.DataContext != viewModel)
            {
                tab.DataContext = viewModel;
            }
            tab.SetContext(context);
            Tabs.SelectedItem = tab;
            return tab;
        }
        return null;
    }
}
