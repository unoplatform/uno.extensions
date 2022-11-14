
using TestHarness.Ext.AdHoc;

namespace TestHarness;

public class AdHocHostInit : BaseHostInitialization
{

	protected override IHostBuilder Serialization(IHostBuilder builder) => builder.UseSerialization(
						services => services
										.AddJsonTypeInfo(AdHocWidgetContext.Default.AdHocWidget)
										.AddJsonTypeInfo(AdHocPersonContext.Default.AdHocPerson)
										.AddJsonTypeInfo(AdHocImmutableContext.Default.AdHocImmutable)
					);

	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder.ConfigureServices(services => services.AddScoped<AdHocNeedsADispatcherService>());
	}

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register();


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap(""));
	}
}

