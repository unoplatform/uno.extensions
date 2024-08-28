namespace TestHarness.UITest;

public class Given_ContentDialog : NavigationTestBase
{
	[TestCase("SimpleDialogNavRequestButton", 0, false)]
	[TestCase("SimpleDialogCodebehindButton", 0, false)]
	[TestCase("SimpleDialogCodebehindWithCancelButton", 0, false)]
	[TestCase("SimpleDialogCodebehindWithCancelButton", 3, true)]
	public async Task When_SimpleContentDialog(string dialogButton, int delayInSeconds, bool dialogCancelled)
	{
		InitTestSection(TestSections.Navigation_Dialogs);

		App.WaitThenTap("ContentDialogsButton");

		App.WaitElement("DialogsContentDialogsPage");
		var screenBefore = TakeScreenshot("When_Dialog_Before");
		App.Tap(dialogButton);
		await Task.Delay(500); // Make sure the dialog is showing completely
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
			App.WaitThenTap("DialogsSimpleDialogCloseButton");
		}

		await Task.Delay(AppExtensions.UIWaitTimeInMilliseconds);

		var screenClosed = TakeScreenshot("When_Dialog_Closed");
		ImageAssert.AreEqual(screenBefore, screenClosed,tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));
	}

	[Test]
	public async Task When_ComplexContentDialog()
	{
		InitTestSection(TestSections.Navigation_Dialogs);

		App.WaitThenTap("ContentDialogsButton");

		App.WaitElement("DialogsContentDialogsPage");

		var screenBefore = TakeScreenshot("When_Dialog_Before");
		App.Tap("ComplexDialogNavRequestButton");
		var screenAfter = TakeScreenshot("When_Dialog_After");
		ImageAssert.AreNotEqual(screenBefore, screenAfter);

		App.WaitElement("ComplexDialogFirstPageNavigationBar");

		App.Tap("ComplexDialogFirstPageCloseButton");

		await Task.Delay(AppExtensions.UIWaitTimeInMilliseconds);

		var screenClosed = TakeScreenshot("When_Dialog_Closed");
		ImageAssert.AreEqual(screenBefore, screenClosed, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));
	}


	[Test]
	public async Task When_ComplexContentDialogNavigateSecondPage()
	{
		InitTestSection(TestSections.Navigation_Dialogs);

		App.WaitThenTap("ContentDialogsButton");

		App.WaitElement("DialogsContentDialogsPage");

		var screenBefore = TakeScreenshot("When_Dialog_Before");
		App.Tap("ComplexDialogNavRequestButton");
		var screenAfter = TakeScreenshot("When_Dialog_After");
		ImageAssert.AreNotEqual(screenBefore, screenAfter);

		App.WaitElement("ComplexDialogFirstPageNavigationBar");

		App.Tap("ComplexDialogFirstPageCloseAndSecondButton");

		App.WaitElement("DialogsContentDialogsSecondPageNavigationBar");

		PlatformHelpers.On(
				iOS: () => App.FastTap("BackButton"),
				Android: () => App.FastTap(q => q.Marked("DialogsContentDialogsSecondPageNavigationBar").Descendant("AppCompatImageButton")),
				Browser: () => App.Tap("DialogsContentDialogsSecondPageBackButton")
			);

		await Task.Delay(AppExtensions.UIWaitTimeInMilliseconds);

		var screenClosed = TakeScreenshot("When_Dialog_Closed");
		ImageAssert.AreEqual(screenBefore, screenClosed, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));
	}

}
