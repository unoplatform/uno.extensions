namespace TestHarness.Ext.Navigation.Apps.ToDo;
public sealed partial class ToDoTaskListPage : Page
{
	public ToDoTaskListViewModel? ViewModel => DataContext as ToDoTaskListViewModel;

	public ToDoTaskListPage()
	{
		this.InitializeComponent();

		

	}

	public void SelectActiveTask1Click(object sender, RoutedEventArgs e)
	{
		ActiveTasks.SelectedIndex = 0;
	}
	public void SelectActiveTask2Click(object sender, RoutedEventArgs e)
	{
		ActiveTasks.SelectedIndex = 1;
	}
	public void SelectActiveTask3Click(object sender, RoutedEventArgs e)
	{
		ActiveTasks.SelectedIndex = 2;
	}

	public void SelectCompletedTask1Click(object sender, RoutedEventArgs e)
	{
		CompletedTasks.SelectedIndex = 0;
	}
	public void SelectCompletedTask2Click(object sender, RoutedEventArgs e)
	{
		CompletedTasks.SelectedIndex = 1;
	}
	public void SelectCompletedTask3Click(object sender, RoutedEventArgs e)
	{
		CompletedTasks.SelectedIndex = 2;
	}
}
