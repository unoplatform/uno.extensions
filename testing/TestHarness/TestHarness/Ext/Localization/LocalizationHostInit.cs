namespace TestHarness;

public class LocalizationHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Localization.appsettings.locale.json" };

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register();


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap(""));
	}
}


