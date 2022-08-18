
namespace TestHarness.Ext.Navigation.PageNavigation;

public sealed partial class PageNavigationOnePage : Page
{
	public PageNavigationOneViewModel? ViewModel => DataContext as PageNavigationOneViewModel;
	public PageNavigationOnePage()
	{
		this.InitializeComponent();
	}

	public async void OnePageToTwoPageCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateViewAsync<PageNavigationTwoPage>(this);
	}

}
