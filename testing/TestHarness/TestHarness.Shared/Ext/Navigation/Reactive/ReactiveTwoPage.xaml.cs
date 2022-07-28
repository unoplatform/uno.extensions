namespace TestHarness.Ext.Navigation.Reactive;

public sealed partial class ReactiveTwoPage : Page
{
	public ReactiveTwoPage()
	{
		this.InitializeComponent();
	}

	public async void TwoPageToThreePageCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateViewAsync<ReactiveThreePage>(this);
	}

	public async void TwoPageBackCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateBackAsync(this);
	}

}
