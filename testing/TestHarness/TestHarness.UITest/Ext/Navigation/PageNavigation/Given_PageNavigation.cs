namespace TestHarness.UITest;

public class Given_PageNavigation : NavigationTestBase
{
	[Test]
	public void When_PageNavigationXAML()
	{
		InitTestSection(TestSections.PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		var screenBefore = TakeScreenshot("When_PageNavigationXAML_Before");
		App.Tap("OnePageToTwoPageButton");
		App.Tap("TwoPageToThreePageButton");
		App.Tap("ThreePageToFourPageButton");
		App.Tap("FourPageToFivePageButton");
		App.Tap("FivePageBackButton");
		App.Tap("FourPageBackButton");
		App.Tap("ThreePageBackButton");
		App.Tap("TwoPageBackButton");


		var screenAfter = TakeScreenshot("When_PageNavigationXAML_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

	}

	[Test]
	public void When_PageNavigationCodebehind()
	{
		InitTestSection(TestSections.PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		var screenBefore = TakeScreenshot("When_PageNavigationCodebehind_Before");
		App.Tap("OnePageToTwoPageCodebehindButton");
		App.Tap("TwoPageToThreePageCodebehindButton");
		App.Tap("ThreePageToFourPageCodebehindButton");
		App.Tap("FourPageToFivePageCodebehindButton");
		App.Tap("FivePageBackCodebehindButton");
		App.Tap("FourPageBackCodebehindButton");
		App.Tap("ThreePageBackCodebehindButton");
		App.Tap("TwoPageBackCodebehindButton");


		var screenAfter = TakeScreenshot("When_PageNavigationCodebehind_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

	}

	[Test]
	public void When_PageNavigationViewModel()
	{
		InitTestSection(TestSections.PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		var screenBefore = TakeScreenshot("When_PageNavigationViewModel_Before");
		App.Tap("OnePageToTwoPageViewModelButton");
		App.Tap("TwoPageToThreePageViewModelButton");
		App.Tap("ThreePageToFourPageViewModelButton");
		App.Tap("FourPageToFivePageViewModelButton");
		App.Tap("FivePageBackViewModelButton");
		App.Tap("FourPageBackViewModelButton");
		App.Tap("ThreePageBackViewModelButton");
		App.Tap("TwoPageBackViewModelButton");


		var screenAfter = TakeScreenshot("When_PageNavigationViewModel_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

	}
}
