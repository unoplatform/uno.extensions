namespace TestHarness.UITest;

public class Given_PageNavigationRegistered : NavigationTestBase
{
	[Test]
	public void When_PageNavigationRegisteredXAML()
	{
		InitTestSection(TestSections.PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitElement("OnePageToTwoPageButton");
		var screenBefore = TakeScreenshot("When_PageNavigationXAML_Before");
		App.WaitThenTap("OnePageToTwoPageButton");
		App.WaitThenTap("TwoPageToThreePageButton");
		App.WaitThenTap("ThreePageToFourPageButton");
		App.WaitThenTap("FourPageToFivePageButton");
		App.WaitThenTap("FivePageBackButton");
		App.WaitThenTap("FourPageBackButton");
		App.WaitThenTap("ThreePageBackButton");
		App.WaitThenTap("TwoPageBackButton");


		App.WaitElement("OnePageToTwoPageButton");
		var screenAfter = TakeScreenshot("When_PageNavigationXAML_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

	}

	[Test]
	public void When_PageNavigationRegisteredCodebehind()
	{
		InitTestSection(TestSections.PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitElement("OnePageToTwoPageCodebehindButton");
		var screenBefore = TakeScreenshot("When_PageNavigationCodebehind_Before");
		App.WaitThenTap("OnePageToTwoPageCodebehindButton");
		App.WaitThenTap("TwoPageToThreePageCodebehindButton");
		App.WaitThenTap("ThreePageToFourPageCodebehindButton");
		App.WaitThenTap("FourPageToFivePageCodebehindButton");
		App.WaitThenTap("FivePageBackCodebehindButton");
		App.WaitThenTap("FourPageBackCodebehindButton");
		App.WaitThenTap("ThreePageBackCodebehindButton");
		App.WaitThenTap("TwoPageBackCodebehindButton");


		App.WaitElement("OnePageToTwoPageCodebehindButton");
		var screenAfter = TakeScreenshot("When_PageNavigationCodebehind_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));
	}

	[Test]
	[Ignore("Failing CI - runs ok locally")]
	public void When_PageNavigationRegisteredViewModel()
	{
		InitTestSection(TestSections.PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitElement("OnePageToTwoPageViewModelButton");
		var screenBefore = TakeScreenshot("When_PageNavigationViewModel_Before");
		App.WaitThenTap("OnePageToTwoPageViewModelButton");
		App.WaitThenTap("TwoPageToThreePageViewModelButton");
		App.WaitThenTap("ThreePageToFourPageViewModelButton");
		App.WaitThenTap("FourPageToFivePageViewModelButton");
		App.WaitThenTap("FivePageBackViewModelButton");
		App.WaitThenTap("FourPageBackViewModelButton");
		App.WaitThenTap("ThreePageBackViewModelButton");
		App.WaitThenTap("TwoPageBackViewModelButton");


		App.WaitElement("OnePageToTwoPageViewModelButton");
		var screenAfter = TakeScreenshot("When_PageNavigationViewModel_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

	}
}
