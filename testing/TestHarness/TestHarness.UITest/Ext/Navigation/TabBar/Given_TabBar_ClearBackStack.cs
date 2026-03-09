namespace TestHarness.UITest;

/// <summary>
/// Tests for navigating back to a tabbed root page from a deeply nested page
/// using various approaches (ClearBackStack, NavigateBackAsync, etc.).
/// 
/// Reproduces: https://github.com/unoplatform/uno.extensions/issues/72
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
}
