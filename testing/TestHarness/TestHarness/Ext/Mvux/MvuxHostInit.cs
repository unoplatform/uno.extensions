namespace TestHarness.Ext.Mvux;

public class MvuxHostInit : BaseHostInitialization
{
	protected override IHostBuilder Navigation(IHostBuilder builder)
	{
		return builder.UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes);
	}

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<MvuxFeedPage, MvuxFeedModel>(),
			new ViewMap<MvuxListFeedPage, MvuxListFeedModel>(),
			new ViewMap<MvuxStatePage, MvuxStateModel>()
		);

		routes.Register(
			new RouteMap("",
				Nested: new[]
				{
					new RouteMap("Feed", View: views.FindByViewModel<MvuxFeedModel>()),
					new RouteMap("ListFeed", View: views.FindByViewModel<MvuxListFeedModel>()),
					new RouteMap("State", View: views.FindByViewModel<MvuxStateModel>()),
				}));
	}
}
