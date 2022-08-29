
namespace TestHarness.Ext.Authentication.MSAL;
public sealed partial class MsalAuthenticationWelcomePage : Page
{
	internal MsalAuthenticationWelcomeViewModel? ViewModel => DataContext as MsalAuthenticationWelcomeViewModel;

	public MsalAuthenticationWelcomePage()
	{
		this.InitializeComponent();
	}
}
