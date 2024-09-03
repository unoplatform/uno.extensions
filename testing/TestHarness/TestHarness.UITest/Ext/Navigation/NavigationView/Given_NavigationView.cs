namespace TestHarness.UITest;

public class Given_NavigationView : NavigationTestBase
{
	[Test]
	public async Task When_NavigationView()
	{
		InitTestSection(TestSections.Navigation_NavigationView);


		// Load the NavigationView home page
		App.WaitThenTap("ShowNavigationViewHomeButton");
		App.WaitElement("NavigationViewHomeNavigationBar");

		// Check basic nav item selection
		App.WaitThenTap("ProductsNavigationViewItem");
		CheckProductsVisible();
		App.WaitThenTap("DealsNavigationViewItem");
		CheckDealsVisible();
		App.WaitThenTap("ProfileNavigationViewItem");
		CheckProfileVisible();
		App.WaitThenTap("ProductsNavigationViewItem");
		CheckProductsVisible();

		// Check nav from buttons in views
		App.WaitThenTap("ProductsNavigationViewItem");
		CheckProductsVisible();
		App.WaitThenTap("ProductsDealsButton");
		CheckDealsVisible();
		App.WaitThenTap("ProductsNavigationViewItem");
		CheckProductsVisible();
		App.WaitThenTap("ProductsProfileButton");
		CheckProfileVisible();

		App.WaitThenTap("DealsNavigationViewItem");
		CheckDealsVisible();
		App.WaitThenTap("DealsProductsButton");
		CheckProductsVisible();
		App.WaitThenTap("DealsNavigationViewItem");
		CheckDealsVisible();
		App.WaitThenTap("DealsProfileButton");
		CheckProfileVisible();

		App.WaitThenTap("ProfileNavigationViewItem");
		CheckProfileVisible();
		App.WaitThenTap("ProfileProductsButton");
		CheckProductsVisible();
		App.WaitThenTap("ProfileNavigationViewItem");
		CheckProfileVisible();
		App.WaitThenTap("ProfileDealsButton");
		CheckDealsVisible();

	}

	private void CheckProductsVisible()
	{
		App.WaitForText("CurrentNavigationViewItemTextBlock", "Products");
		var isVisible = App.MarkedAnywhere("ProductsStackPanel").IsVisible();
		isVisible.Should().Be(true);
		isVisible = App.MarkedAnywhere("DealsStackPanel").IsVisible();
		isVisible.Should().Be(false);
		isVisible = App.MarkedAnywhere("ProfileStackPanel").IsVisible();
		isVisible.Should().Be(false);
	}

	private void CheckDealsVisible()
	{
		App.WaitForText("CurrentNavigationViewItemTextBlock", "Deals");
		var isVisible = App.MarkedAnywhere("ProductsStackPanel").IsVisible();
		isVisible.Should().Be(false);
		isVisible = App.MarkedAnywhere("DealsStackPanel").IsVisible();
		isVisible.Should().Be(true);
		isVisible = App.MarkedAnywhere("ProfileStackPanel").IsVisible();
		isVisible.Should().Be(false);

	}


	private void CheckProfileVisible()
	{
		App.WaitForText("CurrentNavigationViewItemTextBlock", "Profile");
		var isVisible = App.MarkedAnywhere("ProductsStackPanel").IsVisible();
		isVisible.Should().Be(false);
		isVisible = App.MarkedAnywhere("DealsStackPanel").IsVisible();
		isVisible.Should().Be(false);
		isVisible = App.MarkedAnywhere("ProfileStackPanel").IsVisible();
		isVisible.Should().Be(true);

	}
}
