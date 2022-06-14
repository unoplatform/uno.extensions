namespace TestHarness.UITest;

public class Given_Apps_ToDo : NavigationTestBase
{
	[Test]
	public async Task When_Responsive()
	{
		InitTestSection(TestSections.Apps_ToDo);

		App.WaitThenTap("ShowAppButton");

		// Select the narrow layout
		App.WaitThenTap("NarrowButton");

		// Make sure the app has loaded
		App.WaitForElement("WelcomeNavigationBar");

		// Login
		await App.TapAndWait("LoginButton", "HomeNavigationBar");

		// Select a task list

		await App.TapAndWait("SelectTaskList2Button", "TaskListNavigationBar");

		await App.TapAndWait("SelectActiveTask1Button", "TaskNavigationBar");

		await App.TapAndWait("DetailsBackButton", "TaskListNavigationBar");

		await App.TapAndWait("TaskListBackButton", "HomeNavigationBar");


		// Select the narrow layout
		App.WaitThenTap("WideButton");

		// Select a task list

		await App.TapAndWait("SelectTaskList3Button", "TaskListNavigationBar");

		await App.TapAndWait("SelectActiveTask2Button", "TaskNavigationBar");

	}

}
