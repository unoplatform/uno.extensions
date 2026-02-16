namespace TestHarness.Ext.Navigation.TabBarBackNav;

public class TabBarBackNavHostInit : BaseHostInitialization
{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<TabBarBackNavHomePage, TabBarBackNavHomeModel>(),
			new ViewMap<TabBarBackNavSecondPage, TabBarBackNavSecondModel>(),
			new ViewMap<TabBarBackNavThirdPage, TabBarBackNavThirdModel>(),
			new ViewMap<TabBarBackNavFourthPage, TabBarBackNavFourthModel>(),
			new ViewMap<TabBarBackNavSiblingPage, TabBarBackNavSiblingModel>()
		);

		// Route structure matching issue #3016 repro:
		// - Home has a TabBar with nested routes (Second=default, Third, Fourth)
		// - Sibling is a sibling route navigated to via Frame from Home
		// Bug: Navigating to Third tab -> Sibling -> Back should preserve Third tab selection
		//       but instead resets to the default (Second) tab
		routes.Register(
			new RouteMap("",
				Nested:
				[
					new RouteMap("Home", View: views.FindByViewModel<TabBarBackNavHomeModel>(), IsDefault: true,
						Nested:
						[
							new RouteMap("Second", View: views.FindByViewModel<TabBarBackNavSecondModel>(), IsDefault: true),
							new RouteMap("Third", View: views.FindByView<TabBarBackNavThirdPage>()),
							new RouteMap("Fourth", View: views.FindByView<TabBarBackNavFourthPage>()),
						]
					),
					new RouteMap("Sibling", View: views.FindByViewModel<TabBarBackNavSiblingModel>())
				]
			)
		);
	}
}
