namespace TestHarness.Ext.Navigation.TabBar;

/// <summary>
/// Demonstrates sub-route support for TabBarItems:
/// Both TabBarItems on HomePage navigate to the same intermediate page
/// ("ProductListsPage") but with different sub-routes:
///   Tab 1 "Products"  → Region.Name="ProductListsPage/AllProducts"
///   Tab 2 "Favorites" → Region.Name="ProductListsPage/Favorites"
/// The first segment of the composite region name is the page nested inside
/// HomePage; the second segment is the sub-region to activate within that page.
/// </summary>
public class TabBarSubRoutesHostInit : BaseHostInitialization
{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<TabBarSubRoutesHomePage, TabBarSubRoutesHomeViewModel>(),
			new ViewMap<TabBarSubRoutesProductListsPage, TabBarSubRoutesProductListsViewModel>(),
			new ViewMap<TabBarSubRoutesProductsPage, TabBarSubRoutesProductsViewModel>(),
			new ViewMap<TabBarSubRoutesFavoritesPage, TabBarSubRoutesFavoritesViewModel>()
		);

		routes.Register(
			new RouteMap("",
				Nested: new[]
				{
					new RouteMap("Home",
						View: views.FindByViewModel<TabBarSubRoutesHomeViewModel>(),
						IsDefault: true,
						Nested: new RouteMap[]
						{
							new RouteMap("ProductListsPage",
								View: views.FindByViewModel<TabBarSubRoutesProductListsViewModel>(),
								Nested: new RouteMap[]
								{
									new RouteMap("AllProducts", View: views.FindByViewModel<TabBarSubRoutesProductsViewModel>()),
									new RouteMap("Favorites", View: views.FindByViewModel<TabBarSubRoutesFavoritesViewModel>()),
								}),
						}),
				}));
	}
}
