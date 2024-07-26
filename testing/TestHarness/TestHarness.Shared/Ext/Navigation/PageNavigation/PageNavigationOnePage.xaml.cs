
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


	public async void GetUrlFromBrowser(object sender, RoutedEventArgs e)
	{
#if __WASM__
		var url = Imports.GetLocation();

		TxtUrl.Text = url;
#else
		TxtUrl.Text = "Not supported";
#endif
	}
}
#if __WASM__
internal static partial class Imports
{
	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.Uno.Extensions.Hosting.getLocation")]
	public static partial string GetLocation();
}
#endif
