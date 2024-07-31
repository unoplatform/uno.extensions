namespace TestHarness.Ext.Navigation.NavigationView;

[TestSectionRoot("NavigationView", TestSections.Navigation_NavigationView, typeof(NavigationViewHostInit))]
public sealed partial class NavigationViewMainPage : BaseTestSectionPage
{
	public NavigationViewMainPage()
	{
		this.InitializeComponent();
	}

	public async void NavigationViewHomeClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<NavigationViewHomeViewModel>(this);
	}
	public async void NavigationViewDataBoundClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<NavigationViewDataBoundViewModel>(this);
	}
	public async void NavigationViewDataClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<NavigationViewDataViewModel>(this);
	}
}
