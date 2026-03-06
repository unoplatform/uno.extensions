namespace TestHarness.Ext.Navigation.TabBar.TabBarClearBackStack;

/// <summary>
/// Shell ViewModel that navigates to Root on startup.
/// Mirrors the driver app's ShellViewModel which calls:
///   await _navigator.NavigateViewAsync&lt;RootPage&gt;(this, Qualifiers.Root);
/// This is important because the driver app does NOT use IsDefault on the Root route.
/// Instead, Root is navigated to explicitly by the ShellViewModel.
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
		// Navigate to Root explicitly, just like the driver app's ShellViewModel does:
		// await _navigator.NavigateViewAsync<RootPage>(this, Qualifiers.Root);
		await _navigator.NavigateViewAsync<TabBarClearBackStackRootPage>(this, Qualifiers.Root);
	}
}
