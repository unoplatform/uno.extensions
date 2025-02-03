using TestHarness.Ext.Http.Kiota.Client;
using Uno.Extensions.Http;
using Uno.Extensions.Http.Kiota;

namespace TestHarness.Ext.Http.Kiota;
public class KiotaHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => ["TestHarness.Ext.Http.Kiota.appsettings.json"];

	protected override IHostBuilder Custom(IHostBuilder builder) =>
		builder.ConfigureServices((context, services) =>
		{
			services.AddKiotaClient<KiotaTestClient>(context, options: new EndpointOptions { Url = "https://localhost:7193" });
			services.AddTransient<KiotaHomeViewModel>();
		});

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<KiotaMainPage>(),
			new ViewMap<KiotaHomePage, KiotaHomeViewModel>()
		);

		routes.Register(
			new RouteMap("", View: views.FindByView<KiotaMainPage>()),
			new RouteMap("Home", View: views.FindByViewModel<KiotaHomeViewModel>())
		);
	}
}
