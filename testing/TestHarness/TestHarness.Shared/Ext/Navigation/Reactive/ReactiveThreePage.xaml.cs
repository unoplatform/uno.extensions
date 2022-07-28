
namespace TestHarness.Ext.Navigation.Reactive;
public sealed partial class ReactiveThreePage : Page
{
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
