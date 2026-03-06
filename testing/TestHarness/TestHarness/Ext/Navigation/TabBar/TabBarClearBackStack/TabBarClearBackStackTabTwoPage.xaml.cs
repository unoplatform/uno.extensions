namespace TestHarness.Ext.Navigation.TabBar.TabBarClearBackStack;

public sealed partial class TabBarClearBackStackTabTwoPage : Page
{
	public TabBarClearBackStackTabTwoPage()
	{
		this.InitializeComponent();
		NavigationCacheMode = NavigationCacheMode.Required;
	}

	private async void GoToDetails_Click(object sender, RoutedEventArgs e)
	{
		var nav = this.Navigator()!;
		await nav.NavigateViewModelForResultAsync<TabBarClearBackStackDetailModel, string>(this, data: "test-stop-data");
	}
}
