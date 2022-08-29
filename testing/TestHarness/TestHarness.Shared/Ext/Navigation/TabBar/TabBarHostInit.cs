namespace TestHarness.Ext.Navigation.TabBar;

public class TabBarHostInit : BaseHostInitialization
{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
			new ViewMap<TabBarHomePage, TabBarHomeViewModel>(),
			new ViewMap<TabBarSettingsPage, TabBarSettingsViewModel>()
			);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("",
				Nested: new[]
				{
						new RouteMap("Home", View: views.FindByViewModel<TabBarHomeViewModel>()),
						new RouteMap("Settings", View: views.FindByViewModel<TabBarSettingsViewModel>()),
				}));
	}
}


