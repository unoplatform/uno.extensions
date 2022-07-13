namespace TestHarness.Ext.Authentication.Oidc;

public class OidcAuthenticationHostInit : IHostInitialization
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
					b.AddEmbeddedConfigurationFile<App>("TestHarness.Ext.Authentication.Oidc.appsettings.oidc.json");
				})

				// Enable navigation, including registering views and viewmodels
				.UseNavigation(RegisterRoutes)

				.UseToolkitNavigation()

				.ConfigureServices((context, services) =>
				{
					services
						.AddSingleton<ITokenCache, TokenCache>();
				})

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

				.UseAuthenticationFlow(builder =>
						builder
							.OnLoginRequiredNavigateViewModel<OidcAuthenticationLoginViewModel>(this)
							.OnLoginCompletedNavigateViewModel<OidcAuthenticationHomeViewModel>(this)
							.OnLogoutNavigateViewModel<OidcAuthenticationLoginViewModel>(this)
						)

				.UseSerialization()

				.ConfigureServices((context, services) =>
				{
					services
							.AddNativeHandler()
							.AddContentSerializer()

							.AddRefitClient<IOidcAuthenticationTestEndpoint>(context);
				})
				.Build(enableUnoLogging: true);
	}


	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
				new ViewMap(ViewModel: typeof(OidcAuthenticationShellViewModel)),
				new ViewMap<OidcAuthenticationLoginPage, OidcAuthenticationLoginViewModel>(),
				new ViewMap<OidcAuthenticationHomePage, OidcAuthenticationHomeViewModel>()
				);


		routes
			.Register(
				new RouteMap("", View: views.FindByViewModel<OidcAuthenticationShellViewModel>(),
						Nested: new RouteMap[]
						{
							new RouteMap("Login", View: views.FindByViewModel<OidcAuthenticationLoginViewModel>()),
							new RouteMap("Home", View: views.FindByViewModel<OidcAuthenticationHomeViewModel>())
						}));
	}
}





