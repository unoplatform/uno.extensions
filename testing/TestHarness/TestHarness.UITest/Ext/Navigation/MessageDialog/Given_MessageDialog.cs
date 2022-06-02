


namespace TestHarness.UITest;

public class Given_MessageDialog : NavigationTestBase
{
	[Test]
	public void When_MessageDialogFromXAML()
	{
		InitTestSection(TestSections.MessageDialog);

		App.WaitThenTap("SimpleDialogsButton");
		var closeButton = App.Marked("CloseAllDialogsButton");

		App.WaitForElement("MessageDialogFromXamlButton");
		var screenBefore=TakeScreenshot("When_MessageDialogFromXAML_Before");
		App.Tap("MessageDialogFromXamlButton");
		var screenAfter = TakeScreenshot("When_MessageDialogFromXAML_After");
		ImageAssert.AreNotEqual(screenBefore, screenAfter);

		App.Marked("SimpleDialogsPage").Invoke("CloseAllMessageDialogs",null);

//		App.Query(e => e.Id("SimpleDialogsPage").Invoke("CloseAllMessageDialogs", null, null));

		//var buttons = App.Query(e => e.All().Property("Label","Nah"));
		//foreach (var button in buttons)
		//{
		//}
		//App.Tap(closeButton);


		//var popupResult = App.WaitForElement("PopupBorder").First();

		//App.TapCoordinates(popupResult.Rect.Y - 30, popupResult.Rect.X-30);

		var screenClosed = TakeScreenshot("When_MessageDialogFromXAML_Closed");
		ImageAssert.AreEqual(screenBefore, screenClosed);
	}
}
