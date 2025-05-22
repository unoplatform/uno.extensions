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
	public void When_AddressBar_Navigating_Through_NavItems_AddressBar_Updates()
	{
		InitTestSection(TestSections.Navigation_AddressBar_Nested);

		App.WaitElement("AddressBarRootPageNavigationView");

		// Sequencial check
		App.WaitThenTap("AddressBarRootPageCoffeeNavItem");
		AssertAddressBarUrl("AddressBarCoffeePage", "AddressBarCoffee");

		App.WaitThenTap("AddressBarRootPageHomeNavItem");
		AssertAddressBarUrl("AddressBarHomePage", "AddressBarHome");

		App.WaitThenTap("AddressBarRootPageSecondNavItem");
		AssertAddressBarUrl("AddressBarSecondPage", "AddressBarSecond");

		// Non-Sequencial check
		App.WaitThenTap("AddressBarRootPageHomeNavItem");
		AssertAddressBarUrl("AddressBarHomePage", "AddressBarHome");

		App.WaitThenTap("AddressBarRootPageSecondNavItem");
		AssertAddressBarUrl("AddressBarSecondPage", "AddressBarSecond");

		App.WaitThenTap("AddressBarRootPageHomeNavItem");
		AssertAddressBarUrl("AddressBarHomePage", "AddressBarHome");

		App.WaitThenTap("AddressBarRootPageCoffeeNavItem");
		AssertAddressBarUrl("AddressBarCoffeePage", "AddressBarCoffee");

		App.WaitThenTap("AddressBarRootPageSecondNavItem");
		AssertAddressBarUrl("AddressBarSecondPage", "AddressBarSecond");

		App.WaitThenTap("AddressBarRootPageHomeNavItem");
		AssertAddressBarUrl("AddressBarHomePage", "AddressBarHome");

		App.WaitThenTap("AddressBarRootPageSecondNavItem");
		AssertAddressBarUrl("AddressBarSecondPage", "AddressBarSecond");
	}

	private void AssertAddressBarUrl(string pagePrefix, string contains)
	{
		App.WaitThenTap($"{pagePrefix}GetUrlFromBrowser");

		var url = App.GetText($"{pagePrefix}TxtUrlFromBrowser");

		StringAssert.Contains(url, contains);
	}
}
