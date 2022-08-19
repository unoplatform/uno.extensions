namespace TestHarness.Ext.Navigation.PageNavigation;

public class PageNavigationRegisterHostInit: PageNavigationHostInit
{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<PageNavigationOnePage, PageNavigationOneViewModel>(),
			new ViewMap<PageNavigationTwoPage, PageNavigationTwoViewModel>(),
			new ViewMap<PageNavigationThreePage, PageNavigationThreeViewModel>(),
			new ViewMap<PageNavigationFourPage, PageNavigationFourViewModel>(),
			new ViewMap<PageNavigationFivePage, PageNavigationFiveViewModel>(),
			ConfirmDialog
			);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("",
			Nested: new[]
			{
					new RouteMap("One", View: views.FindByViewModel<PageNavigationOneViewModel>()),
					new RouteMap("Two", View: views.FindByViewModel<PageNavigationTwoViewModel>()),
					new RouteMap("Three", View: views.FindByViewModel<PageNavigationThreeViewModel>()),
					new RouteMap("Four", View: views.FindByViewModel<PageNavigationFourViewModel>()),
					new RouteMap("Five", View: views.FindByViewModel<PageNavigationFiveViewModel>()),
					new RouteMap("Confirm", View: ConfirmDialog),
			}));
	}

}
