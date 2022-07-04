


namespace TestHarness.Ext.Authentication.MSAL;

public static class HostBuilderExtensions
{
	public static IHostBuilder Defaults(this IHostBuilder builder, IHostInitialization hostInit)
	{
		return builder
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

				// Enable navigation, including registering views and viewmodels
				.UseNavigation(RegisterRoutes)

				.UseToolkitNavigation()

				.UseAuthenticationFlow(builder =>
						builder
							.OnLoginRequiredNavigateViewModel<MsalAuthenticationWelcomeViewModel>(hostInit)
							.OnLoginCompletedNavigateViewModel<MsalAuthenticationHomeViewModel>(hostInit)
							.OnLogoutNavigateViewModel<MsalAuthenticationWelcomeViewModel>(hostInit)
						)

				.ConfigureServices((context, services) =>
				{
					services
							.AddNativeHandler()
							.AddRefitClient<IMsalAuthenticationTaskListEndpoint>(context);
				});
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


