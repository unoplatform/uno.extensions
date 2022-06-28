
namespace TestHarness.Ext.Authentication.MSAL;
public sealed partial class MsalAuthenticationWelcomePage : Page
{
	public MsalAuthenticationWelcomeViewModel? ViewModel => DataContext as MsalAuthenticationWelcomeViewModel;

	public MsalAuthenticationWelcomePage()
	{
		this.InitializeComponent();
	}
}
