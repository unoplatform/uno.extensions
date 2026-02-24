namespace TestHarness.UITest;

public class Given_KiotaSourceGen : NavigationTestBase
{
	[Test]
	public async Task When_SourceGenClient_Registered()
	{
		InitTestSection(TestSections.Http_Kiota_SourceGen);

		App.WaitThenTap("ShowSourceGenHomeButton");

		var initializationStatus = App.GetText("SourceGenInitStatusTextBlock");
		initializationStatus.Should().Contain("Success");

		var clientType = App.GetText("SourceGenClientTypeTextBlock");
		clientType.Should().Contain("SourceGenPetClient");

		var petsEndpoint = App.GetText("SourceGenPetsEndpointTextBlock");
		petsEndpoint.Should().Be("Available");
	}
}
