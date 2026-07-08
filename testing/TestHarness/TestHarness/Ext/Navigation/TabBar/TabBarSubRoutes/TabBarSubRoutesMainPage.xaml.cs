namespace TestHarness.Ext.Navigation.TabBar;

[TestSectionRoot("TabBar Sub-Routes", TestSections.Navigation_TabBar_SubRoutes, typeof(TabBarSubRoutesHostInit))]
public sealed partial class TabBarSubRoutesMainPage : BaseTestSectionPage
{
	public TabBarSubRoutesMainPage()
	{
		this.InitializeComponent();
	}

	public async void ShowTabBarSubRoutesHomeClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<TabBarSubRoutesHomeViewModel>(this);
	}
}
