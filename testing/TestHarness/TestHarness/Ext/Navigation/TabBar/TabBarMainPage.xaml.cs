namespace TestHarness.Ext.Navigation.TabBar;

[TestSectionRoot("TabBar",TestSections.Navigation_TabBar, typeof(TabBarHostInit))]
public sealed partial class TabBarMainPage : BaseTestSectionPage
{
	public TabBarMainPage()
	{
		this.InitializeComponent();
	}

	public async void TabBarHomeClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<TabBarHomeViewModel>(this);
	}

	public async void TabBarListClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<TabBarListViewModel>(this);
	}
}
