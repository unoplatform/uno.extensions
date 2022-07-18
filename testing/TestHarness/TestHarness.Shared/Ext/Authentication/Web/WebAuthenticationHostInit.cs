namespace TestHarness.Ext.Authentication.Web;

public class WebAuthenticationHostInit : IHostInitialization
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
					b.AddEmbeddedConfigurationFile<App>("TestHarness.Ext.Authentication.Web.appsettings.webauth.json");
				})

				// Enable navigation, including registering views and viewmodels
				.UseNavigation(RegisterRoutes)

				.UseToolkitNavigation()

				.UseAuthentication(auth =>
					auth
					.AddWeb(web => 
						web
							.LoginStartUri("https://localhost:7193/webauth/Facebook")
							.LoginCallbackUri("oidc-auth://")
						))

				.UseAuthenticationFlow(builder =>
						builder
							.OnLoginRequiredNavigateViewModel<WebAuthenticationLoginViewModel>(this)
							.OnLoginCompletedNavigateViewModel<WebAuthenticationHomeViewModel>(this)
							.OnLogoutNavigateViewModel<WebAuthenticationLoginViewModel>(this)
						)

				.UseSerialization()

				.ConfigureServices((context, services) =>
				{
					services
							.AddNativeHandler()
							.AddContentSerializer()

							.AddRefitClient<IWebAuthenticationTestEndpoint>(context);
				})
				.Build(enableUnoLogging: true);
	}


	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
				new ViewMap(ViewModel: typeof(WebAuthenticationShellViewModel)),
				new ViewMap<WebAuthenticationLoginPage, WebAuthenticationLoginViewModel>(),
				new ViewMap<WebAuthenticationHomePage, WebAuthenticationHomeViewModel>()
				);


		routes
			.Register(
				new RouteMap("", View: views.FindByViewModel<WebAuthenticationShellViewModel>(),
						Nested: new RouteMap[]
						{
							new RouteMap("Login", View: views.FindByViewModel<WebAuthenticationLoginViewModel>()),
							new RouteMap("Home", View: views.FindByViewModel<WebAuthenticationHomeViewModel>())
						}));
	}
}





