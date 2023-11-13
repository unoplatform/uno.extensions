

namespace TestHarness.Ext.Navigation.Reactive;

public sealed partial class ReactiveSixPage : Page
{
	public ReactiveSixPage()
	{
		this.InitializeComponent();
	}

	public async void SixPageBackCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateBackAsync(this);
	}

}
