
using TestHarness.Ext.Navigation.AddressBar;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public sealed partial class ChefsHomePage : Page
{
	public ChefsHomePage()
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
