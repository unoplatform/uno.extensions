namespace TestHarness.UITest;

/// <summary>
/// Tests for TabBar back navigation state restoration.
/// See: https://github.com/unoplatform/uno.extensions/issues/3016
/// </summary>
public class Given_TabBar_BackNavigation : NavigationTestBase
{
	/// <summary>
	/// When navigating back from a sibling page to a page with TabBar,
	/// the previously selected tab should be maintained.
	/// 
	/// Steps:
	/// 1. Navigate to Main page (with TabBar containing Second, Third, Fourth)
	/// 2. Navigate to Third route via TabBar
	/// 3. Navigate to Sibling page via button
	/// 4. Navigate back
	/// 5. Verify Third tab is still selected (not reset to default Second)
	/// </summary>
	[Test]
	public async Task When_BackNavigation_TabBar_State_Is_Maintained()
	{
		InitTestSection(TestSections.Navigation_TabBar_BackNavigation);

		// Wait for the Main page to load with default tab (Second)
		App.WaitElement("TabBarBackNavMainNavigationBar");
		App.WaitElement("SecondSection");
		
		// Verify default tab (Second) is selected
		var isSecondVisible = App.Marked("SecondSection").IsVisible();
		isSecondVisible.Should().Be(true, "Second section should be visible by default");

		// Navigate to Third tab via TabBar
		App.WaitThenTap("ThirdTabBarItem");
		
		// Verify Third section is now visible
		App.WaitElement("ThirdSection");
		var isThirdVisible = App.Marked("ThirdSection").IsVisible();
		isThirdVisible.Should().Be(true, "Third section should be visible after selecting tab");
		
		// Navigate to Sibling page
		App.WaitThenTap("GoToSiblingButton");
		
		// Verify we're on the Sibling page
		App.WaitElement("TabBarBackNavSiblingNavigationBar");
		App.WaitElement("SiblingPageText");
		
		// Navigate back to Main page
		App.WaitThenTap("SiblingGoBackButton");
		
		// Verify we're back on Main page
		App.WaitElement("TabBarBackNavMainNavigationBar");
		
		// KEY ASSERTION: Third tab should still be selected (not reset to Second)
		App.WaitElement("ThirdSection");
		isThirdVisible = App.Marked("ThirdSection").IsVisible();
		isThirdVisible.Should().Be(true, "Third section should still be visible after navigating back");
		
		// Verify Second is NOT visible (confirming we didn't reset to default)
		isSecondVisible = App.Marked("SecondSection").IsVisible();
		isSecondVisible.Should().Be(false, "Second section should NOT be visible after navigating back");
	}
	
	/// <summary>
	/// Similar test but navigating to Fourth tab before going to Sibling.
	/// </summary>
	[Test]
	public async Task When_BackNavigation_Fourth_Tab_State_Is_Maintained()
	{
		InitTestSection(TestSections.Navigation_TabBar_BackNavigation);

		// Wait for the Main page to load
		App.WaitElement("TabBarBackNavMainNavigationBar");
		
		// Navigate to Fourth tab via TabBar
		App.WaitThenTap("FourthTabBarItem");
		
		// Verify Fourth section is now visible
		App.WaitElement("FourthSection");
		var isFourthVisible = App.Marked("FourthSection").IsVisible();
		isFourthVisible.Should().Be(true, "Fourth section should be visible after selecting tab");
		
		// Navigate to Sibling page
		App.WaitThenTap("GoToSiblingButton");
		
		// Verify we're on the Sibling page
		App.WaitElement("TabBarBackNavSiblingNavigationBar");
		
		// Navigate back to Main page
		App.WaitThenTap("SiblingGoBackButton");
		
		// Verify we're back on Main page
		App.WaitElement("TabBarBackNavMainNavigationBar");
		
		// KEY ASSERTION: Fourth tab should still be selected
		App.WaitElement("FourthSection");
		isFourthVisible = App.Marked("FourthSection").IsVisible();
		isFourthVisible.Should().Be(true, "Fourth section should still be visible after navigating back");
		
		// Verify Second is NOT visible (confirming we didn't reset to default)
		var isSecondVisible = App.Marked("SecondSection").IsVisible();
		isSecondVisible.Should().Be(false, "Second section should NOT be visible after navigating back");
	}
}
