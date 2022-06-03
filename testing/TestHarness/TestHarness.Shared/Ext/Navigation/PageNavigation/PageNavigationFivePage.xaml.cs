

namespace TestHarness.Ext.Navigation.PageNavigation;

public sealed partial class PageNavigationFivePage : Page
{
	public PageNavigationFiveViewModel? ViewModel => DataContext as PageNavigationFiveViewModel;

	public PageNavigationFivePage()
	{
		this.InitializeComponent();
	}

	public async void FivePageBackCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateBackAsync(this);
	}

}
