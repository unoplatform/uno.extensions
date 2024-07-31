
namespace TestHarness.Ext.Navigation.Apps.ToDo;

public sealed partial class ToDoWelcomePage : Page
{
	public ToDoWelcomeViewModel? ViewModel => DataContext as ToDoWelcomeViewModel;
	public ToDoWelcomePage()
	{
		this.InitializeComponent();
	}
}
