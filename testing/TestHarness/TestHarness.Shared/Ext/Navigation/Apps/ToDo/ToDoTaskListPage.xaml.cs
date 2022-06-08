namespace TestHarness.Ext.Navigation.Apps.ToDo;
public sealed partial class ToDoTaskListPage : Page
{
	public ToDoTaskListViewModel? ViewModel => DataContext as ToDoTaskListViewModel;

	public ToDoTaskListPage()
	{
		this.InitializeComponent();
	}
}
