namespace TestHarness.UITest;

public class Given_Responsive : NavigationTestBase
{
	[Test]
	public async Task When_Responsive()
	{
		InitTestSection(TestSections.Responsive);

		App.WaitThenTap("ShowResponsiveHomeButton");

		App.WaitForElement("ResponsiveHomeNavigationBar");


		var widgetsListView = App.Marked("WidgetsListView");
		App.WaitForElement(widgetsListView);

		// Select the narrow layout
		// Navigation to the selected item should do a forward navigation
		// on the frame, pushing the list page into the backstack
		// (ie no longer visible)
		App.WaitThenTap("NarrowButton");

		widgetsListView.SetDependencyPropertyValue("SelectedIndex", "1");

		// Make sure that the listview isn't visible by querying a depedency property
		// If on screen, this should return a non-null value
		var visibility = widgetsListView.GetDependencyPropertyValue("Visibility");
		visibility.Should().BeNull();

		App.WaitThenTap("DetailsBackButton");

		await Task.Yield();

		widgetsListView = App.Marked("WidgetsListView");
		visibility = widgetsListView.GetDependencyPropertyValue("Visibility");
		visibility.Should().NotBeNull();


		// Select the wide layout
		// Navigation to the selected item should do push the details
		// into the contentcontrol to the right of the list
		// (ie list is still visible)
		App.WaitThenTap("WideButton");

		await Task.Yield();

		widgetsListView.SetDependencyPropertyValue("SelectedIndex", "1");

		// List should still be visible (since details should be in contentcontrol)
		visibility = widgetsListView.GetDependencyPropertyValue("Visibility");
		visibility.Should().NotBeNull();

		await Task.Delay(5000);
		var screenBefore = TakeScreenshot("When_Responsive_Before");
		App.WaitThenTap("DetailsBackButton");
		await Task.Delay(5000);
		var screenAfter = TakeScreenshot("When_Responsive_After");
		// TODO: Fix image comparison
		//ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));
	}

}
