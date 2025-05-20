namespace TestHarness.UITest;

public class Given_AddressBar : NavigationTestBase
{
	[Test]
	public async Task When_AddressBar_HomePage_Wont_Navigate_Twice()
	{
		InitTestSection(TestSections.Navigation_AddressBar);

		App.WaitElement("TbInstanceCountProperty");

		var intanceCount = App.GetText("TbInstanceCountProperty");

		Assert.AreEqual("1", intanceCount);
	}

	[Test]
	[ActivePlatforms(Platform.Browser)]
	public void When_AddressBar_SecondPage_Query_Displayed()
	{
		InitTestSection(TestSections.Navigation_AddressBar);

		App.WaitThenTap("AddressBarSecondButton");

		App.WaitThenTap("GetUrlFromBrowser");

		var url = App.GetText("TxtUrlFromBrowser");

		StringAssert.Contains(url, "QueryUser.Id=8a5c5b2e-ff96-474b-9e4d-65bde598f6bc");
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

	[Test]
	[ActivePlatforms(Platform.Browser)]
	public void When_PageNavigationNavigateQueryParamsDependsOn()
	{
		InitTestSection(TestSections.Navigation_PageNavigationRegistered);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitThenTap("OnePageToTwoPageWithDataButton");

		App.WaitThenTap("SecondPageGetUrlFromBrowser");

		var url = App.GetText("SecondPageTxtUrlFromBrowser");

		StringAssert.Contains(url, "Value=0");
	}
}
