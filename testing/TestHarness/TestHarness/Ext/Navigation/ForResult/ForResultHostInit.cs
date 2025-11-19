namespace TestHarness;

public class ForResultHostInit : BaseHostInitialization
{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<ForResultFirstPage>(),
			new ViewMap<ForResultSecondPage, ForResultSecondViewModel>()
		);

		// RouteMap for the test section
		routes.Register(
			new RouteMap("", View: views.FindByViewModel<ForResultFirstPage>())
		);
	}
}
