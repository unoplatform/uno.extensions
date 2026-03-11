namespace TestHarness.Ext.Navigation.TabBar.TabBarClearBackStack;

public sealed partial class TabBarClearBackStackTestDialog : ContentDialog
{
	public TabBarClearBackStackTestDialog()
	{
		this.InitializeComponent();
	}

	private async void NavToRoot_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			var nav = this.Navigator()!;
			await nav.NavigateRouteAsync(this, "/Root/Home");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"NavToRoot_Click failed: {ex.Message}");
		}
	}
}
