namespace TestHarness.Ext.Navigation.TabBar;

/// <summary>
/// Demonstrates sub-route support for TabBarItems:
/// Two TabBarItems share the same base region ("Home") but navigate to different
/// sub-routes within it ("Products" and "Favorites") using composite region names
/// like "Home/Products" and "Home/Favorites".
/// </summary>
public class TabBarSubRoutesHostInit : BaseHostInitialization
{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<TabBarSubRoutesHomePage, TabBarSubRoutesHomeViewModel>(),
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
							new RouteMap("Products", View: views.FindByViewModel<TabBarSubRoutesProductsViewModel>()),
							new RouteMap("Favorites", View: views.FindByViewModel<TabBarSubRoutesFavoritesViewModel>()),
						}),
				}));
	}
}
