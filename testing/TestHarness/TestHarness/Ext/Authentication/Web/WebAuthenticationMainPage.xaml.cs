namespace TestHarness.Ext.Authentication.Web;

[TestSectionRoot("Authentication: Web",TestSections.Authentication_Web, typeof(WebAuthenticationHostInit))]
[TestSectionRoot("Authentication: Web using appsettings", TestSections.Authentication_Web_Settings, typeof(WebAuthenticationSettingsHostInit))]
public sealed partial class WebAuthenticationMainPage : BaseTestSectionPage
{
	public WebAuthenticationMainPage()
	{
		this.InitializeComponent();
	}

	public async void ShowAppClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateRouteAsync(this,"");
	}
}
