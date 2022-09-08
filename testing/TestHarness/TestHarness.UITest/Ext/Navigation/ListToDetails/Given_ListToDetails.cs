namespace TestHarness.UITest;

public class Given_ListToDetails : NavigationTestBase
{
	[Test]
	public async Task When_ListToDetails()
	{
		InitTestSection(TestSections.Navigation_ListToDetails);

		App.WaitThenTap("ShowListToDetailsHomeButton");

		App.WaitElement("ListToDetailsHomeNavigationBar");
		App.WaitElement("ListToDetailsListNavigationBar");
		await App.TapAndWait("SelectSecondItemButton", "ListToDetailsDetailsNavigationBar");
		App.WaitThenTap("DetailsBackButton");

		await App.TapAndWait("RawNavigateButton", "ListToDetailsDetailsNavigationBar");
		App.WaitThenTap("DetailsBackButton");
		

		App.WaitElement("ListToDetailsListNavigationBar");

	}

}
