namespace TestHarness.UITest;

public class Given_FlyoutDialog : NavigationTestBase
{
	[TestCase("FlyoutDialogXamlButton", 0, false)]
	[TestCase("FlyoutDialogCodebehindButton", 0, false)]
	[TestCase("FlyoutDialogCodebehindBackgroundButton", 0, false)]
	[TestCase("FlyoutDialogCodebehindWithCancelButton", 0, false)]
	[TestCase("FlyoutDialogCodebehindWithCancelButton", 3, true)]
	public async Task When_FlyoutsButton(string dialogButton, int delayInSeconds, bool dialogCancelled)
	{
		InitTestSection(TestSections.Navigation_Dialogs);

		App.WaitThenTap("FlyoutsButton");

		App.WaitElement("DialogsFlyoutsPage");
		var screenBefore = TakeScreenshot("When_Dialog_Before");
		App.Tap(dialogButton);
		await Task.Delay(1000); // Make sure the dialog is showing completely
		var screenAfter = TakeScreenshot("When_Dialog_After");
		ImageAssert.AreNotEqual(screenBefore, screenAfter);

		if (delayInSeconds > 0)
		{
			await Task.Delay(delayInSeconds * 1000);
			var screenAfterDelay = TakeScreenshot("When_Dialog_After_Delay");
			if (dialogCancelled)
			{
				ImageAssert.AreNotEqual(screenAfter, screenAfterDelay);
			}
			else
			{
				ImageAssert.AreEqual(screenAfter, screenAfterDelay, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

			}
		}

		if (!dialogCancelled)
		{
			App.WaitThenTap("DialogsFlyoutDialogCloseButton");
		}

		await Task.Delay(AppExtensions.UIWaitTimeInMilliseconds);

		var screenClosed = TakeScreenshot("When_Dialog_Closed");
		ImageAssert.AreEqual(screenBefore, screenClosed,tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));
	}

}
