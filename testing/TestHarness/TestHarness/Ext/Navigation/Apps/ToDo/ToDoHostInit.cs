namespace TestHarness.Ext.Navigation.Apps.ToDo;

public class ToDoHostInit : BaseHostInitialization
{
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
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
					new RouteMap("Active", IsDefault:true),
					new RouteMap("Completed")
				}),
				new("Task", View: views.FindByViewModel<ToDoTaskViewModel>(), DependsOn:"TaskList"),
				new("Settings", View: views.FindByViewModel<ToDoSettingsViewModel>())
			})
		);
	}
}


