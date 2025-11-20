namespace TestHarness.Ext.Navigation.ForResult;

public sealed partial class ForResultSecondPage : Page
{
	public ForResultSecondPage()
	{
		this.InitializeComponent();
	}

	private async void ReturnButton_Click(object sender, RoutedEventArgs e)
	{
		var navigator = this.Navigator();
		if (navigator is not null)
		{
			await navigator.NavigateBackWithResultAsync(this, data: Option.Some("Result from second page"));
		}
	}
}
