


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
								if (credentials is null)
								{
									return default;
								}

								var name = credentials.FirstOrDefault(x => x.Key == nameof(CustomAuthenticationCredentials.Username)).Value;
								var password = credentials.FirstOrDefault(x => x.Key == nameof(CustomAuthenticationCredentials.Password)).Value;
								await authService.LoginCookie(name, password, cancellationToken);
								return await cache.GetAsync(cancellationToken);
							})
							.Refresh(async (authService, cache, tokenDictionary, cancellationToken) =>
							{
								await authService.RefreshCookie(cancellationToken);
								return await cache.GetAsync(cancellationToken);
							})),
							configureAuthorization: builder =>
							{
								builder.Cookies("AccessToken", "RefreshToken");
							}
				)

				.UseAuthenticationFlow(builder =>
						builder
							.OnLoginRequiredNavigateViewModel<CustomAuthenticationLoginViewModel>(this)
							.OnLoginCompletedNavigateViewModel<CustomAuthenticationHomeTestBackendViewModel>(this)
							.OnLogoutNavigateViewModel<CustomAuthenticationLoginViewModel>(this)
						)

				.UseHttp((ctx, services) =>
						services.AddRefitClient<ICustomAuthenticationTestBackendEndpoint>(ctx));

	}

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
				new ViewMap(ViewModel: typeof(CustomAuthenticationShellViewModel)),
				new ViewMap<CustomAuthenticationLoginPage, CustomAuthenticationLoginViewModel>(),
				new ViewMap<CustomAuthenticationHomeTestBackendPage, CustomAuthenticationHomeTestBackendViewModel>()
				);


		routes
			.Register(
				new RouteMap("", View: views.FindByViewModel<CustomAuthenticationShellViewModel>(),
						Nested: new RouteMap[]
						{
							new RouteMap("Login", View: views.FindByViewModel<CustomAuthenticationLoginViewModel>()),
							new RouteMap("Home", View: views.FindByViewModel<CustomAuthenticationHomeTestBackendViewModel>())
						}));
	}
}


