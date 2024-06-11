namespace TestHarness.Ext.Navigation.Apps.Chefs;

public class ChefsHostInit : BaseHostInitialization
{
	//protected override IHostBuilder Custom(IHostBuilder builder)
	//{
	//	return builder.ConfigureServices(services =>
	//				services
	//						.AddSingleton<IAuthenticationRouteInfo>(
	//								_ => new AuthenticationRouteInfo<
	//										ChefsLoginViewModel,
	//										ChefsHomeViewModel>())
	//			);
	//}

	//protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	//{

	//	views.Register(
	//			new ViewMap(ViewModel: typeof(ChefsShellViewModel)),
	//			new ViewMap<ChefsLoginPage, ChefsLoginViewModel>(ResultData: typeof(ChefsCredentials)),
	//			new DataViewMap<ChefsHomePage,ChefsHomeViewModel, ChefsCredentials>(),
	//			new ViewMap<ChefsProductsPage, ChefsProductsViewModel>(),
	//			new DataViewMap<ChefsProductDetailsPage, ChefsProductDetailsViewModel, ChefsProduct>(),
	//			new ViewMap<ChefsDealsPage, ChefsDealsViewModel>(),
	//			new ViewMap<ChefsProfilePage, ChefsProfileViewModel>(),
	//			new ViewMap<ChefsSettingsPage, ChefsSettingsViewModel>()
	//			);


	//	routes
	//		.Register(
	//			new RouteMap("", View: views.FindByViewModel<ChefsShellViewModel>(),
	//					Nested: new RouteMap[]
	//					{
	//						new RouteMap("Login", View: views.FindByViewModel<ChefsLoginViewModel>()),
	//						new RouteMap("Home", View: views.FindByView<ChefsHomePage>(),
	//								Nested: new RouteMap[]{
	//									new RouteMap("Deals",
	//										View: views.FindByViewModel<ChefsDealsViewModel>(),
	//										IsDefault: true,
	//											Nested: new RouteMap[]{
	//												new RouteMap("DealsTab"),
	//												new RouteMap("FavoritesTab", IsDefault: true)
	//											}),
	//									new RouteMap("DealsProduct",
	//											View: views.FindByViewModel<ChefsProductDetailsViewModel>(),
	//											DependsOn:"Deals"),
	//									new RouteMap("Products",
	//											View: views.FindByViewModel<ChefsProductsViewModel>()),
	//									new RouteMap("Product",
	//											View: views.FindByViewModel<ChefsProductDetailsViewModel>(),
	//											DependsOn:"Products"),

	//									new RouteMap("Profile", View: views.FindByViewModel<ChefsProfileViewModel>())
	//								}),
	//						new RouteMap("Settings", View: views.FindByViewModel<ChefsSettingsViewModel>())
	//					}));
	//}
}


