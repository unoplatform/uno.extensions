
namespace TestHarness.Ext.Authentication.Web;

public sealed partial class WebAuthenticationHomePage : Page
{
	public WebAuthenticationHomeViewModel? ViewModel => DataContext as WebAuthenticationHomeViewModel;
	public WebAuthenticationHomePage()
	{
		this.InitializeComponent();
	}
}
