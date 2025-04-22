namespace TestHarness.UITest;

public class Given_Apps_ToDo : NavigationTestBase
{
	[Test]
	public async Task When_ToDo_Responsive()
	{
		InitTestSection(TestSections.Apps_ToDo);

		App.WaitThenTap("ShowAppButton");

		// Select the narrow layout
		App.WaitThenTap("NarrowButton");

		// Make sure the app has loaded
		App.WaitElement("WelcomeNavigationBar");

		// NavToHome
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

	[Test]
	public async Task When_ToDo_Wide_Nav_ContentControl()
	{
		InitTestSection(TestSections.Apps_ToDo);

		App.WaitThenTap("ShowAppButton");

		// Make sure the app has loaded
		App.WaitElement("WelcomeNavigationBar");

		// NavToHome
		await App.TapAndWait("LoginButton", "HomeNavigationBar");

		// Select a task list
		await App.TapAndWait("SelectTaskList2Button", "TaskListNavigationBar");

		await App.TapAndWait("SelectActiveTask1Button", "TaskNavigationBar");

		var screenBefore = TakeScreenshot("When_ToDo_Wide_Nav_ContentControl_Before");

		await App.TapAndWait("DetailsBackButton", "TaskListNavigationBar");

		var screenAfter = TakeScreenshot("When_ToDo_Wide_Nav_ContentControl_After");

		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));
	}

	[Test]
	public async Task When_ToDo_Narrow_Nav_ContentControl()
	{
		InitTestSection(TestSections.Apps_ToDo);

		App.WaitThenTap("ShowAppButton");

		App.WaitThenTap("NarrowButton");

		// Make sure the app has loaded
		App.WaitElement("WelcomeNavigationBar");

		// NavToHome
		await App.TapAndWait("LoginButton", "HomeNavigationBar");

		// Select a task list
		await App.TapAndWait("SelectTaskList2Button", "TaskListNavigationBar");

		await App.TapAndWait("SelectActiveTask1Button", "TaskNavigationBar");

		await App.TapAndWait("DetailsBackButton", "TaskListNavigationBar");
	}
}
