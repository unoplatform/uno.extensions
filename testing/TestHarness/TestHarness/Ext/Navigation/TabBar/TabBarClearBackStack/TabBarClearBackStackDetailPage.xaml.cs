namespace TestHarness.Ext.Navigation.TabBar.TabBarClearBackStack;

public sealed partial class TabBarClearBackStackDetailPage : Page
{
	public TabBarClearBackStackDetailPage()
	{
		this.InitializeComponent();
	}

	private async void NavTest1_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			// Approach 1: absolute route
			var nav = this.Navigator()!;
			await nav.NavigateRouteAsync(this, "/Root/Home");
		}
		catch (Exception ex)
		{
			StatusText.Text = $"Test 1 failed: {ex.Message}";
		}
	}

	private async void NavTest2_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			// Approach 2: clear-prefix route with dash
			var nav = this.Navigator()!;
			await nav.NavigateRouteAsync(this, "-/Root/Home");
		}
		catch (Exception ex)
		{
			StatusText.Text = $"Test 2 failed: {ex.Message}";
		}
	}

	private async void NavTest3_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			// Approach 3: absolute route + ClearBackStack qualifier
			var nav = this.Navigator()!;
			await nav.NavigateRouteAsync(this, "/Root/Home", Qualifiers.ClearBackStack);
		}
		catch (Exception ex)
		{
			StatusText.Text = $"Test 3 failed: {ex.Message}";
		}
	}

	private async void NavTest4_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			// Approach 4: clear-prefix + ClearBackStack qualifier
			var nav = this.Navigator()!;
			await nav.NavigateRouteAsync(this, "-/Root/Home", Qualifiers.ClearBackStack);
		}
		catch (Exception ex)
		{
			StatusText.Text = $"Test 4 failed: {ex.Message}";
		}
	}

	private async void NavTest5_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			// Approach 5: navigate back to root first, then navigate to Home
			var nav = this.Navigator()!;
			await nav.NavigateBackAsync(this, Qualifiers.Root);
			await nav.NavigateRouteAsync(this, "/Root/Home");
		}
		catch (Exception ex)
		{
			StatusText.Text = $"Test 5 failed: {ex.Message}";
		}
	}

	private async void NavTest6_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			// Approach 6: open a ContentDialog, then navigate to root from inside it.
			// The dialog should be dismissed automatically when root navigation occurs.
			var nav = this.Navigator()!;
			await nav.NavigateViewAsync<TabBarClearBackStackTestDialog>(this, Qualifiers.Dialog);
		}
		catch (Exception ex)
		{
			StatusText.Text = $"Test 6 failed: {ex.Message}";
		}
	}

}
