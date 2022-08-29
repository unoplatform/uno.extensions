using TestHarness.Ext.Authentication;

namespace TestHarness.Ext.Navigation.Apps.Commerce;

public class CommerceHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Navigation.Apps.Commerce.appsettings.logging.json" };

	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder.ConfigureServices(services =>
					services
							.AddSingleton<IAuthenticationRouteInfo>(
									_ => new AuthenticationRouteInfo<
											CommerceLoginViewModel,
											CommerceHomeViewModel>())
				);
	}

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
				new ViewMap(ViewModel: typeof(CommerceShellViewModel)),
				new ViewMap<CommerceLoginPage, CommerceLoginViewModel>(ResultData: typeof(CommerceCredentials)),
				new DataViewMap<CommerceHomePage,CommerceHomeViewModel, CommerceCredentials>(),
				new ViewMap<CommerceProductsPage, CommerceProductsViewModel>(),
				new DataViewMap<CommerceProductDetailsPage, CommerceProductDetailsViewModel, CommerceProduct>(),
				new ViewMap<CommerceDealsPage, CommerceDealsViewModel>(),
				new ViewMap<CommerceProfilePage, CommerceProfileViewModel>()
				);


		routes
			.Register(
				new RouteMap("", View: views.FindByViewModel<CommerceShellViewModel>(),
						Nested: new RouteMap[]
						{
							new RouteMap("Login", View: views.FindByViewModel<CommerceLoginViewModel>()),
							new RouteMap("Home", View: views.FindByView<CommerceHomePage>(),
									Nested: new RouteMap[]{
										new RouteMap("Deals",
											View: views.FindByViewModel<CommerceDealsViewModel>(),
											IsDefault: true,
												Nested: new RouteMap[]{
													new RouteMap("DealsTab"),
													new RouteMap("FavoritesTab", IsDefault: true)
												}),
										new RouteMap("DealsProduct",
												View: views.FindByViewModel<CommerceProductDetailsViewModel>(),
												DependsOn:"Deals"),
										new RouteMap("Products",
												View: views.FindByViewModel<CommerceProductsViewModel>()),
										new RouteMap("Product",
												View: views.FindByViewModel<CommerceProductDetailsViewModel>(),
												DependsOn:"Products"),

										new RouteMap("Profile", View: views.FindByViewModel<CommerceProfileViewModel>())
									})
						}));
	}
}


