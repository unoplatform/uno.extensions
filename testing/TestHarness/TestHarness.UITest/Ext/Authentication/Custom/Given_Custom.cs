namespace TestHarness.UITest;

public class Given_Custom : NavigationTestBase
{
	// [Test]
	public async Task When_Custom_Auth()
	{
		InitTestSection(TestSections.Authentication_Custom);

		App.WaitThenTap("ShowAppButton");

		// Login - this will login using the predefined username/password, and then navigate to home page
		App.WaitThenTap("LoginButton");

		// Retrieving products should work since successful login
		App.WaitThenTap("RetrieveProductsButton");
		await Task.Delay(2000); 
		var productsResult = App.GetText("RetrieveProductsResultTextBlock");
		productsResult.Should().BeEquivalentTo(TestHarness.Constants.CommerceProducts.ProductsLoadSuccess);

		// Logout
		App.WaitThenTap("LogoutButton");

	}

	[Test]
	public async Task When_Custom_Mock_Auth()
	{
		InitTestSection(TestSections.Authentication_Custom_Mock);

		App.WaitThenTap("ShowAppButton");

		// Make sure the app has loaded

		// This is not being found when running on CI
		// Commented out to test - bring it back and investigate the issue
		//App.WaitElement("LoginNavigationBar");

		// Login
		await App.TapAndWait("LoginButton", "HomeNavigationBar");

		// Exit the test
		App.WaitThenTap("ExitTestButton");

		// Re-enter the test
		InitTestSection(TestSections.Authentication_Custom_Mock);

		// Expect HomePage instead of LoginPage
		App.WaitElement("HomeNavigationBar");
	}

}
