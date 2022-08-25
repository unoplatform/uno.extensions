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
			new ViewMap<PageNavigationSixPage, PageNavigationSixViewModel>(),
			new ViewMap<PageNavigationSevenPage, PageNavigationSevenViewModel>(),
			new ViewMap<PageNavigationEightPage, PageNavigationEightViewModel>(),
			new ViewMap<PageNavigationNinePage, PageNavigationNineViewModel>(),
			new ViewMap<PageNavigationTenPage, PageNavigationTenViewModel>(),
			ConfirmDialog
			);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("",
			Nested: new[]
			{
					new RouteMap("One", View: views.FindByViewModel<PageNavigationOneViewModel>()),
					new RouteMap("Two", View: views.FindByViewModel<PageNavigationTwoViewModel>(), IsDefault: true, DependsOn:"One"),
					new RouteMap("Three", View: views.FindByViewModel<PageNavigationThreeViewModel>()),
					new RouteMap("Four", View: views.FindByViewModel<PageNavigationFourViewModel>()),
					new RouteMap("Five", View: views.FindByViewModel<PageNavigationFiveViewModel>()),
					new RouteMap("Six", View: views.FindByViewModel<PageNavigationSixViewModel>()),
					new RouteMap("Seven", View: views.FindByViewModel<PageNavigationSevenViewModel>(), DependsOn: "Six"),
					new RouteMap("Eight", View: views.FindByViewModel<PageNavigationEightViewModel>(), DependsOn: "Seven"),
					new RouteMap("Nine", View: views.FindByViewModel<PageNavigationNineViewModel>(), DependsOn: "Eight"),
					new RouteMap("Ten", View: views.FindByViewModel<PageNavigationTenViewModel>(), DependsOn: "Nine"),
					new RouteMap("Confirm", View: ConfirmDialog),
			}));
	}

}
