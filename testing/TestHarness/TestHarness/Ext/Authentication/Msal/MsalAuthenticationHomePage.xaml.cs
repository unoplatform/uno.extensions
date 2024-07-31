
namespace TestHarness.Ext.Authentication.MSAL;

public sealed partial class MsalAuthenticationHomePage : Page
{
	public MsalAuthenticationHomeViewModel? ViewModel => DataContext as MsalAuthenticationHomeViewModel;
	public MsalAuthenticationHomePage()
	{
		this.InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e) => base.OnNavigatedTo(e);

	protected override void OnNavigatedFrom(NavigationEventArgs e) => base.OnNavigatedFrom(e);

	protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) => base.OnNavigatingFrom(e);
}
