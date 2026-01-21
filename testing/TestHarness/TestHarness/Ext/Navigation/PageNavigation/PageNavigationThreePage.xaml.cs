
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

	public async void GetUrlFromBrowser(object sender, RoutedEventArgs e)
	{
#if __WASM__
		var url = AddressBarJSImports.GetUrl();

		TxtUrl.Text = url;
#else
		TxtUrl.Text = "Not supported";
#endif
	}
}
