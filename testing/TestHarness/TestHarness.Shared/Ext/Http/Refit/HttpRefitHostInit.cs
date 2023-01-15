using TestHarness.Ext.Http.Refit;

namespace TestHarness;

public class HttpRefitHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Http.Refit.appsettings.httprefit.json" };

	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder
				.UseHttp(
			configure: (context, services) =>
			 services.AddRefitClient<IHttpRefitDummyJsonEndpoint>(context, name: "HttpRefitDummyJsonEndpoint"));

	}

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register();


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap(""));
	}
}


