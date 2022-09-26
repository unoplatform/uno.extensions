using TestHarness.Ext.Http.Endpoints;

namespace TestHarness;

public class HttpEndpointsHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Http.Endpoints.appsettings.httpendpoints.json" };

	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder
				.UseHttp(
			configure: (context, services) =>
			 services.AddClient<HttpEndpointsOneViewModel>(context, name: "HttpDummyJsonEndpoint"));

	}

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register();


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap(""));
	}
}


