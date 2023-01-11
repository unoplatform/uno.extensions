namespace TestHarness.Ext.Navigation.NavigationView;

public class NavigationViewHostInit : BaseHostInitialization
{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
			new ViewMap<NavigationViewHomePage, NavigationViewHomeViewModel>(),
			new ViewMap<NavigationViewDataBoundPage, NavigationViewDataBoundViewModel>(),
			new ViewMap<NavigationViewSettingsPage, NavigationViewSettingsViewModel>()
			);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("",
				Nested: new[]
				{
						new RouteMap("Home", View: views.FindByViewModel<NavigationViewHomeViewModel>()),
						new RouteMap("DataBound", View: views.FindByViewModel<NavigationViewDataBoundViewModel>(),
						Nested: new[]
						{
							new RouteMap("Profile"),
							new RouteMap("Deals"),
							new RouteMap("Products")
						}),
						new RouteMap("Settings", View: views.FindByViewModel<NavigationViewSettingsViewModel>()),
				}));
	}
}


