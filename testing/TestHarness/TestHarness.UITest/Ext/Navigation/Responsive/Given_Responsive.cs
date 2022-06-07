using System.Diagnostics;
using FluentAssertions;

namespace TestHarness.UITest;

public class Given_Responsive : NavigationTestBase
{
	[Test]
	public void When_Responsive()
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

		visibility = widgetsListView.GetDependencyPropertyValue("Visibility");
		visibility.Should().NotBeNull();


		// Select the wide layout
		// Navigation to the selected item should do push the details
		// into the contentcontrol to the right of the list
		// (ie list is still visible)
		App.WaitThenTap("WideButton");

		widgetsListView.SetDependencyPropertyValue("SelectedIndex", "1");

		// List should still be visible (since details should be in contentcontrol)
		visibility = widgetsListView.GetDependencyPropertyValue("Visibility");
		visibility.Should().NotBeNull();

	}

}
