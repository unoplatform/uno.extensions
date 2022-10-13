namespace TestHarness.Ext.Authentication.Oidc;

[TestSectionRoot("Authentication: Oidc",TestSections.Authentication_Oidc, typeof(OidcAuthenticationHostInit))]
public sealed partial class OidcAuthenticationMainPage : BaseTestSectionPage
{
	public OidcAuthenticationMainPage()
	{
		this.InitializeComponent();
	}

	public async void ShowAppClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateRouteAsync(this,"");
	}
}
