namespace TestHarness.UITest;

public class Given_Msal : NavigationTestBase
{
	// TODO: Fix UI Test fail
	// [Test]
	public async Task When_Multi()
	{
		InitTestSection(TestSections.Authentication_Multi);

		App.WaitThenTap("ShowAppButton");

		// Login - this will login using the predefined username/password, and then navigate to home page
		App.WaitThenTap("LoginButton");

		// Retrieving products should work since successful login
		App.WaitThenTap("FetchButton");

		// Logout
		App.WaitThenTap("LogoutButton");
	}

}
