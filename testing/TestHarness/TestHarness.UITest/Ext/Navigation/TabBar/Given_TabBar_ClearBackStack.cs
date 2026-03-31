namespace TestHarness.UITest;

/// <summary>
/// Tests for navigating back to a tabbed root page from a deeply nested page
/// using various approaches (ClearBackStack, NavigateBackAsync, etc.).
/// 
/// The issue: When on a page that was forward-navigated from within a tab
/// (e.g. Root/TabTwo -> Details), attempting to navigate back to Root/Home
/// using ClearBackStack or similar qualifiers creates a new Home page without
/// the TabBar, instead of returning to the existing tabbed Root page.
/// </summary>
public class Given_TabBar_ClearBackStack : NavigationTestBase
{
	private void NavigateToDetails()
	{
		InitTestSection(TestSections.Navigation_TabBar_ClearBackStack);

		// Wait for Root page with TabBar to load
		App.WaitElement("ClearBackStackRootNavigationBar");
		App.WaitElement("ClearBackStackTabBar");
		App.WaitElement("HomeSection");

		// Switch to TabTwo tab
		App.WaitThenTap("TabTwoTabBarItem");
		App.WaitElement("TabTwoSection");

		// Navigate to Details (forward navigation, leaves tabbed root)
		App.WaitThenTap("GoToDetailsButton");
		App.WaitElement("DetailNavigationBar");
	}

	private void AssertBackAtTabbedRoot()
	{
		// KEY ASSERTIONS: We should be back on the Root page WITH the TabBar visible
		App.WaitElement("ClearBackStackRootNavigationBar");
		App.WaitElement("ClearBackStackTabBar");

		// The Home tab content should be visible
		App.WaitElement("HomeSection");
		var isHomeVisible = App.Marked("HomeSection").IsVisible();
		isHomeVisible.Should().Be(true, "Home section should be visible inside the tabbed Root page");

		// The TabBar should be visible (this is the key check - if navigation
		// created a new Home page, the TabBar won't be there)
		var isTabBarVisible = App.Marked("ClearBackStackTabBar").IsVisible();
		isTabBarVisible.Should().Be(true, "TabBar should be visible, meaning we're on the Root page not a standalone Home page");
	}

	/// <summary>
	/// Test 1: NavigateRouteAsync with absolute route "/Root/Home"
	/// </summary>
	[Test]
	public async Task When_NavigateRoute_Absolute_From_Deep_Page()
	{
		NavigateToDetails();
		App.WaitThenTap("NavTest1Button");
		AssertBackAtTabbedRoot();
	}

	/// <summary>
	/// Test 2: NavigateRouteAsync with clear-prefix "-/Root/Home"
	/// </summary>
	[Test]
	public async Task When_NavigateRoute_ClearPrefix_From_Deep_Page()
	{
		NavigateToDetails();
		App.WaitThenTap("NavTest2Button");
		AssertBackAtTabbedRoot();
	}

	/// <summary>
	/// Test 3: NavigateRouteAsync "/Root/Home" + Qualifiers.ClearBackStack
	/// </summary>
	[Test]
	public async Task When_NavigateRoute_ClearBackStack_From_Deep_Page()
	{
		NavigateToDetails();
		App.WaitThenTap("NavTest3Button");
		AssertBackAtTabbedRoot();
	}

	/// <summary>
	/// Test 4: NavigateRouteAsync "-/Root/Home" + Qualifiers.ClearBackStack
	/// </summary>
	[Test]
	public async Task When_NavigateRoute_ClearPrefix_ClearBackStack_From_Deep_Page()
	{
		NavigateToDetails();
		App.WaitThenTap("NavTest4Button");
		AssertBackAtTabbedRoot();
	}

	/// <summary>
	/// Test 5: NavigateBackAsync(Root) then NavigateRouteAsync "/Root/Home"
	/// </summary>
	[Test]
	public async Task When_NavigateBack_Root_Then_Route_From_Deep_Page()
	{
		NavigateToDetails();
		App.WaitThenTap("NavTest5Button");
		AssertBackAtTabbedRoot();
	}

	/// <summary>
	/// Test: Standard back navigation from Details should return to Root/TabTwo tab
	/// (baseline test to verify the test setup works)
	/// </summary>
	[Test]
	public async Task When_StandardBack_From_Deep_Page()
	{
		NavigateToDetails();
		App.WaitThenTap("DetailGoBackButton");

		// Should be back on Root page with TabTwo tab still selected
		App.WaitElement("ClearBackStackRootNavigationBar");
		App.WaitElement("ClearBackStackTabBar");
		App.WaitElement("TabTwoSection");

		var isTabBarVisible = App.Marked("ClearBackStackTabBar").IsVisible();
		isTabBarVisible.Should().Be(true, "TabBar should be visible after standard back navigation");

		var isTabTwoVisible = App.Marked("TabTwoSection").IsVisible();
		isTabTwoVisible.Should().Be(true, "TabTwo section should be visible since we went back from Details");
	}

	/// <summary>
	/// Test 6: Open a ContentDialog from the deep page, then navigate to /Root/Home
	/// from inside the dialog. The dialog should be dismissed and we should end up
	/// at the tabbed root.
	/// Reproduces: ContentDialog not closed during root navigation
	/// </summary>
	[Test]
	public async Task When_ContentDialog_Open_And_Navigate_Root()
	{
		NavigateToDetails();

		// Open the ContentDialog
		App.WaitThenTap("NavTest6ShowDialogButton");

		// Wait for the dialog to appear
		App.WaitElement("DialogContentText");
		App.WaitElement("DialogNavToRootButton");

		// Navigate to /Root/Home from inside the dialog
		App.WaitThenTap("DialogNavToRootButton");

		// The dialog should be dismissed and we should be at the tabbed root
		AssertBackAtTabbedRoot();

		// Verify the dialog is actually dismissed (not just hidden behind the root page).
		// Use WaitForNoElement to allow time for the dialog overlay to be removed.
		App.WaitForNoElement("DialogContentText", "ContentDialog content should be dismissed after navigating to root");
		App.WaitForNoElement("DialogNavToRootButton", "ContentDialog button should be dismissed after navigating to root");
	}

	/// <summary>
	/// Test 7: Open a passive ContentDialog from the Home tab (no navigation button inside it),
	/// then trigger root navigation externally from the page behind the dialog.
	/// This simulates a real-world scenario where a root-level ViewModel (e.g. handling
	/// "logged out on another device") calls NavigateRouteAsync("/Root/Home") while a
	/// ContentDialog is open on top.
	/// The dialog should be closed and we should end up at the tabbed root.
	/// </summary>
	[Test]
	public async Task When_ContentDialog_Open_And_External_Root_Navigation()
	{
		InitTestSection(TestSections.Navigation_TabBar_ClearBackStack);

		// Wait for Root page with TabBar and Home tab to load
		App.WaitElement("ClearBackStackRootNavigationBar");
		App.WaitElement("ClearBackStackTabBar");
		App.WaitElement("HomeSection");

		// Open the passive dialog and schedule external root navigation after a delay.
		// A single button is used because the ContentDialog overlay blocks taps to
		// buttons on the page behind it.
		App.WaitThenTap("HomeShowDialogThenNavExternallyButton");
		App.WaitElement("PassiveDialogContentText");

		// The dialog should be dismissed and we should still be at the tabbed root
		AssertBackAtTabbedRoot();

		// Verify the dialog overlay is actually gone
		App.WaitForNoElement("PassiveDialogContentText", "Passive ContentDialog should be dismissed when root navigation is triggered externally");
	}

	/// <summary>
	/// Test 8: Same as Test 7 but the external navigation uses Qualifiers.ClearBackStack,
	/// which produces a route whose qualifier starts with '-' (e.g. "-/Root/Home").
	/// This is a regression test for the use of FrameIsBackNavigation() in
	/// ClosableNavigator.ExecuteRequestAsync: using IsBackOrCloseNavigation() instead would
	/// have matched this '-' prefix and incorrectly deregistered the dialog from the source
	/// region before CloseActiveClosableNavigators could find and close it.
	/// </summary>
	[Test]
	public async Task When_ContentDialog_Open_And_External_Root_Navigation_ClearBackStack()
	{
		InitTestSection(TestSections.Navigation_TabBar_ClearBackStack);

		// Wait for Root page with TabBar and Home tab to load
		App.WaitElement("ClearBackStackRootNavigationBar");
		App.WaitElement("ClearBackStackTabBar");
		App.WaitElement("HomeSection");

		// Open the passive dialog and schedule external root navigation (with ClearBackStack)
		// after a delay. A single button is used because the ContentDialog overlay blocks
		// taps to buttons on the page behind it.
		App.WaitThenTap("HomeShowDialogThenNavExternallyClearBackStackButton");
		App.WaitElement("PassiveDialogContentText");

		// The dialog should be dismissed and we should still be at the tabbed root
		AssertBackAtTabbedRoot();

		// Verify the dialog overlay is actually gone
		App.WaitForNoElement("PassiveDialogContentText", "Passive ContentDialog should be dismissed when root navigation with ClearBackStack is triggered externally");
	}
}
