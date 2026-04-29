namespace TestHarness.UITest;

/// <summary>
/// Tests for sub-route support in TabBarItem region names.
/// A composite region name like "Home/Products" navigates to the "Home" region
/// and then to the "Products" sub-route within it. Two TabBarItems can therefore
/// share the same base region ("Home") but each select a different sub-route.
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

		// Tap the "Products" tab — uses Region.Name="Home/Products"
		App.WaitThenTap("HomeProductsTabBarItem");
		App.WaitElement("TabBarSubRoutesProductsPageTextBlock");

		// Tap the "Favorites" tab — uses Region.Name="Home/Favorites"
		App.WaitThenTap("HomeFavoritesTabBarItem");
		App.WaitElement("TabBarSubRoutesFavoritesPageTextBlock");

		// Switch back to Products
		App.WaitThenTap("HomeProductsTabBarItem");
		App.WaitElement("TabBarSubRoutesProductsPageTextBlock");
	}
}
