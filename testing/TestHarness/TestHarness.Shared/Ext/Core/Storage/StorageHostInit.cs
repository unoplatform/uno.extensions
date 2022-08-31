using System.Collections.Immutable;

namespace TestHarness;

public class StorageHostInit : BaseHostInitialization
{
	
	//protected override IHostBuilder Configuration(IHostBuilder builder)
	//{
	//	return builder.UseConfiguration(configure: builder =>
	//	{
	//		return builder.Section<StorageSettings>();
	//	});
	//}


	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		

		views.Register(
			);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap(""));
	}
}


