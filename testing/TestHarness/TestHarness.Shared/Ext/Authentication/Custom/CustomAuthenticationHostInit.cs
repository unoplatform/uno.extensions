


namespace TestHarness.Ext.Authentication.Custom;

public class CustomAuthenticationHostInit : IHostInitialization
{
	private static IDictionary<string, string> ValidCredentials { get; } = new Dictionary<string, string>()
			{
				{"Bob", "12345" },
				{"Jane", "67890" }
			};


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
					b.AddEmbeddedConfigurationFile<App>("TestHarness.Ext.Authentication.Custom.appsettings.dummyjson.json");
				})

				// Enable navigation, including registering views and viewmodels
				.UseNavigation(RegisterRoutes)

				.UseToolkitNavigation()

				.ConfigureServices((context, services) =>
				{
					services
						.AddSingleton<ITokenCache, TokenCache>();
				})

				.UseAuthentication(builder =>
					builder
						.WithLogin(
								async (sp, dispatcher, tokenCache, credentials, cancellationToken) =>
								{
									var authService = sp.GetRequiredService<ICustomAuthenticationDummyJsonEndpoint>();
									var name = credentials.FirstOrDefault(x => x.Key == "Name").Value;
									var password = credentials.FirstOrDefault(x => x.Key == "Password").Value;
									var creds = new CustomAuthenticationCredentials { Username = name, Password = password };
									var authResponse = await authService.Login(creds,CancellationToken.None);
									if (authResponse?.Token is not null)
									{
										await tokenCache.SaveAsync(credentials);
										return true;
									}
									return false;
								})
						.WithRefresh(
								async (sp, tokenCache, cancellationToken) =>
								{
									var creds = await tokenCache.GetAsync();
									return (creds?.Count() ?? 0) > 0;
								})
						.WithLogout(
							(sp, dispatcher, tokenCache, cancellationToken) => ValueTask.FromResult(true))
				)

				.UseAuthorization(builder => builder.WithAuthorizationHeader())

				.UseAuthenticationFlow(builder=>
						builder
							.OnLoginRequiredNavigateViewModel<CustomAuthenticationLoginViewModel>(this)
							.OnLoginCompletedNavigateViewModel<CustomAuthenticationHomeViewModel>(this)
							.OnLogoutNavigateViewModel<CustomAuthenticationLoginViewModel>(this)
						)

				.UseSerialization()

				.ConfigureServices((context, services) =>
				{
					services
							.AddNativeHandler()
							.AddContentSerializer()

							.AddRefitClient<ICustomAuthenticationDummyJsonEndpoint>(context);
				})
				.Build(enableUnoLogging: true);
	}


	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
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


