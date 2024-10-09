namespace TestHarness.UITest;

public class Given_PageNavigation : NavigationTestBase
{
	[Test]
	public void When_PageNavigationXAML()
	{
		InitTestSection(TestSections.Navigation_PageNavigation);

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
	public void When_PageNavigationCodebehind()
	{
		InitTestSection(TestSections.Navigation_PageNavigation);

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
	public void When_PageNavigationViewModel()
	{
		InitTestSection(TestSections.Navigation_PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitElement("OnePageToTwoPageViewModelButton");
		var screenBefore = TakeScreenshot("When_PageNavigationViewModel_Before");
		App.WaitThenTap("OnePageToTwoPageViewModelButton");
		App.WaitThenTap("TwoPageToThreePageViewModelButton", timeout: TimeSpan.FromSeconds(30));
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
	public void When_PageNavigationDependsn()
	{
		// Note: There's no DependsOns on, so navigating to pages six, seven, eight .... should just be normal page navigations

		InitTestSection(TestSections.Navigation_PageNavigation);

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

		App.WaitThenTap("FivePageToEightPageButton");
		App.WaitThenTap("EightPageBackButton");

		App.WaitThenTap("FivePageToNinePageButton");
		App.WaitThenTap("NinePageBackButton");

		App.WaitThenTap("FivePageToTenPageButton");
		App.WaitThenTap("TenPageBackButton");

		App.WaitThenTap("FivePageBackButton");
		App.WaitThenTap("FourPageBackButton");
		App.WaitThenTap("ThreePageBackButton");
		App.WaitThenTap("TwoPageBackButton");


		App.WaitElement("OnePageToTwoPageButton");
		var screenAfter = TakeScreenshot("When_PageNavigationXAML_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

	}

	[Test]
	public void When_PageNavigationRegisteredRoot()
	{
		InitTestSection(TestSections.Navigation_PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitElement("OnePageToTwoPageButton");
		var screenBefore = TakeScreenshot("When_PageNavigationXAML_Before");
		App.WaitThenTap("OnePageToTwoPageButton");
		App.WaitThenTap("TwoPageToThreePageButton");
		App.WaitThenTap("ThreePageToFourPageButton");
		App.WaitThenTap("FourPageToFivePageButton");

		// / - This does nothing since no default route registered
		App.WaitThenTap("FivePageRootPageButton");
		App.WaitThenTap("FivePageBackButton");
		App.WaitThenTap("FourPageToFivePageButton");

		// /One (will clear to One)
		App.WaitThenTap("FivePageRootOnePageButton");
		OnePageToFivePage();

		// /Three (will clear to Three)
		App.WaitThenTap("FivePageRootThreePageButton");
		App.WaitThenTap("ThreePageToFourPageButton");
		App.WaitThenTap("FourPageToFivePageButton");

		// /One/Four (will navigate to Four with One in backstack)
		App.WaitThenTap("FivePageRootOneFourPageButton");
		App.WaitThenTap("FourPageBackButton");
		OnePageToFivePage();

		// -/Three/Seven (will navigate to Seven with Six in backstack)
		App.WaitThenTap("FivePageRootThreeSevenClearPageButton");
		App.WaitThenTap("SevenPageBackButton");
		App.WaitThenTap("ThreePageToFourPageButton");
		App.WaitThenTap("FourPageToFivePageButton");
		App.WaitThenTap("FivePageRootOnePageButton"); // Clear back to /One


		App.WaitElement("OnePageToTwoPageButton");
		var screenAfter = TakeScreenshot("When_PageNavigationXAML_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));
	}

	[Test]
	public void When_PageNavigationDataContextDidntChange()
	{
		InitTestSection(TestSections.Navigation_PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		// If DataContext is ever changed to anything other than the expected
		// the text will be "DataContext is not correct"
		App.WaitForText("OnePageTxtDataContext", "DataContext is ok");
	}

	private void OnePageToFivePage()
	{
		App.WaitThenTap("OnePageToTwoPageButton");
		App.WaitThenTap("TwoPageToThreePageButton");
		App.WaitThenTap("ThreePageToFourPageButton");
		App.WaitThenTap("FourPageToFivePageButton");

	}

	[Test]
	[ActivePlatforms(Platform.Browser)]
	public void When_PageNavigationNavigateRootUpdateUrl()
	{
		InitTestSection(TestSections.Navigation_PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitThenTap("OnePageGetUrlFromBrowser");
		App.WaitElement("OnePageTxtUrlFromBrowser");
		var urlBefore = App.GetText("OnePageTxtUrlFromBrowser");

		App.WaitThenTap("OnePageToTwoPageButton");

		App.WaitThenTap("TwoPageBackButton");

		App.WaitThenTap("OnePageGetUrlFromBrowser");
		App.WaitElement("OnePageTxtUrlFromBrowser");
		var urlAfter = App.GetText("OnePageTxtUrlFromBrowser");

		Assert.AreEqual(urlBefore, urlAfter);
	}
}
