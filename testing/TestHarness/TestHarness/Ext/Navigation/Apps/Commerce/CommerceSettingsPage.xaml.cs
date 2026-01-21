namespace TestHarness.Ext.Navigation.Apps.Commerce;

public sealed partial class CommerceSettingsPage : Page
{
	public CommerceSettingsPage()
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
