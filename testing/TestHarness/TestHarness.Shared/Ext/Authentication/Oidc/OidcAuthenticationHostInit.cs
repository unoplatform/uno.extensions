namespace TestHarness.Ext.Authentication.Oidc;

public class OidcAuthenticationHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Authentication.Oidc.appsettings.oidc.json" };

	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder
							.UseAuthentication(auth =>
					auth
					.AddOidc(oidc =>
						oidc
							.Authority("https://demo.duendesoftware.com/")
							.ClientId("interactive.confidential")
							.ClientSecret("secret")
							.Scope("openid profile email api offline_access")
							.RedirectUri("oidc-auth://callback")
							.PostLogoutRedirectUri("oidc-auth://callback")
						))

				.ConfigureServices(services =>
					services
							.AddSingleton<IAuthenticationRouteInfo>(
									_ => new AuthenticationRouteInfo<
											OidcAuthenticationLoginViewModel,
											OidcAuthenticationHomeViewModel>())
				)

				.ConfigureServices((context, services) =>
				{
					services
							.AddNativeHandler(context)
							.AddContentSerializer(context)

							.AddRefitClient<IOidcAuthenticationTestEndpoint>(context);
				});
	}

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
				new ViewMap(ViewModel: typeof(AuthenticationShellViewModel)),
				new ViewMap<OidcAuthenticationLoginPage, OidcAuthenticationLoginViewModel>(),
				new ViewMap<OidcAuthenticationHomePage, OidcAuthenticationHomeViewModel>()
				);


		routes
			.Register(
				new RouteMap("", View: views.FindByViewModel<AuthenticationShellViewModel>(),
						Nested: new RouteMap[]
						{
							new RouteMap("Login", View: views.FindByViewModel<OidcAuthenticationLoginViewModel>()),
							new RouteMap("Home", View: views.FindByViewModel<OidcAuthenticationHomeViewModel>())
						}));
	}
}





