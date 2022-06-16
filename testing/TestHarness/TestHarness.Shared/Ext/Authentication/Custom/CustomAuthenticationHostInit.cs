

using Uno.Extensions.Authentication.Custom;

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
						.AddSingleton<ITokenCache, TokenCache>();
				})

				.UseAuthentication(builder =>
					builder
						.Login(
								async (dispatcher, tokenCache, credentials) =>
								{
									var name = credentials.FirstOrDefault(x => x.Key == "Name").Value;
									var password = credentials.FirstOrDefault(x => x.Key == "Password").Value;
									if (ValidCredentials.TryGetValue(name, out var pass) && pass == password)
									{
										await tokenCache.SaveAsync(credentials);
										return true;
									}
									return false;
								})
						.Refresh(
								async (tokenCache) =>
								{
									var creds = await tokenCache.GetAsync();
									return (creds?.Count() ?? 0) > 0;
								})
						.Logout(
							(dispatcher, tokenCache) => Task.FromResult(true))
				)

				.UseAuthenticationFlow(builder=>
						builder
							.OnLoginRequired(
								async (navigator, dispatcher) =>
								{
									await navigator.NavigateViewModelAsync<CustomAuthenticationLoginViewModel>(this, qualifier: Qualifiers.Root);
								})
							.OnLoginCompleted(
								async (navigator, dispatcher) =>
								{
									await navigator.NavigateViewModelAsync<CustomAuthenticationHomeViewModel>(this, qualifier: Qualifiers.Root);
								})
							.OnLogout(
								async (navigator, dispatcher) =>
								{
									await navigator.NavigateViewModelAsync<CustomAuthenticationLoginViewModel>(this, qualifier: Qualifiers.Root);
								})
						)

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


