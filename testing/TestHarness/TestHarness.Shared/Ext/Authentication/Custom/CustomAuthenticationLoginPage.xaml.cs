
namespace TestHarness.Ext.Authentication.Custom;
public sealed partial class CustomAuthenticationLoginPage : Page
{
	public CustomAuthenticationLoginViewModel? ViewModel => DataContext as CustomAuthenticationLoginViewModel;
	public CustomAuthenticationLoginPage()
	{
		this.InitializeComponent();
	}
}
