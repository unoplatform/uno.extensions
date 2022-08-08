


namespace TestHarness.Ext.Authentication.Custom;

public class CustomAuthenticationTestBackendCookieHostInit : IHostInitialization
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
					b.AddEmbeddedConfigurationFile<App>("TestHarness.Ext.Authentication.Custom.appsettings.testbackend.json");
				})

				// Enable navigation, including registering views and viewmodels
				.UseNavigation(RegisterRoutes)

				.UseToolkitNavigation()

				.UseAuthentication(auth =>
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
							.Refresh(async (authService, tokenDictionary, cancellationToken) =>
							{
								// Don't bother refreshing
								return default;
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


				.ConfigureServices((context, services) =>
				{
					services
							.AddNativeHandler()

							.AddRefitClient<ICustomAuthenticationTestBackendEndpoint>(context);
				})
				.Build(enableUnoLogging: true);
	}


	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
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


