namespace TestHarness.UITest;

public class Given_TabBar : NavigationTestBase
{
	[Test]
	public async Task When_TabBar()
	{
		InitTestSection(TestSections.Navigation_TabBar);


		// Load the TabBar home page
		App.WaitThenTap("ShowTabBarHomeButton");
		App.WaitElement("TabBarHomeNavigationBar");

		// Check basic nav item selection
		App.WaitThenTap("ProductsTabBarItem");
		CheckProductsVisible();
		App.WaitThenTap("DealsTabBarItem");
		CheckDealsVisible();
		App.WaitThenTap("ProfileTabBarItem");
		CheckProfileVisible();
		App.WaitThenTap("ProductsTabBarItem");
		CheckProductsVisible();

		// Check nav from buttons in views
		App.WaitThenTap("ProductsTabBarItem");
		CheckProductsVisible();
		App.WaitThenTap("ProductsDealsButton");
		CheckDealsVisible();
		App.WaitThenTap("ProductsTabBarItem");
		CheckProductsVisible();
		App.WaitThenTap("ProductsProfileButton");
		CheckProfileVisible();

		App.WaitThenTap("DealsTabBarItem");
		CheckDealsVisible();
		App.WaitThenTap("DealsProductsButton");
		CheckProductsVisible();
		App.WaitThenTap("DealsTabBarItem");
		CheckDealsVisible();
		App.WaitThenTap("DealsProfileButton");
		CheckProfileVisible();

		App.WaitThenTap("ProfileTabBarItem");
		CheckProfileVisible();
		App.WaitThenTap("ProfileProductsButton");
		CheckProductsVisible();
		App.WaitThenTap("ProfileTabBarItem");
		CheckProfileVisible();
		App.WaitThenTap("ProfileDealsButton");
		CheckDealsVisible();

	}

	private void CheckProductsVisible()
	{
		App.WaitElement("ProductsDealsButton");
		var text = App.MarkedAnywhere("CurrentTabBarItemTextBlock").GetText();
		text.Should().Be("Products");
		var isVisible = App.MarkedAnywhere("ProductsStackPanel").IsVisible();
		isVisible.Should().Be(true);
		isVisible = App.MarkedAnywhere("DealsStackPanel").IsVisible();
		isVisible.Should().Be(false);
		isVisible = App.MarkedAnywhere("ProfileStackPanel").IsVisible();
		isVisible.Should().Be(false);
	}

	private void CheckDealsVisible()
	{
		App.WaitElement("DealsProductsButton");
		var text = App.MarkedAnywhere("CurrentTabBarItemTextBlock").GetText();
		text.Should().Be("Deals");
		var isVisible = App.MarkedAnywhere("ProductsStackPanel").IsVisible();
		isVisible.Should().Be(false);
		isVisible = App.MarkedAnywhere("DealsStackPanel").IsVisible();
		isVisible.Should().Be(true);
		isVisible = App.MarkedAnywhere("ProfileStackPanel").IsVisible();
		isVisible.Should().Be(false);

	}


	private void CheckProfileVisible()
	{
		App.WaitElement("ProfileProductsButton");
		var text = App.MarkedAnywhere("CurrentTabBarItemTextBlock").GetText();
		text.Should().Be("Profile");
		var isVisible = App.MarkedAnywhere("ProductsStackPanel").IsVisible();
		isVisible.Should().Be(false);
		isVisible = App.MarkedAnywhere("DealsStackPanel").IsVisible();
		isVisible.Should().Be(false);
		isVisible = App.MarkedAnywhere("ProfileStackPanel").IsVisible();
		isVisible.Should().Be(true);

	}
}
