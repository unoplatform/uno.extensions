namespace TestHarness.Ext.Navigation.Apps.ToDo;

public record ToDoTaskListViewModel(INavigator Navigator)
{
	public ToDoTask[] ActiveTasks { get; } = new ToDoTask[]
			{
				new ToDoTask("Grocerties"),
				new ToDoTask("Haircut"),
				new ToDoTask("Gym")
			};

	public ToDoTask[] CompletedTasks { get; } = new ToDoTask[]
		{
				new ToDoTask("Clean the car"),
				new ToDoTask("Dog for a walk"),
				new ToDoTask("Make dinner")
		};
}
