namespace TestHarness.Ext.Navigation.RoutesNavigation;

public class RoutesNavigationHostInit : BaseHostInitialization
	{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<HomePage>(),
			new ViewMap<SamplePage>(),
			new ViewMap<ListTemplate>(),
			new ViewMap<ItemsPage>(),
			new ViewMap(ViewModel: typeof(ShellViewModel))
			);


		 //RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
				Nested: new[]
				{
						new RouteMap("HomePage", View: views.FindByView<HomePage>()),
						new RouteMap("SamplePage", View: views.FindByView<SamplePage>()),
						new RouteMap("List_Template", View: views.FindByView<ListTemplate>()),
						new RouteMap("ItemsPage", View: views.FindByView<ItemsPage>()),
				}));
	}
}
