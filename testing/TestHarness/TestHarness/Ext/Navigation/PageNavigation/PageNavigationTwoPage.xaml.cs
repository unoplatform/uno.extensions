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
		var url = JSImports.GetLocation();

		TxtUrl.Text = url;
#else
		TxtUrl.Text = "Not supported";
#endif
	}
}
#if __WASM__
internal static partial class JSImports
{
	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.Uno.Extensions.Hosting.getLocation")]
	public static partial string GetLocation();
}
#endif

