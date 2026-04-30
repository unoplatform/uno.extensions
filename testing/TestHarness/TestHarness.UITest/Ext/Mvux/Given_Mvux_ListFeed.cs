namespace TestHarness.UITest;

public class Given_Mvux_ListFeed : NavigationTestBase
{
	[Test]
	public void When_ListFeed_Loads_Collection()
	{
		InitTestSection(TestSections.Mvux_Basic);

		App.WaitThenTap("ShowMvuxListFeedPageButton");

		App.WaitForText("MvuxListFeedItemCount", "Count: 3");
	}
}
