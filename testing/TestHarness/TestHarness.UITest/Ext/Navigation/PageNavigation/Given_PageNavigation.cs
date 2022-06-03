namespace TestHarness.UITest;

public class Given_PageNavigation : NavigationTestBase
{
	[Test]
	public void When_PageNavigation()
	{
		InitTestSection(TestSections.PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		var screenBefore = TakeScreenshot("When_MessageDialogFromXAML_Before");
		App.Tap("OnePageToTwoPageButton");
		App.Tap("TwoPageToThreePageButton");
		App.Tap("ThreePageToFourPageButton");
		App.Tap("FourPageToFivePageButton");
		App.Tap("FivePageBackButton");
		App.Tap("FourPageBackButton");
		App.Tap("ThreePageBackButton");
		App.Tap("TwoPageBackButton");


		var screenAfter = TakeScreenshot("When_MessageDialogFromXAML_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(10));

	}
}
