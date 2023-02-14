namespace TestHarness.Ext.Navigation.TabBar;

public class TabBarHostInit : BaseHostInitialization
{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
			new ViewMap<TabBarHomePage, TabBarHomeViewModel>(),
			new ViewMap<TabBarListPage, TabBarListViewModel>(),
			new ViewMap<TabBarSettingsPage, TabBarSettingsViewModel>(),
			new ViewMap<Blank1Page, Blank1ViewModel>(),
			new ViewMap<Blank2Page, Blank2ViewModel>(),
			new ViewMap<Blank2NextPage, Blank2NextViewModel>(),
			new ViewMap<Blank3Page, Blank3ViewModel>()
			);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("",
				Nested: new[]
				{
						new RouteMap("Home", View: views.FindByViewModel<TabBarHomeViewModel>()),
						new RouteMap("List", View: views.FindByViewModel<TabBarListViewModel>(), Nested:
						new RouteMap[]
						{
							new RouteMap("Section1", View: views.FindByViewModel<Blank1ViewModel>(), IsDefault:true),
							new RouteMap("Section2", View: views.FindByViewModel<Blank2ViewModel>()),
							new RouteMap("Blank2Next", View: views.FindByViewModel<Blank2NextViewModel>(), DependsOn:"Section2"),
							new RouteMap("Section3", View: views.FindByViewModel<Blank3ViewModel>()),

						}),
						new RouteMap("Settings", View: views.FindByViewModel<TabBarSettingsViewModel>()),
				}));
	}
}


