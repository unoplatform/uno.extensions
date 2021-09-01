using System;
#if WINDOWS_UWP
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

using System.Linq;

namespace Uno.Extensions.Navigation.Controls;

public class TabWrapper : ITabWrapper
{
    private TabView Tabs { get; set; }

    public void Inject(TabView control) => Tabs = control;

    private TabViewItem FindByName(string tabName)
    {
        return (from t in Tabs.TabItems.OfType<TabViewItem>()
                where t.Name == tabName
                select t).FirstOrDefault();
    }

    public bool ContainsTab(string tabName)
    {
        return FindByName(tabName) is not null;
    }

    public bool ActivateTab(string tabName, Type viewModel, Func<object> creator)
    {
        var tab = FindByName(tabName);
        if (tab is not null)
        {
            if (tab.DataContext == null || viewModel is null || tab.DataContext.GetType() != viewModel)
            {
                tab.DataContext = creator();
            }
            Tabs.SelectedItem = tab;
            return true;
        }
        return false;
    }
}
