namespace TestHarness.Ext.Navigation.TabBar.TabBarClearBackStack;

/// <summary>
/// Shell ViewModel that navigates to Root on startup.
/// Root is navigated to explicitly (no IsDefault on the Root route).
/// </summary>
public class TabBarClearBackStackShellModel
{
	private readonly INavigator _navigator;

	public TabBarClearBackStackShellModel(INavigator navigator)
	{
		_navigator = navigator;
		_ = Start();
	}

	private async Task Start()
	{
		// Navigate to Root explicitly (no IsDefault on Root route)
		await _navigator.NavigateViewAsync<TabBarClearBackStackRootPage>(this, Qualifiers.Root);
	}
}
