namespace TestHarness.Ext.Navigation.TabBar.TabBarClearBackStack;

/// <summary>
/// Host initialization for testing navigation back to tabbed root from a deep page.
/// Setup:
/// - Shell implements IContentControlProvider (ExtendedSplashScreen, not Frame)
/// - Shell ViewMap has only ViewModel (no View type)
/// - ShellModel navigates to Root via NavigateViewAsync with Qualifiers.Root
/// - Root route does NOT have IsDefault (navigated to explicitly by ShellModel)
/// - Root route uses FindByView (not FindByViewModel)
/// - Details uses ResultDataViewMap
/// - Multiple sibling routes at Shell level
/// See: https://github.com/unoplatform/uno.extensions/issues/72
/// </summary>
public class TabBarClearBackStackHostInit : BaseHostInitialization
{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			// Shell has no View type, only ViewModel
			new ViewMap(ViewModel: typeof(TabBarClearBackStackShellModel)),
			new ViewMap<TabBarClearBackStackRootPage, TabBarClearBackStackRootModel>(),
			new ViewMap<TabBarClearBackStackHomePage, TabBarClearBackStackHomeModel>(),
			new ViewMap<TabBarClearBackStackTabTwoPage, TabBarClearBackStackTabTwoModel>(),
			// Details uses ResultDataViewMap with data
			new ResultDataViewMap<TabBarClearBackStackDetailPage, TabBarClearBackStackDetailModel, string>(
				Data: new DataMap<string>())
		);

		// Route structure:
		// - Shell (IContentControlProvider with ExtendedSplashScreen)
		//   - Login (sibling route)
		//   - Profile (sibling route)
		//   - Details (sibling route — navigated from TabTwo tab)
		//   - Root (NO IsDefault — ShellModel navigates here explicitly)
		//     - Home (default tab)
		//     - TabTwo (tab)
		//
		// The bug: from Details, navigating to "/Root/Home" with ClearBackStack
		// should pop Details off the stack and return to Root with the Home tab
		// selected. Instead it creates a new Home page without the TabBar.
		routes.Register(
			new RouteMap("", View: views.FindByViewModel<TabBarClearBackStackShellModel>(),
				Nested:
				[
					// Sibling routes
					new RouteMap("Login", View: views.FindByViewModel<TabBarClearBackStackHomeModel>()),
					new RouteMap("Profile", View: views.FindByViewModel<TabBarClearBackStackHomeModel>()),
					new RouteMap("Details", View: views.FindByViewModel<TabBarClearBackStackDetailModel>()),
					// Root has NO IsDefault — ShellModel navigates here explicitly
					new RouteMap("Root", View: views.FindByView<TabBarClearBackStackRootPage>(),
						Nested:
						[
							new RouteMap("Home", View: views.FindByViewModel<TabBarClearBackStackHomeModel>(), IsDefault: true),
							new RouteMap("TabTwo", View: views.FindByViewModel<TabBarClearBackStackTabTwoModel>()),
						]
					)
				]
			)
		);
	}
}
