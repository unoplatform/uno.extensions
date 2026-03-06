namespace TestHarness.Ext.Navigation.TabBar.TabBarClearBackStack;

public sealed partial class TabBarClearBackStackMyRunPage : Page
{
	public TabBarClearBackStackMyRunPage()
	{
		this.InitializeComponent();
		NavigationCacheMode = NavigationCacheMode.Required;
	}

	private async void GoToStopDetails_Click(object sender, RoutedEventArgs e)
	{
		// Navigate via NavigateViewModelForResultAsync like the driver app does:
		// Navigator.NavigateViewModelForResultAsync<StopDetailsViewModel, StopItem>(this, data: options)
		var nav = this.Navigator()!;
		await nav.NavigateViewModelForResultAsync<TabBarClearBackStackStopDetailModel, string>(this, data: "test-stop-data");
	}
}
