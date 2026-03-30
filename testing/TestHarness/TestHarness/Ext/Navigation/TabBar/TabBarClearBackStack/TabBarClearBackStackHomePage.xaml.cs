namespace TestHarness.Ext.Navigation.TabBar.TabBarClearBackStack;

public sealed partial class TabBarClearBackStackHomePage : Page
{
	public TabBarClearBackStackHomePage()
	{
		this.InitializeComponent();
	}

	/// <summary>
	/// Opens a passive ContentDialog (one that has no internal navigation button).
	/// Used to test that the dialog is closed when root navigation is triggered
	/// externally from the page behind it.
	/// </summary>
	private async void ShowPassiveDialog_Click(object sender, RoutedEventArgs e)
	{
		var nav = this.Navigator()!;
		await nav.NavigateViewAsync<TabBarClearBackStackPassiveDialog>(this, Qualifiers.Dialog);
	}

	/// <summary>
	/// Triggers root navigation from outside any open dialog, simulating an external
	/// event such as "logged out on another device" handled by a root-level ViewModel.
	/// If a ContentDialog is open, this should close it and navigate to root.
	/// </summary>
	private async void NavToRootExternally_Click(object sender, RoutedEventArgs e)
	{
		var nav = this.Navigator()!;
		await nav.NavigateRouteAsync(this, "/Root/Home");
	}

	/// <summary>
	/// Same as <see cref="NavToRootExternally_Click"/> but uses
	/// <see cref="Qualifiers.ClearBackStack"/>, which produces a route whose qualifier
	/// starts with '-' (but has a non-empty Base).  Used to verify that
	/// <c>FrameIsBackNavigation()</c> correctly excludes this route from the
	/// "pure back/close" branch in <c>ClosableNavigator.ExecuteRequestAsync</c>.
	/// </summary>
	private async void NavToRootExternallyClearBackStack_Click(object sender, RoutedEventArgs e)
	{
		var nav = this.Navigator()!;
		await nav.NavigateRouteAsync(this, "/Root/Home", Qualifiers.ClearBackStack);
	}
}
