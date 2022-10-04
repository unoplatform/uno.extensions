namespace TestHarness.Ext.Authentication.Custom;

[TestSectionRoot("Authentication: Custom",TestSections.Authentication_Custom, typeof(CustomAuthenticationHostInit))]
[TestSectionRoot("Authentication: Custom with Service",TestSections.Authentication_Custom_Service, typeof(CustomAuthenticationServiceHostInit))]
[TestSectionRoot("Authentication: Custom with Test Backend",TestSections.Authentication_Custom_TestBackend, typeof(CustomAuthenticationTestBackendHostInit))]
[TestSectionRoot("Authentication: Custom with Test Backend using Cookies", TestSections.Authentication_Custom_TestBackend_Cookies, typeof(CustomAuthenticationTestBackendCookieHostInit))]
public sealed partial class CustomAuthenticationMainPage : BaseTestSectionPage
{
	public CustomAuthenticationMainPage()
	{
		this.InitializeComponent();
	}

	public async void ShowAppClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateRouteAsync(this,"");
	}
}
