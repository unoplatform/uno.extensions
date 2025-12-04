namespace TestHarness.UITest;

public class Given_ContentDialog : NavigationTestBase
{
	[TestCase("SimpleDialogNavRequestButton", 0, false)]
	[TestCase("SimpleDialogCodebehindButton", 0, false)]
	[TestCase("SimpleDialogCodebehindWithCancelButton", 0, false)]
	[TestCase("SimpleDialogCodebehindWithCancelButton", 3, true)]
	[Ignore("ImageAssert failures: https://github.com/unoplatform/uno.extensions/issues/2952")]
	public async Task When_SimpleContentDialog(string dialogButton, int delayInSeconds, bool dialogCancelled)
	{
		InitTestSection(TestSections.Navigation_Dialogs);

		App.WaitThenTap("ContentDialogsButton");

		App.WaitElement("DialogsContentDialogsPage");
		App.Tap("DialogsContentDialogsPage");
		App.Wait(TimeSpan.FromSeconds(1));
		var screenBefore = TakeScreenshot("When_Dialog_Before");
		App.FastTap(dialogButton);
		App.Wait(TimeSpan.FromMilliseconds(500)); // Make sure the dialog is showing completely
		var screenAfter = TakeScreenshot("When_Dialog_After");
		ImageAssert.AreNotEqual(screenBefore, screenAfter);

		if (delayInSeconds > 0)
		{
			App.Wait(TimeSpan.FromSeconds(delayInSeconds));
			var screenAfterDelay = TakeScreenshot("When_Dialog_After_Delay");
			if (dialogCancelled)
			{
				ImageAssert.AreNotEqual(screenAfter, screenAfterDelay);
			}
			else
			{
				ImageAssert.AreAlmostEqual(screenAfter, screenAfterDelay, permittedPixelError: 20);

			}
		}

		if (!dialogCancelled)
		{
			App.WaitThenTap("DialogsSimpleDialogCloseButton");
		}
		App.Wait(TimeSpan.FromMilliseconds(AppExtensions.UIWaitTimeInMilliseconds));

		var screenClosed = TakeScreenshot("When_Dialog_Closed");
		ImageAssert.AreAlmostEqual(screenBefore, screenClosed, permittedPixelError: 20);
	}

	[Test]
	[Ignore("ImageAssert failures: https://github.com/unoplatform/uno.extensions/issues/2952")]
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

		App.Wait(TimeSpan.FromMilliseconds(AppExtensions.UIWaitTimeInMilliseconds));

		var screenClosed = TakeScreenshot("When_Dialog_Closed");
		ImageAssert.AreEqual(screenBefore, screenClosed, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));
	}


	[Test]
	[Ignore("ImageAssert failures: https://github.com/unoplatform/uno.extensions/issues/2952")]
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

		App.Tap("DialogsContentDialogsSecondPageBackButton");

		App.Wait(TimeSpan.FromMilliseconds(AppExtensions.UIWaitTimeInMilliseconds));

		var screenClosed = TakeScreenshot("When_Dialog_Closed");
		ImageAssert.AreEqual(screenBefore, screenClosed, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));
	}

}
