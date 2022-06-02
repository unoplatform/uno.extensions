namespace TestHarness.Ext.Navigation.PageNavigation;

[TestSectionRoot("Page Navigation",TestSections.PageNavigation, typeof(PageNavigationHostInit))]
public sealed partial class PageNavigationMainPage : BaseTestSectionPage
{
	public PageNavigationMainPage()
	{
		this.InitializeComponent();
	}

	public async void TestClick(object sender, RoutedEventArgs e)
	{
		await NavigationRoot.Navigator()!.ShowMessageDialogAsync(this, "Confirm");
	}

}
