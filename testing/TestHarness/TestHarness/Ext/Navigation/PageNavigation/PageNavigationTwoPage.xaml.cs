namespace TestHarness.Ext.Navigation.PageNavigation;

public sealed partial class PageNavigationTwoPage : Page
{
	public PageNavigationTwoViewModel? ViewModel => DataContext as PageNavigationTwoViewModel;

	public PageNavigationTwoPage()
	{
		this.InitializeComponent();
	}

	public async void TwoPageToThreePageCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateViewAsync<PageNavigationThreePage>(this);
	}

	public async void TwoPageBackCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateBackAsync(this);
	}

	public async void GetUrlFromBrowser(object sender, RoutedEventArgs e)
	{
#if __WASM__
		var url = PageNavigationJSImports.GetLocation();

		TxtUrl.Text = url;
#else
		TxtUrl.Text = "Not supported";
#endif
	}
}
