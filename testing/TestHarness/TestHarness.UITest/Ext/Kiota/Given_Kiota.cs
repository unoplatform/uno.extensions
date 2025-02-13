namespace TestHarness.UITest;

public class Given_Kiota : NavigationTestBase
{
	[Test]
	public async Task When_KiotaClient_Registered()
	{
		InitTestSection(TestSections.Http_Kiota);

		App.WaitThenTap("ShowKiotaPageButton");

		App.WaitElement("KiotaHomeNavigationBar");

		var initializationStatus = App.GetText("InitializationStatusTextBlock");
		initializationStatus.Should().Contain("Kiota Client initialized successfully.");

	}
}
