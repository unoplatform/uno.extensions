namespace TestHarness.Ext.Navigation.RoutesNavigation;

[TestSectionRoot("Routes Navigation", TestSections.Navigation_RoutesNavigation, typeof(RoutesNavigationHostInit))]
public sealed partial class RoutesNavigationMainPage : BaseTestSectionPage
{
	public RoutesNavigationMainPage()
	{
		this.InitializeComponent();
	}

	public async void ShowAppClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateRouteAsync(this, "");
	}
}
