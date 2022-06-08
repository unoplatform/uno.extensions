namespace TestHarness.Ext.Navigation.Apps.ToDo;

public class ToDoHostInit : IHostInitialization
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

				.UseToolkitNavigation()

				.Build(enableUnoLogging: true);
	}


	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
			// Views
			new ViewMap<ToDoHomePage, ToDoHomeViewModel>(),
			new ViewMap<ToDoSettingsFlyout, ToDoSettingsViewModel>(),
			new ViewMap(ViewModel: typeof(ToDoShellViewModel)),
			new ViewMap<ToDoWelcomePage, ToDoWelcomeViewModel>(),
			new DataViewMap<ToDoTaskListPage, ToDoTaskListViewModel, ToDoTaskList>(),
			new DataViewMap<ToDoTaskPage, ToDoTaskViewModel, ToDoTask>()
		);

		routes.Register(
			new RouteMap("", View: views.FindByViewModel<ToDoShellViewModel>(), Nested: new RouteMap[]
			{
				new("Welcome", View: views.FindByViewModel<ToDoWelcomeViewModel>()),
				new("Home", View: views.FindByViewModel<ToDoHomeViewModel>()),
				new("TaskList", View: views.FindByViewModel<ToDoTaskListViewModel>(), Nested: new[]
				{
					new RouteMap("MultiTaskLists", IsDefault: true, Nested: new[]
					{
						new RouteMap("ToDo", IsDefault:true),
						new RouteMap("Completed")
					})
				}),
				new("Task", View: views.FindByViewModel<ToDoTaskViewModel>(), DependsOn:"TaskList"),
				new("Settings", View: views.FindByViewModel<ToDoSettingsViewModel>())
			})
		);
	}
}


