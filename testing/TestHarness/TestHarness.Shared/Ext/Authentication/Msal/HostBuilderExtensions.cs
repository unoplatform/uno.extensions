
namespace TestHarness.Ext.Authentication.MSAL;

public abstract class BaseMsalHostInitialization : BaseHostInitialization
{
	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder
				.ConfigureServices(services =>
					services
							.AddSingleton<IAuthenticationRouteInfo>(
									_ => new AuthenticationRouteInfo<
											MsalAuthenticationWelcomeViewModel,
											MsalAuthenticationHomeViewModel>())
				)
				.ConfigureServices((context, services) =>
				{
					services
							.AddNativeHandler(context)
							.AddRefitClient<IMsalAuthenticationTaskListEndpoint>(context);
				});
	}


	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
				new ViewMap(ViewModel: typeof(AuthenticationShellViewModel)),
				new ViewMap<MsalAuthenticationWelcomePage, MsalAuthenticationWelcomeViewModel>(),
				new ViewMap<MsalAuthenticationHomePage, MsalAuthenticationHomeViewModel>()
				);


		routes
			.Register(
				new RouteMap("", View: views.FindByViewModel<AuthenticationShellViewModel>(),
						Nested: new RouteMap[]
						{
							new RouteMap("Welcome", View: views.FindByViewModel<MsalAuthenticationWelcomeViewModel>()),
							new RouteMap("Home", View: views.FindByViewModel<MsalAuthenticationHomeViewModel>())
						}));
	}
}


