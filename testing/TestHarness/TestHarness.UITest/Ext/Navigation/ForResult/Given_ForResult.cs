namespace TestHarness.UITest;

public class Given_ForResult : NavigationTestBase
{
	[Test]
	public async Task When_BackPressed_During_ForResult_Navigation_Should_Complete()
	{
		// This test validates the fix for the race condition where NavigateForResult
		// would hang indefinitely if back navigation occurred before page initialization

		InitTestSection(TestSections.Navigation_ForResult);

		// Wait for the first page to load
		App.WaitElement("NavigateForResultButton");
		App.WaitElement("ForResultStatusText");
		
		// Capture initial state
		var statusBefore = App.Marked("ForResultStatusText").GetDependencyPropertyValue("Text")?.ToString();
		statusBefore.Should().Be("Status: Ready");

		// Start navigation with ForResult
		App.Tap("NavigateForResultButton");
		
		// Wait a moment for navigation to start
		await Task.Delay(200);

		// Quickly press the back button on the NavigationBar
		// This simulates the race condition: pressing back before DataContext completes loading
		App.WaitElement("ForResultSecondPageNavigationBar");
		
		// Tap the back button (MainCommand) of the NavigationBar
		// The NavigationBar raises SystemNavigationManager.BackRequested when back is pressed
		var navBar = App.Marked("ForResultSecondPageNavigationBar");
		
		// On platforms with NavigationBar, the back button is the MainCommand
		// We need to find and tap it quickly before initialization completes
		await Task.Delay(100);
		
		// Try to tap back button - implementation varies by platform
		// For now, we'll use the NavigationBar's back functionality
		try
		{
			// Attempt to tap the back icon/button area (usually on the left)
			var navBarRect = navBar.GetRect();
			App.TapCoordinates(navBarRect.X + 40, navBarRect.CenterY);
		}
		catch
		{
			// Fallback: if coordinate tap fails, try finding back button
			App.Back();
		}

		// Wait for navigation to complete
		await Task.Delay(1000);

		// Verify we're back on the first page
		App.WaitElement("NavigateForResultButton");
		App.WaitElement("ForResultStatusText");

		// The key validation: the status should show completion, not hanging
		var statusAfter = App.Marked("ForResultStatusText").GetDependencyPropertyValue("Text")?.ToString();
		
		// With the fix, the ForResult task completes with None when BackRequested fires
		// Without the fix, it would hang indefinitely and the button would stay disabled
		statusAfter.Should().Contain("Completed", 
			"ForResult navigation should complete when SystemNavigationManager.BackRequested fires");
		
		// Verify the button is re-enabled (proves the task completed)
		var buttonEnabled = App.Marked("NavigateForResultButton").GetDependencyPropertyValue("IsEnabled")?.ToString();
		buttonEnabled.Should().Be("True", 
			"Button should be enabled after ForResult task completes");
		
		// Verify result indicates back navigation
		var resultText = App.Marked("ForResultResultText").GetDependencyPropertyValue("Text")?.ToString();
		resultText.Should().Contain("None", 
			"Result should be None when back navigation happens during ForResult");
	}

	[Test]
	public async Task When_NormalReturn_With_ForResult_Should_Return_Value()
	{
		// This test validates that normal ForResult navigation (with return value) still works

		InitTestSection(TestSections.Navigation_ForResult);

		App.WaitElement("NavigateForResultButton");
		
		// Start navigation with ForResult
		App.Tap("NavigateForResultButton");
		
		// Wait for second page to fully load
		App.WaitElement("ForResultSecondPageNavigationBar");
		App.WaitElement("ForResultSecondPageReturnButton");
		
		// Wait for initialization to complete
		await Task.Delay(2500);
		
		// Return with a result value
		App.Tap("ForResultSecondPageReturnButton");
		
		// Wait for navigation back
		await Task.Delay(1000);
		
		// Verify we're back on first page
		App.WaitElement("NavigateForResultButton");
		
		// Verify we got the result value
		var statusText = App.Marked("ForResultStatusText").GetDependencyPropertyValue("Text")?.ToString();
		statusText.Should().Contain("Completed successfully", 
			"Status should show successful completion");
		
		var resultText = App.Marked("ForResultResultText").GetDependencyPropertyValue("Text")?.ToString();
		resultText.Should().Contain("Result from second page", 
			"Should receive the result value from the second page");
	}
}
