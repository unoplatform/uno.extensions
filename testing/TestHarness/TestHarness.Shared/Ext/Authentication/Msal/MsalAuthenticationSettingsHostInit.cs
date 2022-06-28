


namespace TestHarness.Ext.Authentication.MSAL;

public class MsalAuthenticationSettingsHostInit : IHostInitialization
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
					logBuilder.SetMinimumLevel(host.IsDevelopment() ? LogLevel.Warning : LogLevel.Information);
				})

				.UseConfiguration()

				// Only use this syntax for UI tests - use UseConfiguration in apps
				.ConfigureAppConfiguration((ctx, b) =>
				{
					b.AddEmbeddedConfigurationFile<App>("TestHarness.Ext.Authentication.Msal.appsettings.msalauthentication.json");
					b.AddEmbeddedConfigurationFile<App>("TestHarness.Ext.Authentication.Msal.appsettings.msal.json");
				})

				// Enable navigation, including registering views and viewmodels
				.UseNavigation(RegisterRoutes)

				.UseToolkitNavigation()

				.UseMsalAuthentication()

				.UseAuthenticationFlow(builder=>
						builder
							.OnLoginRequiredNavigateViewModel<MsalAuthenticationWelcomeViewModel>(this)
							.OnLoginCompletedNavigateViewModel<MsalAuthenticationHomeViewModel>(this)
							.OnLogoutNavigateViewModel<MsalAuthenticationWelcomeViewModel>(this)
						)

				.ConfigureServices((context, services) =>
				{
					services
							.AddNativeHandler()
							.AddRefitClient<IMsalAuthenticationTaskListEndpoint>(context);
				})

				.Build(enableUnoLogging: true);
	}


	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
				new ViewMap(ViewModel: typeof(MsalAuthenticationShellViewModel)),
				new ViewMap<MsalAuthenticationWelcomePage, MsalAuthenticationWelcomeViewModel>(),
				new ViewMap<MsalAuthenticationHomePage, MsalAuthenticationHomeViewModel>()
				);


		routes
			.Register(
				new RouteMap("", View: views.FindByViewModel<MsalAuthenticationShellViewModel>(),
						Nested: new RouteMap[]
						{
							new RouteMap("Welcome", View: views.FindByViewModel<MsalAuthenticationWelcomeViewModel>()),
							new RouteMap("Home", View: views.FindByViewModel<MsalAuthenticationHomeViewModel>())
						}));
	}
}


