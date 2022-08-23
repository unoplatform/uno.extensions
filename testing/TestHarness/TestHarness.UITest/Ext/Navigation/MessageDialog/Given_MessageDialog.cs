namespace TestHarness.UITest;

public class Given_MessageDialog : NavigationTestBase
{
	[Test]
	public async Task When_MessageDialogFromXAML()
	{
		InitTestSection(TestSections.MessageDialog);

		App.WaitThenTap("SimpleDialogsButton");
		
		App.WaitElement("MessageDialogFromXamlButton");
		var screenBefore=TakeScreenshot("When_MessageDialogFromXAML_Before");
		App.Tap("MessageDialogFromXamlButton");
		var screenAfter = TakeScreenshot("When_MessageDialogFromXAML_After");
		ImageAssert.AreNotEqual(screenBefore, screenAfter);

		App
			.Marked("CloseMessageDialogToggleButton")
			.SetDependencyPropertyValue("IsChecked", true.ToString());


		var screenClosed = TakeScreenshot("When_MessageDialogFromXAML_Closed");
		ImageAssert.AreEqual(screenBefore, screenClosed,tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

		// TODO: Work out how to tap on individual buttons on the message dialog to close them.

	}

}
