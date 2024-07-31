
namespace TestHarness.Ext.Authentication.Custom;

public sealed partial class CustomAuthenticationHomePage : Page
{
	public CustomAuthenticationHomeViewModel? ViewModel => DataContext as CustomAuthenticationHomeViewModel;
	public CustomAuthenticationHomePage()
	{
		this.InitializeComponent();
	}
}
