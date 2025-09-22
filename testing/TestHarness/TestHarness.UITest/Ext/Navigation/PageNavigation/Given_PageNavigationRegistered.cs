namespace TestHarness.UITest;

public class Given_PageNavigationRegistered : NavigationTestBase
{
	[Test]
	public void When_PageNavigationRegisteredXAML()
	{
		InitTestSection(TestSections.Navigation_PageNavigationRegistered);

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
		InitTestSection(TestSections.Navigation_PageNavigationRegistered);

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
	[Ignore("ImageAssert failures: https://github.com/unoplatform/uno.extensions/issues/2952")]
	public void When_PageNavigationRegisteredViewModel()
	{
		InitTestSection(TestSections.Navigation_PageNavigationRegistered);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitElement("OnePageToTwoPageViewModelButton");
		var screenBefore = TakeScreenshot("When_PageNavigationViewModel_Before");
		App.WaitThenTap("OnePageToTwoPageViewModelButton");
		App.WaitThenTap("TwoPageToThreePageViewModelButton", timeout:TimeSpan.FromSeconds(30));
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

	[Test]
	public void When_PageNavigationRegisteredDependsn()
	{
		InitTestSection(TestSections.Navigation_PageNavigationRegistered);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitElement("OnePageToTwoPageButton");
		var screenBefore = TakeScreenshot("When_PageNavigationXAML_Before");
		App.WaitThenTap("OnePageToTwoPageButton");
		App.WaitThenTap("TwoPageToThreePageButton");
		App.WaitThenTap("ThreePageToFourPageButton");
		App.WaitThenTap("FourPageToFivePageButton");

		App.WaitThenTap("FivePageToSixPageButton");
		App.WaitThenTap("SixPageBackButton");


		App.WaitThenTap("FivePageToSevenPageButton");
		App.WaitThenTap("SevenPageBackButton");
		App.WaitThenTap("SixPageBackButton");

		App.WaitThenTap("FivePageToEightPageButton");
		App.WaitThenTap("EightPageBackButton");
		App.WaitThenTap("SevenPageBackButton");
		App.WaitThenTap("SixPageBackButton");

		App.WaitThenTap("FivePageToNinePageButton");
		App.WaitThenTap("NinePageBackButton");
		App.WaitThenTap("EightPageBackButton");
		App.WaitThenTap("SevenPageBackButton");
		App.WaitThenTap("SixPageBackButton");

		App.WaitThenTap("FivePageToTenPageButton");
		App.WaitThenTap("TenPageBackButton");
		App.WaitThenTap("NinePageBackButton");
		App.WaitThenTap("EightPageBackButton");
		App.WaitThenTap("SevenPageBackButton");
		App.WaitThenTap("SixPageBackButton");


		App.WaitThenTap("FivePageBackButton");
		App.WaitThenTap("FourPageBackButton");
		App.WaitThenTap("ThreePageBackButton");
		App.WaitThenTap("TwoPageBackButton");


		App.WaitElement("OnePageToTwoPageButton");
		var screenAfter = TakeScreenshot("When_PageNavigationXAML_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

	}


	[Test]
	[Ignore("ImageAssert failures: https://github.com/unoplatform/uno.extensions/issues/2952")]
	public void When_PageNavigationRegisteredRoot()
	{
		InitTestSection(TestSections.Navigation_PageNavigationRegistered);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitElement("OnePageToTwoPageButton");
		var screenBefore = TakeScreenshot("When_PageNavigationXAML_Before");
		App.WaitThenTap("OnePageToTwoPageButton");
		App.WaitThenTap("TwoPageToThreePageButton");
		App.WaitThenTap("ThreePageToFourPageButton");
		App.WaitThenTap("FourPageToFivePageButton");

		// / (will navigate to Second (default) with One in backstack)
		App.WaitThenTap("FivePageRootPageButton");
		App.WaitThenTap("TwoPageBackButton");
		OnePageToFivePage();

		// /One (will clear to One)
		App.WaitThenTap("FivePageRootOnePageButton");
		OnePageToFivePage();

		// /Three (will navigate to Third with One and Two in backstack)
		App.WaitThenTap("FivePageRootThreePageButton");
		App.WaitThenTap("ThreePageBackButton");
		App.WaitThenTap("TwoPageBackButton");
		OnePageToFivePage();

		// /One/Four (will navigate to Four with One in backstack)
		App.WaitThenTap("FivePageRootOneFourPageButton");
		App.WaitThenTap("FourPageBackButton");
		OnePageToFivePage();

		// -/Three/Seven (will navigate to Seven with Six, Three, Two and One in backstack)
		App.WaitThenTap("FivePageRootThreeSevenClearPageButton");
		App.WaitThenTap("SevenPageBackButton");
		App.WaitThenTap("SixPageBackButton");
		App.WaitThenTap("ThreePageBackButton");
		App.WaitThenTap("TwoPageBackButton");

		App.WaitElement("OnePageToTwoPageButton");
		var screenAfter = TakeScreenshot("When_PageNavigationXAML_After");
		//ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

	}

	private void OnePageToFivePage()
	{
		App.WaitThenTap("OnePageToTwoPageButton");
		App.WaitThenTap("TwoPageToThreePageButton");
		App.WaitThenTap("ThreePageToFourPageButton");
		App.WaitThenTap("FourPageToFivePageButton");

	}
}
