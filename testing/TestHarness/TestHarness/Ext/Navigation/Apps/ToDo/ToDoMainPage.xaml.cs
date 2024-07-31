namespace TestHarness.Ext.Navigation.Apps.ToDo;

[TestSectionRoot("Sample App: ToDo", TestSections.Apps_ToDo, typeof(ToDoHostInit))]
public sealed partial class ToDoMainPage : BaseTestSectionPage
{
	public ToDoMainPage()
	{
		this.InitializeComponent();
	}

	public async void ShowAppClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateRouteAsync(this,"");
	}
	public async void NarrowClick(object sender, RoutedEventArgs e)
	{
		VisualStateManager.GoToState(this, nameof(NarrowWindow), true);
	}

	public async void WideClick(object sender, RoutedEventArgs e)
	{
		VisualStateManager.GoToState(this, nameof(WideWindow), true);
	}
}
