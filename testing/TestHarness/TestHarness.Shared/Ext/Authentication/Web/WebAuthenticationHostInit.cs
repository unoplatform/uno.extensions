namespace TestHarness.Ext.Authentication.Web;

public class WebAuthenticationHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Authentication.Web.appsettings.webauth.json" };

	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder
			.UseAuthentication(auth =>
					auth
					.AddWeb<IWebAuthenticationTestEndpoint>(web =>
						web
							.LoginStartUri("https://localhost:7193/webauth/Login/Facebook")
							.PrepareLoginCallbackUri(
									async (service,services, cache, login, ct)=> login!)
										ValueTask.FromResult(login!))
							.LoginCallbackUri("oidc-auth://")
							.PostLogin(async
							(authService, tokens, ct) =>
							{
								return tokens;
							})))

				.UseAuthenticationFlow(builder =>
						builder
							.OnLoginRequiredNavigateViewModel<WebAuthenticationLoginViewModel>(this)
							.OnLoginCompletedNavigateViewModel<WebAuthenticationHomeViewModel>(this)
							.OnLogoutNavigateViewModel<WebAuthenticationLoginViewModel>(this)
						)


				.ConfigureServices((context, services) =>
				{
					services
							.AddNativeHandler()
							.AddContentSerializer()

							.AddRefitClient<IWebAuthenticationTestEndpoint>(context);
				});
	}

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
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





