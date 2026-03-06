namespace TestHarness.Ext.Navigation.TabBar.TabBarClearBackStack;

public sealed partial class TabBarClearBackStackStopDetailPage : Page
{
	public TabBarClearBackStackStopDetailPage()
	{
		NavigationCacheMode = NavigationCacheMode.Required;
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
			// Approach 6: ViewModel-based navigation with ClearBackStack
			var nav = this.Navigator()!;
			await nav.NavigateViewModelAsync<TabBarClearBackStackHomeModel>(this, qualifier: Qualifiers.ClearBackStack);
		}
		catch (Exception ex)
		{
			StatusText.Text = $"Test 6 failed: {ex.Message}";
		}
	}

	private async void NavTest7_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			// Approach 7: just NavigateBackAsync to Root
			var nav = this.Navigator()!;
			await nav.NavigateBackAsync(this, Qualifiers.Root);
		}
		catch (Exception ex)
		{
			StatusText.Text = $"Test 7 failed: {ex.Message}";
		}
	}
}
