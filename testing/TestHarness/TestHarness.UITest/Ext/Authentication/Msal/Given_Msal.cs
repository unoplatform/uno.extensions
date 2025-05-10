namespace TestHarness.UITest;

public class Given_Msal : NavigationTestBase
{
	// [Test]
	public async Task When_Multi_Auth()
	{
		InitTestSection(TestSections.Authentication_Multi);

		App.WaitThenTap("ShowAppButton");

		// NavToHome - this will login using the predefined username/password, and then navigate to home page
		App.WaitThenTap("LoginButton");

		// Retrieving products should work since successful login
		App.WaitThenTap("FetchButton");

		// Logout
		App.WaitThenTap("LogoutButton");
	}

}
