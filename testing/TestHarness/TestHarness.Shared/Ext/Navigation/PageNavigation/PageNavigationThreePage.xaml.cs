
namespace TestHarness.Ext.Navigation.PageNavigation;
public sealed partial class PageNavigationThreePage : Page
{
	public PageNavigationThreeViewModel? ViewModel => DataContext as PageNavigationThreeViewModel;

	public PageNavigationThreePage()
	{
		this.InitializeComponent();
	}

	public async void ThreePageToFourPageCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateViewAsync<PageNavigationFourPage>(this);
	}
	public async void ThreePageBackCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateBackAsync(this);
	}


}
