#if WINDOWS_UWP 
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

using System.Linq;
using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.Controls;

public class TabWrapper : ITabWrapper
{
    public TabView Tabs { get; set; }

    public bool ActivateTab(string tabName)
    {
        var tab = (from t in Tabs.TabItems.OfType<TabViewItem>()
                   where t.Name == tabName
                   select t).FirstOrDefault();
        if (tab is not null)
        {
            Tabs.SelectedItem = tab;
            return true;
        }
        return false;
    }
}
