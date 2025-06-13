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
		var url = AddressBarJSImports.GetUrl();

		TxtUrl.Text = url;
#else
		TxtUrl.Text = "Not supported";
#endif
	}
}
