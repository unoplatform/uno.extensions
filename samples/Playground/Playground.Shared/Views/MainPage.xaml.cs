namespace Playground.Views;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
		this.InitializeComponent();
	}

	private void AlternateClick(object sender, RoutedEventArgs e)
	{
		this.Frame.Navigate(typeof(AlternatePage));
	}
}
