


namespace TestHarness.Ext.Authentication.Custom;

public class CustomAuthenticationHostInit : BaseHostInitialization
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

								var authService = sp.GetRequiredService<ICustomAuthenticationDummyJsonEndpoint>();
								var name = credentials.FirstOrDefault(x => x.Key == nameof(CustomAuthenticationCredentials.Username)).Value;
								var password = credentials.FirstOrDefault(x => x.Key == nameof(CustomAuthenticationCredentials.Password)).Value;
								var creds = new CustomAuthenticationCredentials { Username = name, Password = password };
								var authResponse = await authService.Login(creds, CancellationToken.None);
								if (authResponse?.Token is not null)
								{
									credentials[TokenCacheExtensions.AccessTokenKey] = authResponse.Token;
									return credentials;
								}
								return default;
							})
							.Refresh(async (sp, tokenDictionary, cancellationToken) =>
							{
								if (tokenDictionary is null)
								{
									return default;
								}

								var authService = sp.GetRequiredService<ICustomAuthenticationDummyJsonEndpoint>();
								var creds = new CustomAuthenticationCredentials
								{
									Username = tokenDictionary.TryGetValue(nameof(CustomAuthenticationCredentials.Username), out var name) ? name : string.Empty,
									Password = tokenDictionary.TryGetValue(nameof(CustomAuthenticationCredentials.Password), out var password) ? password : string.Empty
								};
								try
								{
									var authResponse = await authService.Login(creds, cancellationToken);
									if (authResponse?.Token is not null)
									{
										tokenDictionary[TokenCacheExtensions.AccessTokenKey] = authResponse.Token;
										return tokenDictionary;
									}
								}
								catch
								{
									// Ignore and just return null;
								}
								return default;
							})))

				.ConfigureServices(services =>
					services
							.AddSingleton<IAuthenticationRouteInfo>(
									_ => new AuthenticationRouteInfo<
											CustomAuthenticationLoginViewModel,
											CustomAuthenticationHomeViewModel>())
				)

				.ConfigureServices((context, services) =>
				{
					services
							.AddNativeHandler(context)

							.AddRefitClient<ICustomAuthenticationDummyJsonEndpoint>(context);
				});

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


