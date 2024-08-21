namespace TestHarness.Ext.Navigation.RoutesNavigation;

public class RoutesNavigationHostInit : BaseHostInitialization
	{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<ContentControlHomePage, ContentControlHomeViewModel>()
			);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("",
				Nested: new[]
				{
						new RouteMap("Home", View: views.FindByViewModel<ContentControlHomeViewModel>())
				}));
	}
}
