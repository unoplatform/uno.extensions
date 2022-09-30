


namespace TestHarness.Ext.Authentication.Custom;

public class CustomAuthenticationTestBackendCookieHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Authentication.Custom.appsettings.testbackend.json" };

	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder.UseAuthentication(auth =>
					auth.AddCustom<ICustomAuthenticationTestBackendEndpoint>(custom =>
						custom
							.Login(async (authService, dispatcher, cache, credentials, cancellationToken) =>
							{
								try
								{
									if (credentials is null)
									{
										return default;
									}

									var name = credentials.FirstOrDefault(x => x.Key == nameof(CustomAuthenticationCredentials.Username)).Value;
									var password = credentials.FirstOrDefault(x => x.Key == nameof(CustomAuthenticationCredentials.Password)).Value;
									await authService.LoginCookie(name, password, cancellationToken);
									var tokens = await cache.GetAsync(cancellationToken);
									tokens["Expiry"] = DateTime.Now.AddMinutes(10).ToString();
									return tokens;
								}
								catch
								{
									return default;
								}
							})
							.Refresh(async (authService, cache, tokenDictionary, cancellationToken) =>
							{
								try
								{
									var expiry = tokenDictionary["Expiry"];
									await Task.Delay(3000);
									await authService.RefreshCookie(cancellationToken);
									return await cache.GetAsync(cancellationToken);
								}
								catch
								{
									return default;
								}

							})),
							configureAuthorization: builder =>
							{
								builder
									.Cookies("AccessToken", "RefreshToken");
							}
				)

				.ConfigureServices(services =>
					services
							.AddSingleton<IAuthenticationRouteInfo>(
									_ => new AuthenticationRouteInfo<
											CustomAuthenticationLoginViewModel,
											CustomAuthenticationHomeTestBackendViewModel>())
				)
				.UseHttp((ctx, services) =>
						services.AddRefitClient<ICustomAuthenticationTestBackendEndpoint>(ctx));

	}

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
				new ViewMap(ViewModel: typeof(AuthenticationShellViewModel)),
				new ViewMap<CustomAuthenticationLoginPage, CustomAuthenticationLoginViewModel>(),
				new ViewMap<CustomAuthenticationHomeTestBackendPage, CustomAuthenticationHomeTestBackendViewModel>()
				);


		routes
			.Register(
				new RouteMap("", View: views.FindByViewModel<AuthenticationShellViewModel>(),
						Nested: new RouteMap[]
						{
							new RouteMap("Login", View: views.FindByViewModel<CustomAuthenticationLoginViewModel>()),
							new RouteMap("Home", View: views.FindByViewModel<CustomAuthenticationHomeTestBackendViewModel>())
						}));
	}
}


