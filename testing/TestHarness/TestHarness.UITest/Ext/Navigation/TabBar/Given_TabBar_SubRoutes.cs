namespace TestHarness.UITest;

/// <summary>
/// Tests for sub-route support in TabBarItem region names.
/// Both TabBarItems on HomePage use a composite region name where the first
/// segment is the intermediate page to navigate to ("ProductListsPage") and
/// the second segment is the sub-route to activate within that page:
///   Tab 1 "Products"  — Region.Name="ProductListsPage/AllProducts"
///   Tab 2 "Favorites" — Region.Name="ProductListsPage/Favorites"
/// </summary>
public class Given_TabBar_SubRoutes : NavigationTestBase
{
	[Test]
	public async Task When_TabBar_SubRoutes()
	{
		InitTestSection(TestSections.Navigation_TabBar_SubRoutes);

		// Load the sub-routes home page
		App.WaitThenTap("ShowTabBarSubRoutesHomeButton");
		App.WaitElement("TabBarSubRoutesHomeNavigationBar");

		// Tap the "Products" tab — uses Region.Name="ProductListsPage/AllProducts"
		App.WaitThenTap("ProductsTabBarItem");
		App.WaitElement("TabBarSubRoutesAllProductsTextBlock");

		// Tap the "Favorites" tab — uses Region.Name="ProductListsPage/Favorites"
		App.WaitThenTap("FavoritesTabBarItem");
		App.WaitElement("TabBarSubRoutesFavoritesPageTextBlock");

		// Switch back to Products
		App.WaitThenTap("ProductsTabBarItem");
		App.WaitElement("TabBarSubRoutesAllProductsTextBlock");
	}
}
