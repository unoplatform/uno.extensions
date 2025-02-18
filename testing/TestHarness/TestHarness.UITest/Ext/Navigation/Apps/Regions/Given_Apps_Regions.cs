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

		var textToSet = "Hello, World!";

		App.SetText("RegionsFirstTbiDataPageTextBox", textToSet);

		App.WaitThenTap("RegionsTbDataPageTabTwo");

		var textFromTb = App.GetText("RegionsSecondTbiDataPageTextBox");

		Assert.AreEqual(textToSet, textFromTb);
	}
}
