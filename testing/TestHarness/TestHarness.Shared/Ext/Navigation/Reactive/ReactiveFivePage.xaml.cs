

namespace TestHarness.Ext.Navigation.Reactive;

public sealed partial class ReactiveFivePage : Page
{
	public ReactiveFivePage()
	{
		this.InitializeComponent();
	}

	public async void FivePageToSixPageCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateViewAsync<ReactiveSixPage>(this);
	}

	public async void FivePageBackCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateBackAsync(this);
	}

}
