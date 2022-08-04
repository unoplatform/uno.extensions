
namespace TestHarness.Ext.Authentication.Oidc;

public sealed partial class OidcAuthenticationHomePage : Page
{
	public OidcAuthenticationHomeViewModel? ViewModel => DataContext as OidcAuthenticationHomeViewModel;
	public OidcAuthenticationHomePage()
	{
		this.InitializeComponent();
	}
}
