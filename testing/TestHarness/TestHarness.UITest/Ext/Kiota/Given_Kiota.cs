namespace TestHarness.UITest;

public class Given_Kiota : NavigationTestBase
{
	[Test]
	public async Task When_KiotaClient_Registered()
	{
		InitTestSection(TestSections.Http_Kiota);

		App.WaitThenTap("ShowAppButton");

		App.WaitElement("KiotaHomeNavigationBar");

		App.WaitThenTap("FetchPostsButton");

		await Task.Delay(2000);

		var fetchResult = App.GetText("FetchPostsResultTextBlock");
		fetchResult.Should().Contain("Retrieved").And.Contain("posts");
	}
}
