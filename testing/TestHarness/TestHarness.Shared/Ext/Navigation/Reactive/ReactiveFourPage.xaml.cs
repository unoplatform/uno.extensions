
namespace TestHarness.Ext.Navigation.Reactive;
public sealed partial class ReactiveFourPage : Page
{
	public ReactiveFourViewModel? ViewModel => DataContext as ReactiveFourViewModel;

	public ReactiveFourPage()
	{
		this.InitializeComponent();
	}
	public async void FourPageToFivePageCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateViewAsync<ReactiveFivePage>(this);
	}

	public async void FourPageBackCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateBackAsync(this);
	}


}
