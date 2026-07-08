namespace TestHarness.Ext.Navigation.TabBar.TabBarBackNavigation;

/// <summary>
/// Host initialization for testing TabBar back navigation state restoration.
/// This test validates that when navigating back to a page with TabBar,
/// the previously selected tab is maintained instead of resetting to default.
/// See: https://github.com/unoplatform/uno.extensions/issues/3016
/// </summary>
public class TabBarBackNavHostInit : BaseHostInitialization
{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<TabBarBackNavShell>(),
			new ViewMap<TabBarBackNavMainPage, TabBarBackNavMainModel>(),
			new ViewMap<TabBarBackNavSiblingPage, TabBarBackNavSiblingModel>(),
			new ViewMap<TabBarBackNavSecondSection>(),
			new ViewMap<TabBarBackNavThirdSection>(),
			new ViewMap<TabBarBackNavFourthSection>()
		);

		// Route structure matching the issue:
		// - Shell (root, provides a Frame for forward/back page navigation)
		//   - Main (with TabBar containing Second, Third, Fourth)
		//   - Sibling (separate page at same level)
		// The shell is required so the root ContentControl renders content,
		// which creates child regions that can process IsDefault navigation.
		routes.Register(
			new RouteMap("", View: views.FindByView<TabBarBackNavShell>(),
				Nested:
				[
					new RouteMap("Main", View: views.FindByViewModel<TabBarBackNavMainModel>(), IsDefault: true,
						Nested:
						[
							new RouteMap("Second", View: views.FindByView<TabBarBackNavSecondSection>(), IsDefault: true),
							new RouteMap("Third", View: views.FindByView<TabBarBackNavThirdSection>()),
							new RouteMap("Fourth", View: views.FindByView<TabBarBackNavFourthSection>()),
						]
					),
					new RouteMap("Sibling", View: views.FindByViewModel<TabBarBackNavSiblingModel>())
				]
			)
		);
	}
}
