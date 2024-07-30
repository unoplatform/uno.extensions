namespace TestHarness.Ext.Navigation.Responsive;

public class ResponsiveHostInit : BaseHostInitialization
{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<ResponsiveHomePage, ResponsiveHomeViewModel>(),
			new ViewMap<ResponsiveListPage, ResponsiveListViewModel>(),
			new DataViewMap<ResponsiveDetailsPage, ResponsiveDetailsViewModel, Widget>()
			);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("",
				Nested: new[]
				{
						new RouteMap("Home", View: views.FindByViewModel<ResponsiveHomeViewModel>(),
								Nested: new[]
								{
										new RouteMap(
											"List",
											View: views.FindByViewModel<ResponsiveListViewModel>(),
											IsDefault: true),
										new RouteMap(
											"Details",
											View: views.FindByViewModel<ResponsiveDetailsViewModel>(),
											DependsOn: "List")
								}),
				}));
	}
}


