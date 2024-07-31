
namespace TestHarness.Ext.Navigation.Apps.ToDo;

public sealed partial class ToDoTaskPage : Page
{
	public ToDoTaskViewModel? ViewModel => DataContext as ToDoTaskViewModel;

	public ToDoTaskPage()
	{
		this.InitializeComponent();
	}
}
