
namespace TestHarness.Ext.Authentication.Oidc;
public sealed partial class OidcAuthenticationLoginPage : Page
{
	internal OidcAuthenticationLoginViewModel? ViewModel => DataContext as OidcAuthenticationLoginViewModel;
	public OidcAuthenticationLoginPage()
	{
		this.InitializeComponent();
	}
}
