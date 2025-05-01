namespace TestHarness.UITest;

public class Given_Custom : NavigationTestBase
{
	// [Test]
	public async Task When_Custom_Auth()
	{
		InitTestSection(TestSections.Authentication_Custom);

		App.WaitThenTap("ShowAppButton");

		// NavToHome - this will login using the predefined username/password, and then navigate to home page
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
		// this ensures `CustomAuthenticationProvider` wasn't linked out by the linker
		App.WaitElement("LoginNavigationBar");

		// NavToHome
		await App.TapAndWait("LoginButton", "HomeNavigationBar");

		// Exit the test
		App.WaitThenTap("ExitTestButton");

		// Re-enter the test
		InitTestSection(TestSections.Authentication_Custom_Mock);

		// Expect HomePage instead of LoginPage
		App.WaitElement("HomeNavigationBar");
	}

}
