


namespace TestHarness.Ext.Authentication.Custom;

public class CustomAuthenticationNoServiceHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Authentication.Custom.appsettings.dummyjson.json" };

	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder
			.UseAuthentication(auth =>
					auth.AddCustom(custom =>
						custom
							.Login(async (sp, dispatcher, credentials, cancellationToken) =>
							{
								if (credentials is null)
								{
									return default;
								}
								credentials[TokenCacheExtensions.AccessTokenKey] = "Any access key";
								return credentials;
							})
							.Refresh(async (sp, tokenDictionary, cancellationToken) =>
							{
								if (tokenDictionary is null)
								{
									return default;
								}

								
								return default;
							})))

				.ConfigureServices(services =>
					services
							.AddSingleton<IAuthenticationRouteInfo>(
									_ => new AuthenticationRouteInfo<
											CustomAuthenticationLoginViewModel,
											CustomAuthenticationHomeViewModel>())
				);

	}

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
				new ViewMap(ViewModel: typeof(AuthenticationShellViewModel)),
				new ViewMap<CustomAuthenticationLoginPage, CustomAuthenticationLoginViewModel>(),
				new ViewMap<CustomAuthenticationHomePage, CustomAuthenticationHomeViewModel>()
				);


		routes
			.Register(
				new RouteMap("", View: views.FindByViewModel<AuthenticationShellViewModel>(),
						Nested: new RouteMap[]
						{
							new RouteMap("Login", View: views.FindByViewModel<CustomAuthenticationLoginViewModel>()),
							new RouteMap("Home", View: views.FindByViewModel<CustomAuthenticationHomeViewModel>())
						}));
	}
}


