namespace TestHarness.Ext.Navigation.Apps.ToDo;

public class ToDoTaskListViewModel
{
	public INavigator Navigator { get; }
	public ToDoTaskList TaskList { get; }

	public ToDoTaskListViewModel(INavigator navigator, ToDoTaskList taskList)
	{
		Navigator = navigator;
		TaskList = taskList;
	}

	public ToDoTask[] ActiveTasks { get; } = new ToDoTask[]
			{
				new ToDoTask("Grocerties "+ Guid.NewGuid().ToString()),
				new ToDoTask("Haircut "+ Guid.NewGuid().ToString()),
				new ToDoTask("Gym "+ Guid.NewGuid().ToString())
			};

	public ToDoTask[] CompletedTasks { get; } = new ToDoTask[]
		{
				new ToDoTask("Clean the car "+ Guid.NewGuid().ToString()),
				new ToDoTask("Dog for a walk "+ Guid.NewGuid().ToString()),
				new ToDoTask("Make dinner "+ Guid.NewGuid().ToString())
		};
}
