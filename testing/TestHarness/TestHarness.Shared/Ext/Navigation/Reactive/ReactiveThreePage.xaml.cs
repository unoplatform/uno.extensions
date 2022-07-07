
namespace TestHarness.Ext.Navigation.Reactive;
public sealed partial class ReactiveThreePage : Page
{
	public ReactiveThreeViewModel? ViewModel => DataContext as ReactiveThreeViewModel;

	public ReactiveThreePage()
	{
		this.InitializeComponent();
	}

	public async void ThreePageToFourPageCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateViewAsync<ReactiveFourPage>(this);
	}
	public async void ThreePageBackCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateBackAsync(this);
	}


}
