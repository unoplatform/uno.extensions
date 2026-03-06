namespace TestHarness.Ext.Navigation.TabBar.TabBarClearBackStack;

/// <summary>
/// Host initialization for testing navigation back to tabbed root from a deep page.
/// This replicates the exact setup of the driver app:
/// - Shell implements IContentControlProvider (ExtendedSplashScreen, not Frame)
/// - Shell ViewMap has only ViewModel (no View type)
/// - ShellModel navigates to Root via NavigateViewAsync with Qualifiers.Root
/// - Root route does NOT have IsDefault (navigated to explicitly by ShellModel)
/// - Root route uses FindByView (not FindByViewModel)
/// - StopDetails uses ResultDataViewMap
/// - Multiple sibling routes at Shell level (like the real app)
/// See: https://github.com/unoplatform/uno.extensions/issues/72
/// </summary>
public class TabBarClearBackStackHostInit : BaseHostInitialization
{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			// Shell has no View type, only ViewModel — matches driver app pattern
			new ViewMap(ViewModel: typeof(TabBarClearBackStackShellModel)),
			new ViewMap<TabBarClearBackStackRootPage, TabBarClearBackStackRootModel>(),
			new ViewMap<TabBarClearBackStackHomePage, TabBarClearBackStackHomeModel>(),
			new ViewMap<TabBarClearBackStackMyRunPage, TabBarClearBackStackMyRunModel>(),
			// StopDetails uses ResultDataViewMap with data, matching driver app
			new ResultDataViewMap<TabBarClearBackStackStopDetailPage, TabBarClearBackStackStopDetailModel, string>(
				Data: new DataMap<string>())
		);

		// Route structure mirrors the driver app exactly:
		// - Shell (IContentControlProvider with ExtendedSplashScreen)
		//   - Login (sibling route, like driver app has ~40 siblings)
		//   - Profile (sibling route)
		//   - StopDetails (sibling route — navigated from MyRun tab)
		//   - Root (NO IsDefault — ShellModel navigates here explicitly)
		//     - Home (default tab)
		//     - MyRun (tab)
		//
		// The bug: from StopDetails, navigating to "/Root/Home" with ClearBackStack
		// should pop StopDetails off the stack and return to Root with the Home tab
		// selected. Instead it creates a new Home page without the TabBar.
		routes.Register(
			new RouteMap("", View: views.FindByViewModel<TabBarClearBackStackShellModel>(),
				Nested:
				[
					// Sibling routes (like driver app's Login, Profile, etc.)
					new RouteMap("Login", View: views.FindByViewModel<TabBarClearBackStackHomeModel>()),
					new RouteMap("Profile", View: views.FindByViewModel<TabBarClearBackStackHomeModel>()),
					new RouteMap("StopDetails", View: views.FindByViewModel<TabBarClearBackStackStopDetailModel>()),
					// Root has NO IsDefault — ShellModel navigates here explicitly
					new RouteMap("Root", View: views.FindByView<TabBarClearBackStackRootPage>(),
						Nested:
						[
							new RouteMap("Home", View: views.FindByViewModel<TabBarClearBackStackHomeModel>(), IsDefault: true),
							new RouteMap("MyRun", View: views.FindByViewModel<TabBarClearBackStackMyRunModel>()),
						]
					)
				]
			)
		);
	}
}
