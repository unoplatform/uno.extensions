

using Uno.Extensions.Authentication.Handlers;
using Uno.Extensions.Authentication.MSAL;

namespace TestHarness.Ext.Authentication.MSAL;

public class MsalAuthenticationHostInit : IHostInitialization
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

				.UseAuthentication(
					(IMsalAuthenticationBuilder builder) => {
						builder
							.WithClientId("161a9fb5-3b16-487a-81a2-ac45dcc0ad3b")
							.WithScopes(new[] { "Tasks.Read", "User.Read", "Tasks.ReadWrite" })
							.MsalBuilder!
#if __WASM__
								.WithWebRedirectUri();
#else
								.WithRedirectUri("uno-extensions://auth");
#endif
						// TODO: add ios support here - see https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3127
						//if (!string.IsNullOrWhiteSpace(settings.KeychainSecurityGroup))
						//{
						//	builder = builder.WithIosKeychainSecurityGroup(settings.KeychainSecurityGroup);
						//}
					}
				)

				.UseAuthorization(builder => builder.WithAuthorizationHeader())

				.UseAuthenticationFlow(builder=>
						builder
							.OnLoginRequired(
								async (navigator, dispatcher) =>
								{
									await navigator.NavigateViewModelAsync<MsalAuthenticationWelcomeViewModel>(this, qualifier: Qualifiers.Root);
								})
							.OnLoginCompleted(
								async (navigator, dispatcher) =>
								{
									await navigator.NavigateViewModelAsync<MsalAuthenticationHomeViewModel>(this, qualifier: Qualifiers.Root);
								})
							.OnLogout(
								async (navigator, dispatcher) =>
								{
									await navigator.NavigateViewModelAsync<MsalAuthenticationWelcomeViewModel>(this, qualifier: Qualifiers.Root);
								})
						)

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


