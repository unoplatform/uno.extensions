namespace TestHarness.UITest;

/// <summary>
/// Tests for navigating back to a tabbed root page from a deeply nested page
/// using various approaches (ClearBackStack, NavigateBackAsync, etc.).
/// 
/// Reproduces: https://github.com/unoplatform/uno.extensions/issues/72
/// 
/// The issue: When on a page that was forward-navigated from within a tab
/// (e.g. Root/MyRun -> StopDetails), attempting to navigate back to Root/Home
/// using ClearBackStack or similar qualifiers creates a new Home page without
/// the TabBar, instead of returning to the existing tabbed Root page.
/// </summary>
public class Given_TabBar_ClearBackStack : NavigationTestBase
{
	private void NavigateToStopDetails()
	{
		InitTestSection(TestSections.Navigation_TabBar_ClearBackStack);

		// Wait for Root page with TabBar to load
		App.WaitElement("ClearBackStackRootNavigationBar");
		App.WaitElement("ClearBackStackTabBar");
		App.WaitElement("HomeSection");

		// Switch to MyRun tab
		App.WaitThenTap("MyRunTabBarItem");
		App.WaitElement("MyRunSection");

		// Navigate to StopDetails (forward navigation, leaves tabbed root)
		App.WaitThenTap("GoToStopDetailsButton");
		App.WaitElement("StopDetailNavigationBar");
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

		// StopDetails should NOT be visible
		var isStopDetailVisible = App.Marked("StopDetailNavigationBar").IsVisible();
		isStopDetailVisible.Should().Be(false, "StopDetails page should no longer be visible");
	}

	/// <summary>
	/// Test 1: NavigateRouteAsync with absolute route "/Root/Home"
	/// </summary>
	[Test]
	public async Task When_NavigateRoute_Absolute_From_Deep_Page()
	{
		NavigateToStopDetails();
		App.WaitThenTap("NavTest1Button");
		AssertBackAtTabbedRoot();
	}

	/// <summary>
	/// Test 2: NavigateRouteAsync with clear-prefix "-/Root/Home"
	/// </summary>
	[Test]
	public async Task When_NavigateRoute_ClearPrefix_From_Deep_Page()
	{
		NavigateToStopDetails();
		App.WaitThenTap("NavTest2Button");
		AssertBackAtTabbedRoot();
	}

	/// <summary>
	/// Test 3: NavigateRouteAsync "/Root/Home" + Qualifiers.ClearBackStack
	/// </summary>
	[Test]
	public async Task When_NavigateRoute_ClearBackStack_From_Deep_Page()
	{
		NavigateToStopDetails();
		App.WaitThenTap("NavTest3Button");
		AssertBackAtTabbedRoot();
	}

	/// <summary>
	/// Test 4: NavigateRouteAsync "-/Root/Home" + Qualifiers.ClearBackStack
	/// </summary>
	[Test]
	public async Task When_NavigateRoute_ClearPrefix_ClearBackStack_From_Deep_Page()
	{
		NavigateToStopDetails();
		App.WaitThenTap("NavTest4Button");
		AssertBackAtTabbedRoot();
	}

	/// <summary>
	/// Test 5: NavigateBackAsync(Root) then NavigateRouteAsync "/Root/Home"
	/// </summary>
	[Test]
	public async Task When_NavigateBack_Root_Then_Route_From_Deep_Page()
	{
		NavigateToStopDetails();
		App.WaitThenTap("NavTest5Button");
		AssertBackAtTabbedRoot();
	}

	/// <summary>
	/// Test 6: NavigateViewModelAsync with ClearBackStack qualifier
	/// </summary>
	[Test]
	public async Task When_NavigateViewModel_ClearBackStack_From_Deep_Page()
	{
		NavigateToStopDetails();
		App.WaitThenTap("NavTest6Button");
		AssertBackAtTabbedRoot();
	}

	/// <summary>
	/// Test 7: NavigateBackAsync to Root only (should return to Root with last selected tab)
	/// </summary>
	[Test]
	public async Task When_NavigateBack_Root_From_Deep_Page()
	{
		NavigateToStopDetails();
		App.WaitThenTap("NavTest7Button");

		// For this test, we should be back at Root with the TabBar
		// The MyRun tab should still be selected since that's where we were
		App.WaitElement("ClearBackStackRootNavigationBar");
		App.WaitElement("ClearBackStackTabBar");

		var isTabBarVisible = App.Marked("ClearBackStackTabBar").IsVisible();
		isTabBarVisible.Should().Be(true, "TabBar should be visible after navigating back to root");
	}

	/// <summary>
	/// Test: Standard back navigation from StopDetails should return to Root/MyRun tab
	/// (baseline test to verify the test setup works)
	/// </summary>
	[Test]
	public async Task When_StandardBack_From_Deep_Page()
	{
		NavigateToStopDetails();
		App.WaitThenTap("StopDetailGoBackButton");

		// Should be back on Root page with MyRun tab still selected
		App.WaitElement("ClearBackStackRootNavigationBar");
		App.WaitElement("ClearBackStackTabBar");
		App.WaitElement("MyRunSection");

		var isTabBarVisible = App.Marked("ClearBackStackTabBar").IsVisible();
		isTabBarVisible.Should().Be(true, "TabBar should be visible after standard back navigation");

		var isMyRunVisible = App.Marked("MyRunSection").IsVisible();
		isMyRunVisible.Should().Be(true, "MyRun section should be visible since we went back from StopDetails");
	}
}
