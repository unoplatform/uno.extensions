namespace TestHarness.Ext.Authentication.MSAL;

[TestSectionRoot("Authentication: Msal",TestSections.Authentication_Msal, typeof(MsalAuthenticationHostInit))]
public sealed partial class MsalAuthenticationMainPage : BaseTestSectionPage
{
	public MsalAuthenticationMainPage()
	{
		this.InitializeComponent();
	}

	public async void ShowAppClick(object sender, RoutedEventArgs e)
	{
		await NavigationRoot.Navigator()!.NavigateRouteAsync(this,"");
	}
}
