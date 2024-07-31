namespace TestHarness.Ext.Navigation.ListToDetails;

public class ListToDetailsHostInit : BaseHostInitialization
{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<ListToDetailsHomePage, ListToDetailsHomeViewModel>(),
			new ViewMap<ListToDetailsListPage, ListToDetailsListViewModel>(),
			new DataViewMap<ListToDetailsDetailsPage, ListToDetailsDetailsViewModel, Widget>()
			);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("",
				Nested: new[]
				{
						new RouteMap("Home", View: views.FindByViewModel<ListToDetailsHomeViewModel>(),
								Nested: new[]
								{
										new RouteMap(
											"List",
											View: views.FindByViewModel<ListToDetailsListViewModel>(),
											IsDefault: true),
										new RouteMap(
											"Details",
											View: views.FindByViewModel<ListToDetailsDetailsViewModel>(),
											DependsOn: "List")
								}),
				}));
	}
}


