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
}
