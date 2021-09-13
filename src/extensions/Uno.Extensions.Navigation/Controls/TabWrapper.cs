using System.Linq;
#if WINDOWS_UWP
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Controls;

public class TabWrapper : BaseWrapper, ITabWrapper
{
    private TabView Tabs => Control as TabView;

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

    public void Navigate(NavigationContext context, bool isBackNavigation, object viewModel)
    {
        var tab = FindByName(context.Path);
        if (tab is not null)
        {
            InitialiseView(tab, context, viewModel);
            Tabs.SelectedItem = tab;
        }
    }
}
