namespace TestHarness.UITest;

public class Given_Apps_Regions : NavigationTestBase
{
	[Test]
	public async Task When_Regions_Send_Data_NavView()
	{
		InitTestSection(TestSections.Apps_Regions);

		App.WaitThenTap("ShowAppButton");

		App.WaitElement("RegionsHomePageTextBox");

		var textToSet = "Hello, World!";

		App.SetText("RegionsHomePageTextBox", textToSet);

		App.WaitThenTap("RegionsHomePageThirdPage");

		App.WaitElement("RegionsThirdPageTextBock");

		var textFromTb = App.GetText("RegionsThirdPageTextBock");

		Assert.AreEqual(textToSet, textFromTb);
	}

	[Test]
	public async Task When_Regions_Send_Data_TabBar()
	{
		InitTestSection(TestSections.Apps_Regions);

		App.WaitThenTap("ShowAppButton");

		App.WaitThenTap("RegionsHomePageRegionsTbData");

		App.WaitThenTap("RegionsTbDataPageTabOne");

		// Wait for each tab's content before touching it. Tapping a TabBarItem returns as soon as the
		// tap lands, not when the region has finished materializing the page, so SetText/GetText could
		// run against a tree that does not contain the TextBox yet - which surfaced as
		// "InvalidOperationException: The query returned no results" on the WebAssembly lane. The
		// NavView test above already waits this way.
		App.WaitElement("RegionsFirstTbiDataPageTextBox");

		var textToSet = "Hello, World!";

		App.SetText("RegionsFirstTbiDataPageTextBox", textToSet);

		App.WaitThenTap("RegionsTbDataPageTabTwo");

		App.WaitElement("RegionsSecondTbiDataPageTextBox");

		var textFromTb = App.GetText("RegionsSecondTbiDataPageTextBox");

		Assert.AreEqual(textToSet, textFromTb);
	}
}
