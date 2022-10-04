namespace TestHarness.Ext.Navigation.PageNavigation;

[TestSectionRoot("Page Navigation",TestSections.Navigation_PageNavigation, typeof(PageNavigationHostInit))]
[TestSectionRoot("Page Navigation - Registerd Routes", TestSections.Navigation_PageNavigationRegistered, typeof(PageNavigationRegisterHostInit))]
public sealed partial class PageNavigationMainPage : BaseTestSectionPage
{
	public PageNavigationMainPage()
	{
		this.InitializeComponent();
	}

	public async void OnePageClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<PageNavigationOneViewModel>(this);
	}

}
