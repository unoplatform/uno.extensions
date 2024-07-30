
namespace TestHarness.Ext.Authentication.Custom;

public class CustomAuthenticationServiceHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Authentication.Custom.appsettings.dummyjson.json" };

	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder.UseAuthentication(auth =>
					auth.AddCustom<ICustomAuthenticationDummyJsonEndpoint>(custom =>
						custom
							.Login(async (authService, services, dispatcher, cache, credentials, cancellationToken) =>
							{
								if (credentials is null)
								{
									return default;
								}

								var name = credentials.FirstOrDefault(x => x.Key == nameof(CustomAuthenticationCredentials.Username)).Value;
								var password = credentials.FirstOrDefault(x => x.Key == nameof(CustomAuthenticationCredentials.Password)).Value;
								var creds = new CustomAuthenticationCredentials { Username = name, Password = password };
								var authResponse = await authService.Login(creds, cancellationToken);
								if (authResponse?.Token is not null)
								{
									credentials[TokenCacheExtensions.AccessTokenKey] = authResponse.Token;

									var w = new Widget { Name = "Bob" };
									var serializer = services.GetRequiredService<ISerializer<Widget>>();
									credentials.Set(serializer, nameof(Widget), w);

									return credentials;
								}
								return default;
							})
							.Refresh(async (authService, services, cache, tokenDictionary, cancellationToken) =>
							{
								var serializer = services.GetRequiredService<ISerializer<Widget>>();
								var widget = tokenDictionary.Get(serializer, nameof(Widget));
								if (widget is null)
								{
									return default;
								}

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
										tokenDictionary.Set<Widget>(serializer, nameof(Widget), widget);
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
											CustomAuthenticationHomeViewModel>())
				)

				.ConfigureServices((context, services) =>
				{
					services
							.AddNativeHandler(context)
							.AddTransient<DelegatingHandler, DynamicUrlHandler>()
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


