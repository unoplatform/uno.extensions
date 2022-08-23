


namespace TestHarness.Ext.Authentication.Custom;

public class CustomAuthenticationHostInit : BaseHostInitialization
{
	public IHost InitializeHost()
	{

		return UnoHost
				.CreateDefaultBuilder()
#if DEBUG
				// Switch to Development environment when running in DEBUG
				.UseEnvironment(Environments.Development)
#endif

				// Add platform specific log providers
				.UseLogging(configure: (context, logBuilder) =>
				{
					var host = context.HostingEnvironment;
					// Configure log levels for different categories of logging
					logBuilder.SetMinimumLevel(host.IsDevelopment() ? LogLevel.Trace : LogLevel.Information);
				})

				.UseConfiguration()

				// Only use this syntax for UI tests - use UseConfiguration in apps
				.ConfigureAppConfiguration((ctx, b) =>
				{
					b.AddEmbeddedConfigurationFile<App>("TestHarness.Ext.Authentication.Custom.appsettings.dummyjson.json");
				})

				// Enable navigation, including registering views and viewmodels
				.UseNavigation(RegisterRoutes)

				.UseToolkitNavigation()

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
								if(tokenDictionary is null)
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

				.UseAuthenticationFlow(flow =>
					flow
						.OnLoginRequiredNavigateViewModel<CustomAuthenticationLoginViewModel>(this)
						.OnLoginCompletedNavigateViewModel<CustomAuthenticationHomeViewModel>(this)
						.OnLogoutNavigateViewModel<CustomAuthenticationLoginViewModel>(this))


				.ConfigureServices((context, services) =>
				{
					services
							.AddNativeHandler()

							.AddRefitClient<ICustomAuthenticationDummyJsonEndpoint>(context);
				})
				.Build(enableUnoLogging: true);
	}


	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
				new ViewMap(ViewModel: typeof(CustomAuthenticationShellViewModel)),
				new ViewMap<CustomAuthenticationLoginPage, CustomAuthenticationLoginViewModel>(),
				new ViewMap<CustomAuthenticationHomePage, CustomAuthenticationHomeViewModel>()
				);


		routes
			.Register(
				new RouteMap("", View: views.FindByViewModel<CustomAuthenticationShellViewModel>(),
						Nested: new RouteMap[]
						{
							new RouteMap("Login", View: views.FindByViewModel<CustomAuthenticationLoginViewModel>()),
							new RouteMap("Home", View: views.FindByViewModel<CustomAuthenticationHomeViewModel>())
						}));
	}
}


