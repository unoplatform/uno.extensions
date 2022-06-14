namespace TestHarness.Ext.Navigation.Apps.ToDo;

public record ToDoHomeViewModel(INavigator Navigator)
{
	public ToDoTaskList[] Lists { get; } = new[]
		{
			new ToDoTaskList("Important"),
			new ToDoTaskList("Tasks"),
			new ToDoTaskList("Custom List")
		};
}
