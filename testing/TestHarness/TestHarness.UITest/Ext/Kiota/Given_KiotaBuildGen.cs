namespace TestHarness.UITest;

public class Given_KiotaBuildGen : NavigationTestBase
{
	[Test]
	public async Task When_BuildGenClient_Registered()
	{
		InitTestSection(TestSections.Http_Kiota_BuildGen);

		App.WaitThenTap("ShowBuildGenHomeButton");

		var initializationStatus = App.GetText("BuildGenInitStatusTextBlock");
		initializationStatus.Should().Contain("Success");

		var clientType = App.GetText("BuildGenClientTypeTextBlock");
		clientType.Should().Contain("BuildGenPetClient");

		var petsEndpoint = App.GetText("BuildGenPetsEndpointTextBlock");
		petsEndpoint.Should().Be("Available");
	}
}
