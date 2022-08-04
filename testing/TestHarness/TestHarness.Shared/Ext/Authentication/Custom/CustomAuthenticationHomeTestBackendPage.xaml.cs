
namespace TestHarness.Ext.Authentication.Custom;

public sealed partial class CustomAuthenticationHomeTestBackendPage : Page
{
	public CustomAuthenticationHomeTestBackendViewModel? ViewModel => DataContext as CustomAuthenticationHomeTestBackendViewModel;
	public CustomAuthenticationHomeTestBackendPage()
	{
		this.InitializeComponent();
	}
}
