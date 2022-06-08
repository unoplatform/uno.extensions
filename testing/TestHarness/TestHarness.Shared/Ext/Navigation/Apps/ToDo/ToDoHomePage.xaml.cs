
namespace TestHarness.Ext.Navigation.Apps.ToDo;

public sealed partial class ToDoHomePage : Page
{
	public ToDoHomeViewModel? ViewModel => DataContext as ToDoHomeViewModel;
	public ToDoHomePage()
	{
		this.InitializeComponent();
	}
}
