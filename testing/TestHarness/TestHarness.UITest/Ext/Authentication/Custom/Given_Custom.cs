namespace TestHarness.UITest;

public class Given_Custom : NavigationTestBase
{
	// TODO: Fix UI Test fail
	// [Test]
	public async Task When_Custom()
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

}
