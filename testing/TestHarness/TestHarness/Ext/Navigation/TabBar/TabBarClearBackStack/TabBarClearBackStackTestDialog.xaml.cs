namespace TestHarness.Ext.Navigation.TabBar.TabBarClearBackStack;

public sealed partial class TabBarClearBackStackTestDialog : ContentDialog
{
	public TabBarClearBackStackTestDialog()
	{
		this.InitializeComponent();
	}

	private async void NavToRoot_Click(object sender, RoutedEventArgs e)
	{
		var nav = this.Navigator()!;
		await nav.NavigateRouteAsync(this, "/Root/Home");
	}
}
