using TestHarness;
using TestHarness.Ext.Http.Kiota;
using TestHarness.Ext.Http.Kiota.Client;
using Uno.Extensions.Http;
using Uno.Extensions.Http.Kiota;

public class KiotaHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => new[] { "TestHarness.Ext.Http.Kiota.appsettings.json" };

	protected override IHostBuilder Custom(IHostBuilder builder) =>
		builder.ConfigureServices((context, services) =>
		{
			services.AddKiotaClient<PostsApiClient>(context, options: new EndpointOptions { Url = "https://jsonplaceholder.typicode.com" });
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
