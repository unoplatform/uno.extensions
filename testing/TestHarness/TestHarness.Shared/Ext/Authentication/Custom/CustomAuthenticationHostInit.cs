

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

				// Enable navigation, including registering views and viewmodels
				.UseNavigation(RegisterRoutes)

				.UseToolkitNavigation()

				.ConfigureServices((context, services) =>
				{
					services
						.AddSingleton<ITokenCache, TokenCache>()
						.AddSingleton(new CustomAuthenticationSettings(
							LoginCallback: async (dispatcher, tokenCache, credentials) =>
								{
									var name = credentials.FirstOrDefault(x => x.Key == "Name").Value;
									var password = credentials.FirstOrDefault(x => x.Key == "Password").Value;
									if (ValidCredentials.TryGetValue(name, out var pass) && pass == password)
									{
										await tokenCache.Save(credentials);
										return true;
									}
									return false;
								},
							RefreshCallback: async (tokenCache) =>
							{
								var creds = await tokenCache.Get();
								return (creds?.Count() ?? 0) > 0;
							},
							LogoutCallback: (dispatcher, tokenCache) => Task.FromResult(true)
							))
						.AddSingleton<IAuthenticationService, CustomAuthenticationService>()
						.AddSingleton(new AuthenticationFlowSettings(
							LoginViewModel: typeof(CustomAuthenticationLoginViewModel),
							HomeViewModel: typeof(CustomAuthenticationHomeViewModel),
							ErrorViewModel: typeof(CustomAuthenticationErrorViewModel)
							))
						.AddTransient<IAuthenticationFlow, AuthenticationFlow>();
				})

				.Build(enableUnoLogging: true);
	}


	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
				new ViewMap(ViewModel: typeof(CustomAuthenticationShellViewModel)),
				new ViewMap<CustomAuthenticationLoginPage, CustomAuthenticationLoginViewModel>(),
				new ViewMap<CustomAuthenticationHomePage, CustomAuthenticationHomeViewModel>(),
				new ViewMap<CustomAuthenticationErrorPage, CustomAuthenticationErrorViewModel>()
				);


		routes
			.Register(
				new RouteMap("", View: views.FindByViewModel<CustomAuthenticationShellViewModel>(),
						Nested: new RouteMap[]
						{
							new RouteMap("Login", View: views.FindByViewModel<CustomAuthenticationLoginViewModel>()),
							new RouteMap("Home", View: views.FindByViewModel<CustomAuthenticationHomeViewModel>()),
							new RouteMap("Error", View: views.FindByViewModel<CustomAuthenticationErrorViewModel>())
						}));
	}
}


