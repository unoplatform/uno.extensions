

namespace TestHarness.Ext.Navigation.Reactive;

public sealed partial class ReactiveFivePage : Page
{
	public ReactiveFiveViewModel? ViewModel => DataContext as ReactiveFiveViewModel;

	public ReactiveFivePage()
	{
		this.InitializeComponent();
	}

	public async void FivePageBackCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateBackAsync(this);
	}

}
