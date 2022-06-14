namespace TestHarness.Ext.Navigation.Responsive;

public class ResponsiveHostInit : IHostInitialization
{
	public IHost InitializeHost()
	{

		return UnoHost
				.CreateDefaultBuilder()
#if DEBUG
				// Switch to Development environment when running in DEBUG
				.UseEnvironment(Environments.Development)
#endif

				// Add platform specific log providers
				.UseLogging(configure: (context, logBuilder) =>
				{
					var host = context.HostingEnvironment;
					// Configure log levels for different categories of logging
					logBuilder.SetMinimumLevel(host.IsDevelopment() ? LogLevel.Warning : LogLevel.Information);
				})

				// Enable navigation, including registering views and viewmodels
				.UseNavigation(RegisterRoutes)

				.Build(enableUnoLogging: true);
	}


	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
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


