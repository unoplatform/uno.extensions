namespace TestHarness.Ext.Navigation.AddressBar;

public sealed partial class AddressBarSecondPage : Page
{
	public AddressBarSecondPage()
	{
		this.InitializeComponent();
	}

	public async void GetUrlFromBrowser(object sender, RoutedEventArgs e)
	{
#if __WASM__
		var url = ImportsJS.GetUrl();

		TxtUrl.Text = url;
#else
		TxtUrl.Text = "Not supported";
#endif
	}
}
#if __WASM__
internal static partial class ImportsJS
{
	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.Uno.Extensions.Hosting.getLocation")]
	public static partial string GetUrl();
}
#endif
