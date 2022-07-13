
namespace TestHarness.Ext.Authentication.Oidc;
public sealed partial class OidcAuthenticationLoginPage : Page
{
	public OidcAuthenticationLoginViewModel? ViewModel => DataContext as OidcAuthenticationLoginViewModel;
	public OidcAuthenticationLoginPage()
	{
		this.InitializeComponent();
	}
}
