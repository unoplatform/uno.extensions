namespace TestHarness.UITest;

public class Given_Mvux_Feed : NavigationTestBase
{
	[Test]
	public void When_Feed_Loads_Data()
	{
		InitTestSection(TestSections.Mvux_Basic);

		App.WaitThenTap("ShowMvuxFeedPageButton");

		App.WaitForText("MvuxFeedItemName", "Test Item");
		var idText = App.GetText("MvuxFeedItemId");
		idText.Should().Be("1");
	}
}
