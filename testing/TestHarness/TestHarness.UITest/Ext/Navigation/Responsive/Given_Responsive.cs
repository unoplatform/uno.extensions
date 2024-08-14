namespace TestHarness.UITest;

public class Given_Responsive : NavigationTestBase
{
	[Test]
	public async Task When_Navigation_Responsive()
	{
		InitTestSection(TestSections.Navigation_Responsive);

		App.WaitThenTap("ShowResponsiveHomeButton");

		App.WaitElement("ResponsiveHomeNavigationBar");


		var widgetsListView = App.MarkedAnywhere("WidgetsListView");
		App.WaitElement("WidgetsListView");

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

		widgetsListView = App.MarkedAnywhere("WidgetsListView");
		visibility = widgetsListView.WaitUntilExists().GetDependencyPropertyValue("Visibility");
		visibility.Should().NotBeNull();


		// Select the wide layout
		// Navigation to the selected item should do push the details
		// into the contentcontrol to the right of the list
		// (ie list is still visible)
		App.WaitThenTap("WideButton");

		await Task.Yield();

		widgetsListView.SetDependencyPropertyValue("SelectedIndex", "2");

		// List should still be visible (since details should be in contentcontrol)
		visibility = widgetsListView.GetDependencyPropertyValue("Visibility");
		visibility.Should().NotBeNull();
	}

}
