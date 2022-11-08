namespace TestHarness.UITest;

public class Given_ContentControl : NavigationTestBase
{
	[Test]
	public async Task When_ContentControl()
	{
		InitTestSection(TestSections.Navigation_ContentControl);


		App.WaitThenTap("ShowContentControlHomeButton");
		App.WaitElement("ContentControlHomePageNavBar");

		await App.TapAndWait("NestedNamedRegionOneButton", "ContentControlOnePage");

		await App.TapAndWait("NestedNamedRegionTwoButton", "ContentControlTwoPage");

		await App.TapAndWait("NestedUnNamedRegionOneButton", "ContentControlOnePage");

		await App.TapAndWait("NestedNamedRegionOneButton", "ContentControlOnePage");


		await App.TapAndWait("NestedUnNamedRegionTwoButton", "ContentControlTwoPage");

	}
}
