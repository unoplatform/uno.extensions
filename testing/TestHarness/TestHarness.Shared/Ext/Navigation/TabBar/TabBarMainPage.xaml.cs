namespace TestHarness.Ext.Navigation.TabBar;

[TestSectionRoot("TabBar",TestSections.TabBar, typeof(TabBarHostInit))]
public sealed partial class TabBarMainPage : BaseTestSectionPage
{
	public TabBarMainPage()
	{
		this.InitializeComponent();
	}

	public async void TabBarHomeClick(object sender, RoutedEventArgs e)
	{
		await NavigationRoot.Navigator()!.NavigateViewModelAsync<TabBarHomeViewModel>(this);
	}

}
