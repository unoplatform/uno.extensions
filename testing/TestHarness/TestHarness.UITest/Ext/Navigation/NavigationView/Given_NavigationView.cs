using static System.Net.Mime.MediaTypeNames;

namespace TestHarness.UITest;

public class Given_NavigationView : NavigationTestBase
{
	[Test]
	public async Task When_NavigationView()
	{
		InitTestSection(TestSections.NavigationView);


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
		var text = App.Marked("CurrentNavigationViewItemTextBlock").GetText();
		text.Should().Be("Products");
		var isVisible = App.Marked("ProductsStackPanel").IsVisible();
		isVisible.Should().Be(true);
		isVisible = App.Marked("DealsStackPanel").IsVisible();
		isVisible.Should().Be(false);
		isVisible = App.Marked("ProfileStackPanel").IsVisible();
		isVisible.Should().Be(false);
	}

	private void CheckDealsVisible()
	{
		var text = App.Marked("CurrentNavigationViewItemTextBlock").GetText();
		text.Should().Be("Deals");
		var isVisible = App.Marked("ProductsStackPanel").IsVisible();
		isVisible.Should().Be(false);
		isVisible = App.Marked("DealsStackPanel").IsVisible();
		isVisible.Should().Be(true);
		isVisible = App.Marked("ProfileStackPanel").IsVisible();
		isVisible.Should().Be(false);

	}


	private void CheckProfileVisible()
	{
		var text = App.Marked("CurrentNavigationViewItemTextBlock").GetText();
		text.Should().Be("Profile");
		var isVisible = App.Marked("ProductsStackPanel").IsVisible();
		isVisible.Should().Be(false);
		isVisible = App.Marked("DealsStackPanel").IsVisible();
		isVisible.Should().Be(false);
		isVisible = App.Marked("ProfileStackPanel").IsVisible();
		isVisible.Should().Be(true);

	}
}
