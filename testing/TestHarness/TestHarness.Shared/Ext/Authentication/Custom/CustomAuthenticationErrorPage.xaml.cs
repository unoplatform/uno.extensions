namespace TestHarness.Ext.Authentication.Custom;

public sealed partial class CustomAuthenticationErrorPage : Page
{
	public CustomAuthenticationErrorViewModel? ViewModel => DataContext as CustomAuthenticationErrorViewModel;

	public CustomAuthenticationErrorPage()
	{
		this.InitializeComponent();
	}
}
