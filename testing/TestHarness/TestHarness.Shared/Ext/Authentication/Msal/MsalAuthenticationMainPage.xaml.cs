namespace TestHarness.Ext.Authentication.MSAL;

[TestSectionRoot("Authentication: Msal (config in code)",TestSections.Authentication_Msal, typeof(MsalAuthenticationHostInit))]
[TestSectionRoot("Authentication: Msal (config in settings)", TestSections.Authentication_Msal_Settings, typeof(MsalAuthenticationSettingsHostInit))]
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
