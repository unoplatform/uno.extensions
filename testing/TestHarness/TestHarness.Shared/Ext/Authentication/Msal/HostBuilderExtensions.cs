
namespace TestHarness.Ext.Authentication.MSAL;

public abstract class BaseMsalHostInitialization : BaseHostInitialization
{
	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder
				.UseAuthenticationFlow(builder =>
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
				});
	}


	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
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


