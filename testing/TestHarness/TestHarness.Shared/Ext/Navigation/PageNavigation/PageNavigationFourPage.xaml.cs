
namespace TestHarness.Ext.Navigation.PageNavigation;
public sealed partial class PageNavigationFourPage : Page
{
	public PageNavigationFourViewModel? ViewModel => DataContext as PageNavigationFourViewModel;

	public PageNavigationFourPage()
	{
		this.InitializeComponent();
	}
	public async void FourPageToFivePageCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateViewAsync<PageNavigationFivePage>(this);
	}

	public async void FourPageBackCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateBackAsync(this);
	}


}
