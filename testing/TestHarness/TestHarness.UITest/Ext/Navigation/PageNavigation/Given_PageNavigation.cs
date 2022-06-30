namespace TestHarness.UITest;

public class Given_PageNavigation : NavigationTestBase
{
	[Test]
	public void When_PageNavigationXAML()
	{
		InitTestSection(TestSections.PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitForElement("OnePageToTwoPageButton");
		var screenBefore = TakeScreenshot("When_PageNavigationXAML_Before");
		App.WaitThenTap("OnePageToTwoPageButton");
		App.WaitThenTap("TwoPageToThreePageButton");
		App.WaitThenTap("ThreePageToFourPageButton");
		App.WaitThenTap("FourPageToFivePageButton");
		App.WaitThenTap("FivePageBackButton");
		App.WaitThenTap("FourPageBackButton");
		App.WaitThenTap("ThreePageBackButton");
		App.WaitThenTap("TwoPageBackButton");


		App.WaitForElement("OnePageToTwoPageButton");
		var screenAfter = TakeScreenshot("When_PageNavigationXAML_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

	}

	[Test]
	public void When_PageNavigationCodebehind()
	{
		InitTestSection(TestSections.PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitForElement("OnePageToTwoPageCodebehindButton");
		var screenBefore = TakeScreenshot("When_PageNavigationCodebehind_Before");
		App.WaitThenTap("OnePageToTwoPageCodebehindButton");
		App.WaitThenTap("TwoPageToThreePageCodebehindButton");
		App.WaitThenTap("ThreePageToFourPageCodebehindButton");
		App.WaitThenTap("FourPageToFivePageCodebehindButton");
		App.WaitThenTap("FivePageBackCodebehindButton");
		App.WaitThenTap("FourPageBackCodebehindButton");
		App.WaitThenTap("ThreePageBackCodebehindButton");
		App.WaitThenTap("TwoPageBackCodebehindButton");


		App.WaitForElement("OnePageToTwoPageCodebehindButton");
		var screenAfter = TakeScreenshot("When_PageNavigationCodebehind_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));
	}

	[Test]
	public void When_PageNavigationViewModel()
	{
		InitTestSection(TestSections.PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitForElement("OnePageToTwoPageViewModelButton");
		var screenBefore = TakeScreenshot("When_PageNavigationViewModel_Before");
		App.WaitThenTap("OnePageToTwoPageViewModelButton");
		App.WaitThenTap("TwoPageToThreePageViewModelButton");
		App.WaitThenTap("ThreePageToFourPageViewModelButton");
		App.WaitThenTap("FourPageToFivePageViewModelButton");
		App.WaitThenTap("FivePageBackViewModelButton");
		App.WaitThenTap("FourPageBackViewModelButton");
		App.WaitThenTap("ThreePageBackViewModelButton");
		App.WaitThenTap("TwoPageBackViewModelButton");


		App.WaitForElement("OnePageToTwoPageViewModelButton");
		var screenAfter = TakeScreenshot("When_PageNavigationViewModel_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

	}
}
