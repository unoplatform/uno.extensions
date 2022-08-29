
using Uno.Toolkit.UI;

namespace TestHarness.Ext.Navigation.TabBar;

public sealed partial class TabBarHomePage : Page
{
	public TabBarHomePage()
	{
		this.InitializeComponent();
	}



	private void TabBarSelectionChanged(Uno.Toolkit.UI.TabBar sender, Uno.Toolkit.UI.TabBarSelectionChangedEventArgs args)
	{
		CurrentTabBarItemText.Text = (args.NewItem as TabBarItem)?.Content + string.Empty;
	}
}
