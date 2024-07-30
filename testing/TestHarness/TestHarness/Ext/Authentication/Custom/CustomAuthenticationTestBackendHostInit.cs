namespace TestHarness.Ext.Authentication.Custom;

public class CustomAuthenticationTestBackendHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Authentication.Custom.appsettings.testbackend.json" };

	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder.UseAuthentication(auth =>
					auth.AddCustom<ICustomAuthenticationTestBackendEndpoint>(custom =>
						custom
							.Login(async (authService, dispatcher, credentials, cancellationToken) =>
							{
								if (credentials is null)
								{
									return default;
								}

								var name = credentials.FirstOrDefault(x => x.Key == nameof(CustomAuthenticationCredentials.Username)).Value;
								var password = credentials.FirstOrDefault(x => x.Key == nameof(CustomAuthenticationCredentials.Password)).Value;
								var authResponse = await authService.Login(name, password, cancellationToken);
								if (authResponse?.AccessToken is not null)
								{
									credentials[TokenCacheExtensions.AccessTokenKey] = authResponse.AccessToken;
									return credentials;
								}
								return default;
							})
							.Refresh(async (authService, tokenDictionary, cancellationToken) =>
							{
								var (Username, Password) = (
									tokenDictionary.TryGetValue(nameof(CustomAuthenticationCredentials.Username), out var name) ? name : string.Empty,
									tokenDictionary.TryGetValue(nameof(CustomAuthenticationCredentials.Password), out var password) ? password : string.Empty
								);

								try
								{
									var authResponse = await authService.Login(Username, Password, cancellationToken);
									if (authResponse?.AccessToken is not null)
									{
										tokenDictionary[TokenCacheExtensions.AccessTokenKey] = authResponse.AccessToken;
										return tokenDictionary;
									}
								}
								catch
								{
									// Ignore and return null
								}
								return default;
							}))
				)

				.ConfigureServices(services =>
					services
							.AddSingleton<IAuthenticationRouteInfo>(
									_ => new AuthenticationRouteInfo<
											CustomAuthenticationLoginViewModel,
											CustomAuthenticationHomeTestBackendViewModel>())
				)

				.ConfigureServices((context, services) =>
				{
					services
							.AddNativeHandler(context)

							.AddRefitClient<ICustomAuthenticationTestBackendEndpoint>(context);
				});
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


