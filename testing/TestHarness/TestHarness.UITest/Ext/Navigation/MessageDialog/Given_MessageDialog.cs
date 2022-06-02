


namespace TestHarness.UITest;

public class Given_MessageDialog : NavigationTestBase
{
	[Test]
	public void When_MessageDialogFromXAML()
	{
		InitTestSection(TestSections.MessageDialog);

		App.WaitThenTap("SimpleDialogsButton");


		App.WaitForElement("MessageDialogFromXamlButton");
		var screenBefore=TakeScreenshot("When_MessageDialogFromXAML_Before");
		App.Tap("MessageDialogFromXamlButton");
		var screenAfter = TakeScreenshot("When_MessageDialogFromXAML_After");
		ImageAssert.AreEqual(screenBefore, screenAfter);


	}
}
