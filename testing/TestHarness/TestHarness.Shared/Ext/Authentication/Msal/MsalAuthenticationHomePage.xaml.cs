
namespace TestHarness.Ext.Authentication.MSAL;

public sealed partial class MsalAuthenticationHomePage : Page
{
	public MsalAuthenticationHomeViewModel? ViewModel => DataContext as MsalAuthenticationHomeViewModel;
	public MsalAuthenticationHomePage()
	{
		this.InitializeComponent();
	}
}
