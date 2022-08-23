namespace TestHarness.Ext.Navigation.PageNavigation;

[TestSectionRoot("Page Navigation",TestSections.PageNavigation, typeof(PageNavigationHostInit))]
[TestSectionRoot("Page Navigation - Registerd Routes", TestSections.PageNavigationRegistered, typeof(PageNavigationRegisterHostInit))]
public sealed partial class PageNavigationMainPage : BaseTestSectionPage
{
	public PageNavigationMainPage()
	{
		this.InitializeComponent();
	}

	public async void OnePageClick(object sender, RoutedEventArgs e)
	{
		await NavigationRoot.Navigator()!.NavigateViewModelAsync<PageNavigationOneViewModel>(this);
	}

}
